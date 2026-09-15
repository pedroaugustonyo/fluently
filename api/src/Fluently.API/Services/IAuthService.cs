using Fluently.API.DTOs.Users;

namespace Fluently.API.Services;

/// <summary>
/// Operações de cadastro e autenticação de usuários.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Cadastra uma nova conta de usuário.
    /// </summary>
    /// <param name="request">Dados necessários para o cadastro.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Dados públicos do usuário cadastrado.</returns>
    Task<CreateUserResponseDTO> RegisterAsync(CreateUserRequestDTO request,
                                               CancellationToken cancellationToken);

    /// <summary>
    /// Autentica um usuário com e-mail e senha.
    /// </summary>
    /// <param name="request">Credenciais utilizadas na autenticação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Token de acesso e dados do usuário autenticado.</returns>
    Task<LoginUserResponseDTO> LoginAsync(LoginUserRequestDTO request,
                                          CancellationToken cancellationToken);
}
