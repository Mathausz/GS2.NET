using Microsoft.AspNetCore.Identity;
using NexusVerde.Application.Interfaces;

namespace NexusVerde.Infrastructure.Security;

/// <summary>Hash de senha com PBKDF2 (implementação do ASP.NET Core Identity, com salt e iterações).</summary>
public sealed class Pbkdf2PasswordHasher : NexusVerde.Application.Interfaces.IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Marker = new();

    public string Hash(string senha) => _hasher.HashPassword(Marker, senha);

    public bool Verify(string senha, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        return _hasher.VerifyHashedPassword(Marker, hash, senha) != PasswordVerificationResult.Failed;
    }
}
