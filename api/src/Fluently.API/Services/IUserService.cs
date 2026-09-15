using Fluently.API.DTOs.Users;

namespace Fluently.API.Services;

/// <summary>
/// Operações relacionadas ao usuário autenticado.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Obtém os dados do usuário autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Dados públicos do usuário autenticado.</returns>
    Task<GetUserResponseDTO> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza o perfil do usuário.
    /// </summary>
    /// <param name="request">Dados do perfil que serão atualizados.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Dados atualizados do usuário.</returns>
    Task<UpdateUserResponseDTO> UpdateProfileAsync(UpdateUserRequestDTO request,
                                                    CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza as credenciais do usuário autenticado.
    /// </summary>
    /// <param name="request">Novo endereço de e-mail, nova senha e sua confirmação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a operação assíncrona.</returns>
    Task UpdateCredentialsAsync(UpdateUserCredentialsRequestDTO request,
                                CancellationToken cancellationToken);
}
