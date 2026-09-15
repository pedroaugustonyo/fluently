namespace Fluently.API.Exceptions;

/// <summary>
/// Erro causado por uma requisição inválida.
/// </summary>
public sealed class BadRequestException : ApiException
{
    /// <summary>
    /// Inicializa uma nova instância do erro de requisição inválida.
    /// </summary>
    /// <param name="detail">Detalhe público do erro.</param>
    public BadRequestException(string detail)
        : base(StatusCodes.Status400BadRequest, detail)
    {
    }
}
