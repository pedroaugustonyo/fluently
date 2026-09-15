using System.ComponentModel.DataAnnotations;

namespace Fluently.API.Attributes;

/// <summary>
/// Validação de número em senhas.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordNumberAttribute : ValidationAttribute
{
    /// <summary>
    /// Inicializa a validação de número da senha.
    /// </summary>
    public PasswordNumberAttribute()
        : base("A senha deve conter pelo menos um número.")
    {
    }

    /// <summary>
    /// Verifica se o valor contém pelo menos um número.
    /// </summary>
    /// <param name="value">Valor que será validado.</param>
    /// <returns>Valor que indica se a senha atende ao requisito.</returns>
    public override bool IsValid(object? value)
    {
        if (value is not string password || password.Length == 0)
        {
            return true;
        }

        return password.Any(char.IsDigit);
    }
}
