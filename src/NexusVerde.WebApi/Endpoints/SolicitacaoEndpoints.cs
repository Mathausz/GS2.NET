using NexusVerde.Application.Hateoas;
using System.Security.Claims;
using NexusVerde.Api.Filters;
using NexusVerde.Api.Middleware;
using NexusVerde.Api.Security;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;

namespace NexusVerde.Api.Endpoints;

public static class SolicitacaoEndpoints
{
    public static IEndpointRouteBuilder MapSolicitacaoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/solicitacoes").WithTags("Solicitações de ajuda").RequireAuthorization();

        group.MapPost("/", CriarAsync)
            .RequireAuthorization(Policies.Cidadao)
            .RequireRateLimiting(RateLimitPolicies.Criacao)
            .RequireIdempotency()
            .WithValidation<SolicitacaoRequest>()
            .WithSummary("Cidadão solicita ajuda (falta de água, água suspeita ou acesso negado)")
            .WithDescription("Calcula a prioridade (gravidade do desastre, pessoas, horas sem água e chuva prevista), " +
                             "indica o ponto de água seguro mais próximo e devolve as orientações de segurança. " +
                             "Exige o header Idempotency-Key.")
            .Produces<SolicitacaoCriadaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/minhas", ListarMinhasAsync)
            .RequireAuthorization(Policies.Cidadao)
            .WithValidation<SolicitacaoQuery>()
            .WithSummary("Lista as solicitações do cidadão autenticado")
            .Produces<PagedResult<SolicitacaoResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/fila", ListarFilaAsync)
            .RequireAuthorization(Policies.Gestor)
            .WithValidation<SolicitacaoQuery>()
            .WithSummary("Fila priorizada de solicitações (Gestor/Admin)")
            .WithDescription("Padrão: abertas e em atendimento, da maior para a menor prioridade. " +
                             "Gestores veem apenas o próprio órgão; Admin e Defesa Civil veem todos. " +
                             "Filtros: regiaoId, tipo, status, prioridade, orgaoDestino. Ordenação: score, criadoEm, status, tipo.")
            .Produces<PagedResult<SolicitacaoResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", ObterAsync)
            .WithSummary("Obtém uma solicitação (autor, gestor do órgão ou Admin)")
            .Produces<SolicitacaoResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/timeline", ListarTimelineAsync)
            .WithValidation<TimelineQuery>()
            .WithSummary("Linha do tempo da solicitação")
            .WithDescription("Eventos em ordem cronológica (desc=true inverte; filtro: tipo). Mesmo acesso da solicitação: " +
                             "autor, gestor do órgão ou Admin.")
            .Produces<PagedResult<TimelineEventoResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{id:guid}/notas", AdicionarNotaAsync)
            .RequireAuthorization(Policies.Gestor)
            .RequireRateLimiting(RateLimitPolicies.Criacao)
            .RequireIdempotency()
            .WithValidation<NotaRequest>()
            .WithSummary("Gestor registra uma nota na linha do tempo")
            .WithDescription("Anotação da equipe sobre o andamento. Exige Idempotency-Key (evita nota duplicada em reenvio).")
            .Produces<TimelineEventoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPatch("/{id:guid}/cancelar", CancelarAsync)
            .RequireAuthorization(Policies.Cidadao)
            .WithSummary("Cidadão cancela uma solicitação ainda aberta")
            .Produces<SolicitacaoResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> CriarAsync(SolicitacaoRequest request, ClaimsPrincipal user,
        ISolicitacaoService service, CancellationToken ct)
    {
        var resultado = await service.CriarAsync(user.ToContexto(), request, ct);
        return Results.Created($"/api/v1/solicitacoes/{resultado.Solicitacao.Id}", resultado);
    }

    private static async Task<IResult> ListarMinhasAsync([AsParameters] SolicitacaoQuery query, ClaimsPrincipal user,
        HttpRequest request, ISolicitacaoService service, CancellationToken ct)
    {
        var contexto = user.ToContexto();
        var pagina = (await service.ListarMinhasAsync(contexto, query, ct)).Map(s => ResourceLinks.Com(s, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ListarFilaAsync([AsParameters] SolicitacaoQuery query, ClaimsPrincipal user,
        HttpRequest request, ISolicitacaoService service, CancellationToken ct)
    {
        var contexto = user.ToContexto();
        var pagina = (await service.ListarFilaAsync(contexto, query, ct)).Map(s => ResourceLinks.Com(s, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsPrincipal user, ISolicitacaoService service, CancellationToken ct)
    {
        var contexto = user.ToContexto();
        return Results.Ok(ResourceLinks.Com(await service.ObterAsync(contexto, id, ct), contexto));
    }

    private static async Task<IResult> ListarTimelineAsync(Guid id, [AsParameters] TimelineQuery query, ClaimsPrincipal user,
        HttpRequest request, ITimelineService service, CancellationToken ct)
    {
        var pagina = (await service.ListarAsync(user.ToContexto(), id, query, ct)).Map(e => ResourceLinks.Com(e));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> AdicionarNotaAsync(Guid id, NotaRequest request, ClaimsPrincipal user,
        ITimelineService service, CancellationToken ct)
    {
        var evento = await service.AdicionarNotaAsync(user.ToContexto(), id, request, ct);
        return Results.Created($"/api/v1/solicitacoes/{id}/timeline", ResourceLinks.Com(evento));
    }

    private static async Task<IResult> CancelarAsync(Guid id, ClaimsPrincipal user, ISolicitacaoService service, CancellationToken ct) =>
        Results.Ok(await service.CancelarAsync(user.ToContexto(), id, ct));
}
