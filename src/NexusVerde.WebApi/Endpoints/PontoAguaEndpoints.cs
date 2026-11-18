using NexusVerde.Application.Hateoas;
using System.Security.Claims;
using NexusVerde.Api.Filters;
using NexusVerde.Api.Security;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;

namespace NexusVerde.Api.Endpoints;

public static class PontoAguaEndpoints
{
    public static IEndpointRouteBuilder MapPontoAguaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pontos-agua").WithTags("Pontos de água").RequireAuthorization();

        group.MapGet("/", ListarAsync)
            .WithValidation<PontoAguaQuery>()
            .WithSummary("Lista pontos de água (paginado, com filtros e ordenação)")
            .WithDescription("Filtros: busca, regiaoId, status. Ordenação (sortBy): nome, capacidade, status.")
            .Produces<PagedResult<PontoAguaResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/proximos", ProximosAsync)
            .WithSummary("Pontos de água ativos mais próximos de uma coordenada")
            .WithDescription("Ordena por distância (km). Use lat/lng do cidadão; limite de 1 a 20 (padrão 5).")
            .Produces<IReadOnlyList<PontoAguaProximoResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", ObterAsync)
            .WithSummary("Obtém um ponto de água")
            .Produces<PontoAguaResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CriarAsync)
            .RequireAuthorization(Policies.Gestor)
            .WithValidation<PontoAguaRequest>()
            .WithSummary("Cadastra um ponto de água (Gestor/Admin)")
            .Produces<PontoAguaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", AtualizarAsync)
            .RequireAuthorization(Policies.Gestor)
            .WithValidation<PontoAguaRequest>()
            .WithSummary("Atualiza um ponto de água (Gestor/Admin)")
            .Produces<PontoAguaResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .RequireAuthorization(Policies.Gestor)
            .WithValidation<PontoAguaStatusRequest>()
            .WithSummary("Altera o status (Ativo, Lotado, Contaminado, Inativo) (Gestor/Admin)")
            .Produces<PontoAguaResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", RemoverAsync)
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Remove um ponto de água sem vínculos (Admin)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListarAsync([AsParameters] PontoAguaQuery query, ClaimsPrincipal user,
        HttpRequest request, IPontoAguaService service, CancellationToken ct)
    {
        var contexto = user.ContextoOuNull();
        var pagina = (await service.ListarAsync(query, ct)).Map(p => ResourceLinks.Com(p, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ProximosAsync(double lat, double lng, int? limite, ClaimsPrincipal user,
        IPontoAguaService service, CancellationToken ct)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["lat/lng"] = new[] { "lat deve estar entre -90 e 90 e lng entre -180 e 180." }
            });
        }

        var contexto = user.ContextoOuNull();
        var proximos = await service.ProximosAsync(lat, lng, limite ?? 5, ct);
        return Results.Ok(proximos.Select(p => ResourceLinks.Com(p, contexto)).ToList());
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsPrincipal user, IPontoAguaService service, CancellationToken ct) =>
        Results.Ok(ResourceLinks.Com(await service.ObterAsync(id, ct), user.ContextoOuNull()));

    private static async Task<IResult> CriarAsync(PontoAguaRequest request, IPontoAguaService service, CancellationToken ct)
    {
        var ponto = await service.CriarAsync(request, ct);
        return Results.Created($"/api/v1/pontos-agua/{ponto.Id}", ponto);
    }

    private static async Task<IResult> AtualizarAsync(Guid id, PontoAguaRequest request, IPontoAguaService service, CancellationToken ct) =>
        Results.Ok(await service.AtualizarAsync(id, request, ct));

    private static async Task<IResult> AlterarStatusAsync(Guid id, PontoAguaStatusRequest request, IPontoAguaService service, CancellationToken ct) =>
        Results.Ok(await service.AlterarStatusAsync(id, request.Status, ct));

    private static async Task<IResult> RemoverAsync(Guid id, IPontoAguaService service, CancellationToken ct)
    {
        await service.RemoverAsync(id, ct);
        return Results.NoContent();
    }
}
