namespace Fluently.API.Exceptions;

/// <summary>
/// Indisponibilidade temporária de um serviço.
/// </summary>
public sealed class ServiceUnavailableException(string detail)
    : ApiException(StatusCodes.Status503ServiceUnavailable, detail) { }
