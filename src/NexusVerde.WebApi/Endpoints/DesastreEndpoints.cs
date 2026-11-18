using NexusVerde.Application.Hateoas;
using System.Security.Claims;
using NexusVerde.Api.Filters;
using NexusVerde.Api.Security;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;

namespace NexusVerde.Api.Endpoints;

public static class DesastreEndpoints
{
    public static IEndpointRouteBuilder MapDesastreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/desastres").WithTags("Desastres").RequireAuthorization();

        group.MapGet("/", ListarAsync)
            .WithValidation<DesastreQuery>()
            .WithSummary("Lista desastres (paginado, com filtros e ordenação)")
            .WithDescription("Filtros: regiaoId, tipo, status. Ordenação (sortBy): gravidade, tipo, status, inicio.")
            .Produces<PagedResult<DesastreResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", ObterAsync)
            .WithSummary("Obtém um desastre")
            .Produces<DesastreResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CriarAsync)
            .RequireAuthorization(Policies.Gestor)
            .WithValidation<DesastreRequest>()
            .WithSummary("Registra um desastre e recalcula o risco da região (Gestor/Admin)")
            .Produces<DesastreResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", AtualizarAsync)
            .RequireAuthorization(Policies.Gestor)
            .WithValidation<DesastreUpdateRequest>()
            .WithSummary("Atualiza/encerra um desastre (Gestor/Admin)")
            .Produces<DesastreResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}", RemoverAsync)
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Remove um desastre sem vínculos (Admin)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListarAsync([AsParameters] DesastreQuery query, ClaimsPrincipal user,
        HttpRequest request, IDesastreService service, CancellationToken ct)
    {
        var contexto = user.ContextoOuNull();
        var pagina = (await service.ListarAsync(query, ct)).Map(d => ResourceLinks.Com(d, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsPrincipal user, IDesastreService service, CancellationToken ct) =>
        Results.Ok(ResourceLinks.Com(await service.ObterAsync(id, ct), user.ContextoOuNull()));

    private static async Task<IResult> CriarAsync(DesastreRequest request, IDesastreService service, CancellationToken ct)
    {
        var desastre = await service.CriarAsync(request, ct);
        return Results.Created($"/api/v1/desastres/{desastre.Id}", desastre);
    }

    private static async Task<IResult> AtualizarAsync(Guid id, DesastreUpdateRequest request, IDesastreService service, CancellationToken ct) =>
        Results.Ok(await service.AtualizarAsync(id, request, ct));

    private static async Task<IResult> RemoverAsync(Guid id, IDesastreService service, CancellationToken ct)
    {
        await service.RemoverAsync(id, ct);
        return Results.NoContent();
    }
}
