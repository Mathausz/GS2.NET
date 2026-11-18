using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NexusVerde.Api.Options;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Telemetry;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Api.Middleware;

/// <summary>Marca endpoints que exigem o header Idempotency-Key.</summary>
public sealed class IdempotentMetadata;

public static class IdempotencyEndpointExtensions
{
    public static TBuilder RequireIdempotency<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new IdempotentMetadata());
}

/// <summary>
/// Idempotência para operações de criação (POST): a mesma Idempotency-Key + mesmo corpo devolve a
/// resposta original sem repetir o efeito; mesma chave com corpo diferente é rejeitada (422).
/// Executa após autenticação/autorização, então a chave é isolada por usuário e rota. A rota é normalizada
/// (sem "/" final e em minúsculas) e cada registro vale por <see cref="IdempotencyOptions.RetentionHours"/> horas.
/// </summary>
public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayHeaderName = "Idempotency-Replayed";

    // Locks em faixas: serializa requisições concorrentes com a mesma chave nesta instância.
    private static readonly SemaphoreSlim[] Locks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store, IOptions<IdempotencyOptions> options)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IdempotentMetadata>() is null)
        {
            await next(context);
            return;
        }

        var chave = context.Request.Headers[HeaderName].ToString().Trim();
        if (string.IsNullOrEmpty(chave) || chave.Length > 100)
        {
            await Problema(context, StatusCodes.Status400BadRequest, "Idempotency-Key ausente ou inválida",
                $"Informe o header '{HeaderName}' (até 100 caracteres) nesta operação.");
            return;
        }

        if (!Guid.TryParse(context.User.FindFirst("sub")?.Value, out var usuarioId))
        {
            await next(context);
            return;
        }

        var rota = MontarRota(context.Request.Method, context.Request.Path);

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(context.Request.Body, context.RequestAborted));
        context.Request.Body.Position = 0;

        var semaforo = Locks[(uint)$"{usuarioId}|{chave}|{rota}".GetHashCode() % (uint)Locks.Length];
        await semaforo.WaitAsync(context.RequestAborted);
        try
        {
            var existente = await store.BuscarAsync(chave, usuarioId, rota, context.RequestAborted);
            if (existente is not null && Expirado(existente.CriadoEm, options.Value.RetentionHours))
            {
                // Chave antiga: o registro vencido é descartado e a requisição é tratada como nova.
                await store.RemoverAsync(existente, context.RequestAborted);
                existente = null;
            }

            if (existente is not null)
            {
                if (existente.HashCorpo != hash)
                {
                    await Problema(context, StatusCodes.Status422UnprocessableEntity, "Idempotency-Key reutilizada",
                        "Esta Idempotency-Key já foi usada com um corpo de requisição diferente.");
                    return;
                }

                context.Response.StatusCode = existente.StatusCode;
                context.Response.ContentType = existente.ContentType ?? "application/json";
                context.Response.Headers[ReplayHeaderName] = "true";
                NexusVerdeTelemetry.IdempotencyReplays.Add(1);
                await context.Response.WriteAsync(existente.CorpoResposta ?? string.Empty, context.RequestAborted);
                return;
            }

            var original = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;
            try
            {
                await next(context);
            }
            finally
            {
                context.Response.Body = original;
            }

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await store.SalvarAsync(new RegistroIdempotencia
                {
                    Chave = chave,
                    UsuarioId = usuarioId,
                    Rota = rota,
                    HashCorpo = hash,
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType,
                    CorpoResposta = Encoding.UTF8.GetString(buffer.ToArray())
                }, context.RequestAborted);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(original, context.RequestAborted);
        }
        finally
        {
            semaforo.Release();
        }
    }

    /// <summary>
    /// "POST /api/v1/solicitacoes/" e "post /API/v1/solicitacoes" representam a mesma operação:
    /// remove a barra final e padroniza a caixa para que a mesma chave não gere dois registros.
    /// </summary>
    public static string MontarRota(string metodo, PathString path)
    {
        var caminho = (path.Value ?? string.Empty).TrimEnd('/');
        if (caminho.Length == 0) caminho = "/";

        var rota = $"{metodo.ToUpperInvariant()} {caminho.ToLowerInvariant()}";
        return rota.Length > 200 ? rota[..200] : rota;
    }

    public static bool Expirado(DateTime criadoEm, int retencaoHoras) =>
        retencaoHoras > 0 && criadoEm < DateTime.UtcNow.AddHours(-retencaoHoras);

    private static Task Problema(HttpContext context, int status, string titulo, string detalhe) =>
        Results.Problem(detail: detalhe, statusCode: status, title: titulo).ExecuteAsync(context);
}
