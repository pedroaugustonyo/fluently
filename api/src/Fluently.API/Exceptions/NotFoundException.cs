namespace Fluently.API.Exceptions;

/// <summary>
/// Ausência do recurso solicitado.
/// </summary>
public sealed class NotFoundException(string detail) : ApiException(StatusCodes.Status404NotFound, detail) { }
