namespace Fluently.API.Exceptions;

/// <summary>
/// Erro causado por uma requisição inválida.
/// </summary>
public sealed class BadRequestException(string detail) : ApiException(StatusCodes.Status400BadRequest, detail) { }
