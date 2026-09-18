using System.ComponentModel.DataAnnotations;
using Fluently.API.Enums;

namespace Fluently.API.DTOs.Users;

/// <summary>
/// Dados opcionais utilizados para atualizar o perfil do usuário.
/// </summary>
public sealed class UpdateUserRequestDTO
{
    /// <summary>
    /// Primeiro nome.
    /// </summary>
    [MinLength(1, ErrorMessage = "O primeiro nome não pode estar vazio.")]
    [StringLength(100, ErrorMessage = "O primeiro nome deve ter até 100 caracteres.")]
    public string? FirstName { get; init; }

    /// <summary>
    /// Sobrenome.
    /// </summary>
    [MinLength(1, ErrorMessage = "O sobrenome não pode estar vazio.")]
    [StringLength(100, ErrorMessage = "O sobrenome deve ter até 100 caracteres.")]
    public string? LastName { get; init; }

    /// <summary>
    /// Nível atual de proficiência.
    /// </summary>
    [EnumDataType(typeof(ProficiencyLevelEnum), ErrorMessage = "Informe um nível de proficiência válido.")]
    public ProficiencyLevelEnum? Proficiency { get; init; }

    /// <summary>
    /// Biografia usada como contexto das questões geradas.
    /// </summary>
    [MinLength(1, ErrorMessage = "A biografia não pode estar vazia.")]
    [StringLength(2000, ErrorMessage = "A biografia deve ter até 2000 caracteres.")]
    public string? Bio { get; init; }

    /// <summary>
    /// Imagem de perfil codificada em Base64.
    /// </summary>
    [StringLength(1000000, ErrorMessage = "A imagem de perfil é muito grande.")]
    public string? ProfileImageBase64 { get; init; }
}
