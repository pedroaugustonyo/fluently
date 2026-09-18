namespace Fluently.API.Exceptions;

/// <summary>
/// Conflito com o estado atual do recurso.
/// </summary>
public sealed class ConflictException(string detail) : ApiException(StatusCodes.Status409Conflict, detail) { }
