using Fluently.API.DTOs.Common;

namespace Fluently.API.Helpers;

/// <summary>
/// Cálculos e criação de respostas paginadas.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Calcula a quantidade de registros que devem ser ignorados.
    /// </summary>
    /// <param name="request">Parâmetros da página solicitada.</param>
    /// <returns>Quantidade de registros anteriores à página atual.</returns>
    public static int CalculateSkip(PaginationRequestDTO request)
    {
        return (request.Page - 1) * request.PageSize;
    }

    /// <summary>
    /// Cria uma resposta paginada com os metadados calculados.
    /// </summary>
    /// <param name="items">Itens contidos na página atual.</param>
    /// <param name="request">Parâmetros da página solicitada.</param>
    /// <param name="totalItems">Quantidade total de itens disponíveis.</param>
    /// <typeparam name="T">Tipo dos itens retornados.</typeparam>
    /// <returns>Resposta paginada pronta para retorno pela API.</returns>
    public static PaginatedResponseDTO<T> CreateResponse<T>(IReadOnlyList<T> items,
                                                             PaginationRequestDTO request,
                                                             int totalItems)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)request.PageSize));

        return new PaginatedResponseDTO<T>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }
}
