using Fluently.API.DTOs.Questions;
using Fluently.API.Models;

namespace Fluently.API.Services;

/// <summary>
/// Operações de geração de questões personalizadas.
/// </summary>
public interface IQuestionGenerationService
{
    /// <summary>
    /// Gera uma questão de acordo com o contexto disponível do estudante.
    /// </summary>
    /// <param name="user">Usuário cujo contexto será utilizado na questão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão gerada e validada.</returns>
    Task<GeneratedQuestionDTO> GenerateAsync(UserModel user, CancellationToken cancellationToken);
}
