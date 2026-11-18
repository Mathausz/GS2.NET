using NexusVerde.Application.Hateoas;
using System.Security.Claims;
using NexusVerde.Api.Filters;
using NexusVerde.Api.Security;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;

namespace NexusVerde.Api.Endpoints;

public static class OrientacaoEndpoints
{
    public static IEndpointRouteBuilder MapOrientacaoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orientacoes").WithTags("Orientações de segurança");

        group.MapGet("/", ListarAsync)
            .AllowAnonymous()
            .WithValidation<OrientacaoQuery>()
            .WithSummary("Lista instruções de segurança (público; filtre por tipo)")
            .WithDescription("Informação pública, disponível sem login: o que fazer quando a água está suspeita, falta ou o acesso é negado.")
            .Produces<PagedResult<OrientacaoResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", ObterAsync)
            .AllowAnonymous()
            .WithSummary("Obtém uma orientação (público)")
            .Produces<OrientacaoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CriarAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<OrientacaoRequest>()
            .WithSummary("Cadastra uma orientação (Admin)")
            .Produces<OrientacaoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", AtualizarAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<OrientacaoRequest>()
            .WithSummary("Atualiza uma orientação (Admin)")
            .Produces<OrientacaoResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", RemoverAsync)
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Remove uma orientação (Admin)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListarAsync([AsParameters] OrientacaoQuery query, ClaimsPrincipal user,
        HttpRequest request, IOrientacaoService service, CancellationToken ct)
    {
        var contexto = user.ContextoOuNull();
        var pagina = (await service.ListarAsync(query, ct)).Map(o => ResourceLinks.Com(o, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsPrincipal user, IOrientacaoService service, CancellationToken ct) =>
        Results.Ok(ResourceLinks.Com(await service.ObterAsync(id, ct), user.ContextoOuNull()));

    private static async Task<IResult> CriarAsync(OrientacaoRequest request, IOrientacaoService service, CancellationToken ct)
    {
        var orientacao = await service.CriarAsync(request, ct);
        return Results.Created($"/api/v1/orientacoes/{orientacao.Id}", orientacao);
    }

    private static async Task<IResult> AtualizarAsync(Guid id, OrientacaoRequest request, IOrientacaoService service, CancellationToken ct) =>
        Results.Ok(await service.AtualizarAsync(id, request, ct));

    private static async Task<IResult> RemoverAsync(Guid id, IOrientacaoService service, CancellationToken ct)
    {
        await service.RemoverAsync(id, ct);
        return Results.NoContent();
    }
}
