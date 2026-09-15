using System.ComponentModel.DataAnnotations;

using Fluently.API.Attributes;

namespace Fluently.API.DTOs.Users;

/// <summary>
/// Dados necessários para criar uma conta de usuário.
/// </summary>
public sealed class CreateUserRequestDTO
{
    /// <summary>
    /// Primeiro nome.
    /// </summary>
    [Required(ErrorMessage = "O primeiro nome é obrigatório.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O primeiro nome deve ter até 100 caracteres.")]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Sobrenome.
    /// </summary>
    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O sobrenome deve ter até 100 caracteres.")]
    public string LastName { get; init; } = string.Empty;

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
    [PasswordUppercase]
    [PasswordNumber]
    [PasswordSymbol]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Confirmação da senha.
    /// </summary>
    [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
    [Compare(nameof(Password), ErrorMessage = "A confirmação da senha deve ser igual à senha.")]
    public string PasswordConfirmation { get; init; } = string.Empty;
}
