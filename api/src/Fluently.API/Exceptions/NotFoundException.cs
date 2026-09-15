namespace Fluently.API.Exceptions;

/// <summary>
/// Ausência do recurso solicitado.
/// </summary>
public sealed class NotFoundException : ApiException
{
    /// <summary>
    /// Inicializa uma nova instância do erro de recurso não encontrado.
    /// </summary>
    /// <param name="detail">Detalhe público do erro.</param>
    public NotFoundException(string detail)
        : base(StatusCodes.Status404NotFound, detail)
    {
    }
}
