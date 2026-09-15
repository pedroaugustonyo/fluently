using System.ComponentModel.DataAnnotations;

namespace Fluently.API.Attributes;

/// <summary>
/// Validação de letra maiúscula em senhas.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordUppercaseAttribute : ValidationAttribute
{
    /// <summary>
    /// Inicializa a validação de letra maiúscula da senha.
    /// </summary>
    public PasswordUppercaseAttribute()
        : base("A senha deve conter pelo menos uma letra maiúscula.")
    {
    }

    /// <summary>
    /// Verifica se o valor contém pelo menos uma letra maiúscula.
    /// </summary>
    /// <param name="value">Valor que será validado.</param>
    /// <returns>Valor que indica se a senha atende ao requisito.</returns>
    public override bool IsValid(object? value)
    {
        if (value is not string password || password.Length == 0)
        {
            return true;
        }

        return password.Any(char.IsUpper);
    }
}
