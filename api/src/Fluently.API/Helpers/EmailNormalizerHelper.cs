namespace Fluently.API.Helpers;

/// <summary>
/// Normalização de endereços de e-mail.
/// </summary>
public static class EmailNormalizerHelper
{
    /// <summary>
    /// Normaliza um endereço de e-mail para comparação e persistência.
    /// </summary>
    /// <param name="email">Endereço de e-mail que será normalizado.</param>
    /// <returns>Endereço de e-mail normalizado.</returns>
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
