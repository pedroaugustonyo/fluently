namespace Fluently.API.Exceptions;

/// <summary>
/// Indisponibilidade temporária de um serviço.
/// </summary>
public sealed class ServiceUnavailableException : ApiException
{
    /// <summary>
    /// Inicializa uma nova instância do erro de serviço indisponível.
    /// </summary>
    /// <param name="detail">Detalhe público do erro.</param>
    public ServiceUnavailableException(string detail)
        : base(StatusCodes.Status503ServiceUnavailable, detail)
    {
    }
}
