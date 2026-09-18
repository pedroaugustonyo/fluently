namespace Fluently.API.Exceptions;

/// <summary>
/// Erro controlado retornado pela API.
/// </summary>
public abstract class ApiException : Exception
{
    /// <summary>
    /// Código HTTP associado ao erro.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Detalhe público do erro.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Inicializa uma nova instância de um erro controlado da API.
    /// </summary>
    /// <param name="statusCode">Código HTTP associado ao erro.</param>
    /// <param name="detail">Detalhe público do erro.</param>
    protected ApiException(int statusCode, string detail)
        : base(detail)
    {
        StatusCode = statusCode;
        Detail = detail;
    }
}
