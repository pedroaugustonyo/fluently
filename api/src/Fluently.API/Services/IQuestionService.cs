using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Questions;

namespace Fluently.API.Services;

/// <summary>
/// Operações do fluxo de exercícios do estudante.
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// Obtém a questão pendente do usuário autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão pendente.</returns>
    Task<QuestionResponseDTO> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma página de questões do usuário autenticado.
    /// </summary>
    /// <param name="request">Parâmetros de paginação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Página de questões do usuário.</returns>
    Task<PaginatedResponseDTO<QuestionDetailsResponseDTO>> GetAllAsync(PaginationRequestDTO request,
                                                                       CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma questão do usuário autenticado.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão encontrada.</returns>
    Task<QuestionDetailsResponseDTO> GetByIdAsync(Guid questionId, CancellationToken cancellationToken);

    /// <summary>
    /// Cria uma nova questão para o usuário com contexto de aprendizagem preenchido.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão criada.</returns>
    Task<QuestionResponseDTO> CreateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registra a única resposta permitida para uma questão.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="request">Resposta enviada pelo estudante.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Resultado da resposta e progresso atualizado.</returns>
    Task<QuestionAnswerResponseDTO> SubmitAnswerAsync(Guid questionId,
                                                      SubmitQuestionAnswerRequestDTO request,
                                                      CancellationToken cancellationToken);
}
