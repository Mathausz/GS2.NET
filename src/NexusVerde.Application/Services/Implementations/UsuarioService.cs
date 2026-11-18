using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Services.Implementations;

public class UsuarioService(
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher) : IUsuarioService
{
    public async Task<PagedResult<UsuarioResponse>> ListarAsync(UsuarioQuery query, CancellationToken ct = default) =>
        (await usuarios.GetPagedAsync(query, ct)).Map(u => u.ToResponse());

    public async Task<UsuarioResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdAsync(id, ct) ?? throw new NotFoundException("Usuário não encontrado.");
        return usuario.ToResponse();
    }

    public async Task<UsuarioResponse> CriarAsync(UsuarioCreateRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await usuarios.GetByEmailAsync(email, ct) is not null)
            throw new ConflictException("Já existe um usuário cadastrado com este e-mail.");

        var usuario = new Usuario(request.Nome, email, hasher.Hash(request.Senha), request.Perfil, request.Orgao);
        await usuarios.AddAsync(usuario, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return usuario.ToResponse();
    }

    public async Task DesativarAsync(Guid id, Guid adminId, CancellationToken ct = default)
    {
        if (id == adminId)
            throw new ConflictException("Você não pode desativar o próprio usuário.");

        var usuario = await usuarios.GetByIdAsync(id, ct) ?? throw new NotFoundException("Usuário não encontrado.");
        usuario.Desativar();
        await unitOfWork.SaveChangesAsync(ct);
    }
}
