using System.ComponentModel;

namespace Fluently.API.Enums;

/// <summary>
/// Níveis de proficiência disponíveis.
/// </summary>
public enum ProficiencyLevelEnum
{
    /// <summary>
    /// Nível básico inicial do CEFR.
    /// </summary>
    [Description("A1 - Básico inicial.")]
    A1 = 1,

    /// <summary>
    /// Nível básico do CEFR.
    /// </summary>
    [Description("A2 - Básico.")]
    A2 = 2,

    /// <summary>
    /// Nível intermediário inicial do CEFR.
    /// </summary>
    [Description("B1 - Intermediário inicial.")]
    B1 = 3,

    /// <summary>
    /// Nível intermediário superior do CEFR.
    /// </summary>
    [Description("B2 - Intermediário superior.")]
    B2 = 4,

    /// <summary>
    /// Nível avançado do CEFR.
    /// </summary>
    [Description("C1 - Avançado.")]
    C1 = 5,

    /// <summary>
    /// Nível proficiente do CEFR.
    /// </summary>
    [Description("C2 - Proficiente.")]
    C2 = 6
}
