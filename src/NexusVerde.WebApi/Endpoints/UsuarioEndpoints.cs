using NexusVerde.Application.Hateoas;
using System.Security.Claims;
using NexusVerde.Api.Filters;
using NexusVerde.Api.Security;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;

namespace NexusVerde.Api.Endpoints;

public static class UsuarioEndpoints
{
    public static IEndpointRouteBuilder MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/usuarios")
            .WithTags("Usuários (Admin)")
            .RequireAuthorization(Policies.Admin);

        group.MapGet("/", ListarAsync)
            .WithValidation<UsuarioQuery>()
            .WithSummary("Lista usuários (paginado, com filtros e ordenação)")
            .Produces<PagedResult<UsuarioResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", ObterAsync)
            .WithSummary("Obtém um usuário")
            .Produces<UsuarioResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CriarAsync)
            .WithValidation<UsuarioCreateRequest>()
            .WithSummary("Cria um usuário com perfil e órgão (inclusive Gestor)")
            .Produces<UsuarioResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}", DesativarAsync)
            .WithSummary("Desativa um usuário (não pode mais autenticar)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListarAsync([AsParameters] UsuarioQuery query, ClaimsPrincipal user,
        HttpRequest request, IUsuarioService service, CancellationToken ct)
    {
        var contexto = user.ContextoOuNull();
        var pagina = (await service.ListarAsync(query, ct)).Map(u => ResourceLinks.Com(u, contexto));
        return Results.Ok(pagina.ComLinksDePagina(request));
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsPrincipal user, IUsuarioService service, CancellationToken ct) =>
        Results.Ok(ResourceLinks.Com(await service.ObterAsync(id, ct), user.ContextoOuNull()));

    private static async Task<IResult> CriarAsync(UsuarioCreateRequest request, IUsuarioService service, CancellationToken ct)
    {
        var usuario = await service.CriarAsync(request, ct);
        return Results.Created($"/api/v1/usuarios/{usuario.Id}", usuario);
    }

    private static async Task<IResult> DesativarAsync(Guid id, ClaimsPrincipal user,
        IUsuarioService service, CancellationToken ct)
    {
        await service.DesativarAsync(id, user.ToContexto().Id, ct);
        return Results.NoContent();
    }
}
