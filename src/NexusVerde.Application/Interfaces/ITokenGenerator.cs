using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Interfaces;

public interface ITokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(Usuario usuario);
}
