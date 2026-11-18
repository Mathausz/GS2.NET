using System.ComponentModel.DataAnnotations;

namespace NexusVerde.Api.Options;

/// <summary>Configuração do JWT. A chave NUNCA fica no repositório: vem de variável de ambiente (Jwt__Key).</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required] public string Issuer { get; set; } = "NexusVerde.WebApi";
    [Required] public string Audience { get; set; } = "NexusVerde.Clients";

    [Required, MinLength(32, ErrorMessage = "Jwt:Key deve ter pelo menos 32 caracteres.")]
    public string Key { get; set; } = string.Empty;

    [Range(1, 1440)] public int ExpiresMinutes { get; set; } = 60;
}

/// <summary>Idempotência: por quanto tempo uma Idempotency-Key fica válida (padrão 24h; 0 = sem expiração).</summary>
public sealed class IdempotencyOptions
{
    public const string Section = "Idempotency";

    [Range(0, 24 * 30)] public int RetentionHours { get; set; } = 24;
}

public sealed class LimitPolicyOptions
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>Limites de requisições por janela fixa (HTTP 429 ao exceder).</summary>
public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    /// <summary>Limite geral por usuário autenticado (ou por IP quando anônimo).</summary>
    public LimitPolicyOptions Global { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60 };

    /// <summary>Login e cadastro: mais restritivo contra força bruta.</summary>
    public LimitPolicyOptions Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Criação de solicitações e atendimentos.</summary>
    public LimitPolicyOptions Criacao { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };
}
