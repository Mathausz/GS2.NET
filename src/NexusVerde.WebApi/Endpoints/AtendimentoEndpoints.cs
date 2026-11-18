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

public static class AtendimentoEndpoints
{
    public static IEndpointRouteBuilder MapAtendimentoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/atendimentos")
            .WithTags("Atendimentos")
            .RequireAuthorization(Policies.Gestor);

        group.MapPost("/", CriarAsync)
            .RequireRateLimiting(RateLimitPolicies.Criacao)
            .RequireIdempotency()
            .WithValidation<AtendimentoRequest>()
            .WithSummary("Gestor assume o atendimento de uma solicitação")
            .WithDescription("Usa o ponto de água informado ou o sugerido na solicitação; o ponto precisa estar Ativo. Exige Idempotency-Key.")
            .Produces<AtendimentoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPatch("/{id:guid}/status", AtualizarStatusAsync)
            .WithValidation<AtendimentoStatusRequest>()
            .WithSummary("Conclui ou cancela um atendimento")
            .Produces<AtendimentoResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", ListarAsync)
            .WithValidation<AtendimentoQuery>()
            .WithSummary("Lista atendimentos do gestor (Admin vê todos)")
            .Produces<PagedResult<AtendimentoResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", ObterAsync)
            .WithSummary("Obtém um atendimento")
            .Produces<AtendimentoResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CriarAsync(AtendimentoRequest request, ClaimsPrincipal user,
        IAtendimentoService service, CancellationToken ct)
    {
        var atendimento = await service.CriarAsync(user.ToContexto(), request, ct);
        return Results.Created($"/api/v1/atendimentos/{atendimento.Id}", atendimento);
    }

    private static async Task<IResult> AtualizarStatusAsync(Guid id, AtendimentoStatusRequest request, ClaimsPrincipal user,
        IAtendimentoService service, CancellationToken ct) =>
        Results.Ok(await service.AtualizarStatusAsync(user.ToContexto(), id, request, ct));

    private static async Task<IResult> ListarAsync([AsParameters] AtendimentoQuery query, ClaimsPrincipal user,
        HttpRequest request, IAtendimentoService service, CancellationToken ct)
    {
        var contexto = user.ToContexto();
        var pagina = (await service.ListarAsync(contexto, query, ct)).Map(a => ResourceLinks.Com(a, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsPrincipal user, IAtendimentoService service, CancellationToken ct)
    {
        var contexto = user.ToContexto();
        return Results.Ok(ResourceLinks.Com(await service.ObterAsync(contexto, id, ct), contexto));
    }
}
