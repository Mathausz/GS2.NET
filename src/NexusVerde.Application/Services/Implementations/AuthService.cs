using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Application.Telemetry;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Services.Implementations;

public class AuthService(
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    ITokenGenerator tokenGenerator) : IAuthService
{
    public async Task<UsuarioResponse> RegistrarAsync(RegistroRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await usuarios.GetByEmailAsync(email, ct) is not null)
            throw new ConflictException("Já existe um usuário cadastrado com este e-mail.");

        // Cadastro público sempre cria Cidadão; gestores são criados por um Admin.
        var usuario = new Usuario(request.Nome, email, hasher.Hash(request.Senha), PerfilUsuario.Cidadao);
        await usuarios.AddAsync(usuario, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return usuario.ToResponse();
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        using var activity = NexusVerdeTelemetry.ActivitySource.StartActivity("Auth.Login");
        var usuario = await usuarios.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        // Mensagem única para não revelar se o e-mail existe.
        if (usuario is null || !usuario.Ativo || !hasher.Verify(request.Senha, usuario.SenhaHash))
        {
            NexusVerdeTelemetry.LoginsFalhos.Add(1);
            throw new UnauthorizedException("E-mail ou senha inválidos.");
        }

        var (token, expiresAt) = tokenGenerator.Generate(usuario);
        return new LoginResponse(token, expiresAt, usuario.ToResponse());
    }
}
