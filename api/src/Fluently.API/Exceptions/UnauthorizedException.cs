namespace Fluently.API.Exceptions;

/// <summary>
/// Falha de autenticação ou identificação do usuário.
/// </summary>
public sealed class UnauthorizedException : ApiException
{
    /// <summary>
    /// Inicializa uma nova instância do erro de autenticação.
    /// </summary>
    /// <param name="detail">Detalhe público do erro.</param>
    public UnauthorizedException(string detail)
        : base(StatusCodes.Status401Unauthorized, detail)
    {
    }
}
