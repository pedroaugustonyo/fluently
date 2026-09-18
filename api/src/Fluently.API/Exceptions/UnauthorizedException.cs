namespace Fluently.API.Exceptions;

/// <summary>
/// Falha de autenticação ou identificação do usuário.
/// </summary>
public sealed class UnauthorizedException(string detail) : ApiException(StatusCodes.Status401Unauthorized, detail) { }
