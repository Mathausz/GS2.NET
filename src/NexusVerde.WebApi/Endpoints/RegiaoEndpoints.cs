using NexusVerde.Application.Hateoas;
using System.Security.Claims;
using NexusVerde.Api.Filters;
using NexusVerde.Api.Security;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;

namespace NexusVerde.Api.Endpoints;

public static class RegiaoEndpoints
{
    public static IEndpointRouteBuilder MapRegiaoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/regioes").WithTags("Regiões").RequireAuthorization();

        group.MapGet("/", ListarAsync)
            .WithValidation<RegiaoQuery>()
            .WithSummary("Lista regiões afetadas (paginado, com filtros e ordenação)")
            .WithDescription("Filtros: busca, estado, nivelRisco. Ordenação (sortBy): nome, municipio, estado, risco, populacao, criadoEm.")
            .Produces<PagedResult<RegiaoResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", ObterAsync)
            .WithSummary("Obtém uma região")
            .Produces<RegiaoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CriarAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<RegiaoRequest>()
            .WithSummary("Cadastra uma região (Admin)")
            .Produces<RegiaoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}", AtualizarAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<RegiaoRequest>()
            .WithSummary("Atualiza uma região (Admin)")
            .Produces<RegiaoResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", RemoverAsync)
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Remove uma região sem vínculos (Admin)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListarAsync([AsParameters] RegiaoQuery query, ClaimsPrincipal user,
        HttpRequest request, IRegiaoService service, CancellationToken ct)
    {
        var contexto = user.ContextoOuNull();
        var pagina = (await service.ListarAsync(query, ct)).Map(r => ResourceLinks.Com(r, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsPrincipal user, IRegiaoService service, CancellationToken ct) =>
        Results.Ok(ResourceLinks.Com(await service.ObterAsync(id, ct), user.ContextoOuNull()));

    private static async Task<IResult> CriarAsync(RegiaoRequest request, IRegiaoService service, CancellationToken ct)
    {
        var regiao = await service.CriarAsync(request, ct);
        return Results.Created($"/api/v1/regioes/{regiao.Id}", regiao);
    }

    private static async Task<IResult> AtualizarAsync(Guid id, RegiaoRequest request, IRegiaoService service, CancellationToken ct) =>
        Results.Ok(await service.AtualizarAsync(id, request, ct));

    private static async Task<IResult> RemoverAsync(Guid id, IRegiaoService service, CancellationToken ct)
    {
        await service.RemoverAsync(id, ct);
        return Results.NoContent();
    }
}
