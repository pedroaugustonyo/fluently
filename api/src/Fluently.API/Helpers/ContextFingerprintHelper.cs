using System.Security.Cryptography;
using System.Text;

namespace Fluently.API.Helpers;

/// <summary>
/// Identificadores de contextos de questões.
/// </summary>
public static class ContextFingerprintHelper
{
    /// <summary>
    /// Cria uma impressão digital determinística para um contexto.
    /// </summary>
    /// <param name="context">Contexto utilizado na geração.</param>
    /// <returns>Impressão digital hexadecimal do contexto.</returns>
    public static string Create(string context)
    {
        var normalizedContext = AnswerNormalizerHelper.Normalize(context);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedContext)));
    }
}
