namespace Fluently.API.DTOs.Common;

/// <summary>
/// Coleção paginada.
/// </summary>
/// <typeparam name="T">Tipo dos itens da coleção.</typeparam>
public sealed class PaginatedResponseDTO<T>
{
    /// <summary>
    /// Itens contidos na página atual.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// Número da página atual.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Quantidade máxima de itens por página.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Quantidade total de itens.
    /// </summary>
    public int TotalItems { get; init; }

    /// <summary>
    /// Quantidade total de páginas.
    /// </summary>
    public int TotalPages { get; init; }
}
