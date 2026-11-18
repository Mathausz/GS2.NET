using NexusVerde.Api.Filters;
using NexusVerde.Api.Security;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Services.Interfaces;

namespace NexusVerde.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Autenticação");

        group.MapPost("/registro", RegistrarAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .WithValidation<RegistroRequest>()
            .WithSummary("Cadastra um cidadão")
            .WithDescription("Cadastro público. Sempre cria um usuário com perfil Cidadão; gestores são criados por um administrador.")
            .Produces<UsuarioResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .WithValidation<LoginRequest>()
            .WithSummary("Autentica e devolve o token JWT")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static async Task<IResult> RegistrarAsync(RegistroRequest request, IAuthService service, CancellationToken ct)
    {
        var usuario = await service.RegistrarAsync(request, ct);
        return Results.Created($"/api/v1/usuarios/{usuario.Id}", usuario);
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, IAuthService service, CancellationToken ct) =>
        Results.Ok(await service.LoginAsync(request, ct));
}
