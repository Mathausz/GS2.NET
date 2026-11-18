namespace NexusVerde.Domain.Exceptions;

/// <summary>
/// Violação de uma regra de negócio do domínio (estado inválido, transição não permitida etc.).
/// É traduzida para HTTP 422 pela camada de API.
/// </summary>
public class DomainException(string message) : Exception(message);
