using System.ComponentModel.DataAnnotations;

using Fluently.API.Attributes;

namespace Fluently.API.DTOs.Users;

/// <summary>
/// Credenciais do usuário autenticado.
/// </summary>
public sealed class UpdateUserCredentialsRequestDTO
{
    /// <summary>
    /// Endereço de e-mail.
    /// </summary>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um endereço de e-mail válido.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter até 320 caracteres.")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Nova senha.
    /// </summary>
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 128 caracteres.")]
    [PasswordUppercase]
    [PasswordNumber]
    [PasswordSymbol]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Confirmação da nova senha.
    /// </summary>
    [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
    [Compare(nameof(Password), ErrorMessage = "A confirmação da senha deve ser igual à senha.")]
    public string PasswordConfirmation { get; init; } = string.Empty;
}
