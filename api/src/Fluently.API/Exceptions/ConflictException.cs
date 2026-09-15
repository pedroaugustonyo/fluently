namespace Fluently.API.Exceptions;

/// <summary>
/// Conflito com o estado atual do recurso.
/// </summary>
public sealed class ConflictException : ApiException
{
    /// <summary>
    /// Inicializa uma nova instância do erro de conflito.
    /// </summary>
    /// <param name="detail">Detalhe público do erro.</param>
    public ConflictException(string detail)
        : base(StatusCodes.Status409Conflict, detail)
    {
    }
}
