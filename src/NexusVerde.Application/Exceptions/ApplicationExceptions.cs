namespace NexusVerde.Application.Exceptions;

/// <summary>Recurso inexistente (HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Conflito com o estado atual dos dados (HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Usuário autenticado sem permissão para o recurso (HTTP 403).</summary>
public class ForbiddenException(string message) : Exception(message);

/// <summary>Credenciais inválidas ou ausentes (HTTP 401).</summary>
public class UnauthorizedException(string message) : Exception(message);

/// <summary>Dependência externa (ex.: armazenamento da linha do tempo) indisponível para uma operação que depende dela (HTTP 503).</summary>
public class ExternalServiceException(string message) : Exception(message);
