using System.ComponentModel.DataAnnotations;

namespace Fluently.API.DTOs.Users;

/// <summary>
/// Credenciais utilizadas para autenticação.
/// </summary>
public sealed class LoginUserRequestDTO
{
    /// <summary>
    /// Endereço de e-mail.
    /// </summary>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um endereço de e-mail válido.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter até 320 caracteres.")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Senha.
    /// </summary>
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 128 caracteres.")]
    public string Password { get; init; } = string.Empty;
}
