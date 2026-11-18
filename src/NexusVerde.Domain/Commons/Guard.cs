using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Domain.Commons;

/// <summary>Validações reutilizáveis das invariantes das entidades.</summary>
public static class Guard
{
    public static string NotEmpty(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} é obrigatório.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} deve ter no máximo {maxLength} caracteres.");

        return trimmed;
    }

    public static void Coordenadas(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude deve estar entre -90 e 90.");
        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude deve estar entre -180 e 180.");
    }

    public static void Faixa(int value, int min, int max, string field)
    {
        if (value < min || value > max)
            throw new DomainException($"{field} deve estar entre {min} e {max}.");
    }
}
