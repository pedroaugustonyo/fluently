namespace Fluently.API.Services;

/// <summary>
/// Dados do usuário autenticado na requisição atual.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Obtém o identificador do usuário autenticado.
    /// </summary>
    /// <returns>Identificador do usuário autenticado.</returns>
    Guid GetUserId();
}
