using System.ComponentModel.DataAnnotations;

namespace Fluently.API.Attributes;

/// <summary>
/// Validação de símbolo em senhas.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordSymbolAttribute() : ValidationAttribute("A senha deve conter pelo menos um símbolo.")
{
    /// <summary>
    /// Verifica se o valor contém pelo menos um símbolo.
    /// </summary>
    /// <param name="value">Valor que será validado.</param>
    /// <returns>Valor que indica se a senha atende ao requisito.</returns>
    public override bool IsValid(object? value)
    {
        if (value is not string password || password.Length == 0)
        {
            return true;
        }

        return password.Any(character => char.IsPunctuation(character) || char.IsSymbol(character));
    }
}
