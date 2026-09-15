using System.Globalization;
using System.Text;

namespace Fluently.API.Helpers;

/// <summary>
/// Normalização de respostas textuais.
/// </summary>
public static class AnswerNormalizerHelper
{
    /// <summary>
    /// Normaliza uma resposta para permitir comparações consistentes.
    /// </summary>
    /// <param name="value">Texto que será normalizado.</param>
    /// <returns>Texto normalizado para comparação.</returns>
    public static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            if (char.IsPunctuation(character))
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
            previousWasSpace = false;
        }

        return builder.ToString().Trim().Normalize(NormalizationForm.FormC);
    }
}
