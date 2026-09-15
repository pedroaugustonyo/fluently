using System.ComponentModel.DataAnnotations;

namespace Fluently.API.DTOs.Common;

/// <summary>
/// Parâmetros de paginação.
/// </summary>
public sealed class PaginationRequestDTO
{
    /// <summary>
    /// Número da página.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a 1.")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Quantidade de itens por página.
    /// </summary>
    [Range(1, 100, ErrorMessage = "A quantidade por página deve estar entre 1 e 100.")]
    public int PageSize { get; init; } = 20;
}
