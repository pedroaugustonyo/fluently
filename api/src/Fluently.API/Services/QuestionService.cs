using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Questions;
using Fluently.API.Exceptions;
using Fluently.API.Helpers;
using Fluently.API.Models;
using Fluently.API.Repositories;

namespace Fluently.API.Services;

/// <summary>
/// Criação, consulta e resposta das questões do estudante.
/// </summary>
public sealed class QuestionService : IQuestionService
{
    private const int StreakMultiplierThreshold = 3;

    /// <summary>
    /// Serviço do usuário atual.
    /// </summary>
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Repositório de usuários.
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Repositório de questões.
    /// </summary>
    private readonly IQuestionRepository _questionRepository;

    /// <summary>
    /// Serviço de geração de questões.
    /// </summary>
    private readonly IQuestionGenerationService _questionGenerationService;

    /// <summary>
    /// Registrador de exercícios.
    /// </summary>
    private readonly ILogger<QuestionService> _logger;

    /// <summary>
    /// Inicializa uma nova instância do serviço de questões.
    /// </summary>
    /// <param name="currentUserService">Serviço utilizado para identificar o usuário atual.</param>
    /// <param name="userRepository">Repositório utilizado para acessar os usuários.</param>
    /// <param name="questionRepository">Repositório utilizado para acessar as questões.</param>
    /// <param name="questionGenerationService">Serviço utilizado para gerar questões.</param>
    /// <param name="logger">Registrador dos eventos internos dos exercícios.</param>
    public QuestionService(ICurrentUserService currentUserService,
                           IUserRepository userRepository,
                           IQuestionRepository questionRepository,
                           IQuestionGenerationService questionGenerationService,
                           ILogger<QuestionService> logger)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _questionRepository = questionRepository;
        _questionGenerationService = questionGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém a questão pendente.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão pendente.</returns>
    public async Task<QuestionResponseDTO> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var question = await _questionRepository.GetCurrentAsync(userId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException("Não existe uma questão pendente.");
        }

        return MapToResponse(question);
    }

    /// <summary>
    /// Obtém uma página de questões.
    /// </summary>
    /// <param name="request">Parâmetros de paginação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Página de questões.</returns>
    public async Task<PaginatedResponseDTO<QuestionDetailsResponseDTO>> GetAllAsync(PaginationRequestDTO request,
                                                                                      CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var skip = PaginationHelper.CalculateSkip(request);
        var totalItems = await _questionRepository.CountByUserAsync(userId, cancellationToken);
        var questions = await _questionRepository.GetPageByUserAsync(
            userId,
            skip,
            request.PageSize,
            cancellationToken);
        var items = questions.Select(MapToDetailsResponse).ToArray();

        return PaginationHelper.CreateResponse(items, request, totalItems);
    }

    /// <summary>
    /// Obtém uma questão pelo ID.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão encontrada.</returns>
    public async Task<QuestionDetailsResponseDTO> GetByIdAsync(Guid questionId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var question = await _questionRepository.GetByIdAsync(questionId, userId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException("A questão não foi encontrada.");
        }

        return MapToDetailsResponse(question);
    }

    /// <summary>
    /// Cria uma nova questão somente quando não existe outra pendente.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão criada.</returns>
    public async Task<QuestionResponseDTO> CreateAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var user = await GetUserAsync(userId, cancellationToken);

        EnsureLearningSettingsAreComplete(user);
        await EnsureThereIsNoPendingQuestionAsync(userId, cancellationToken);

        return await CreateForUserAsync(user, cancellationToken);
    }

    /// <summary>
    /// Gera e persiste uma questão para o usuário informado.
    /// </summary>
    /// <param name="user">Usuário que receberá a questão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão criada.</returns>
    private async Task<QuestionResponseDTO> CreateForUserAsync(UserModel user,
                                                                CancellationToken cancellationToken)
    {
        var generatedQuestion = await _questionGenerationService.GenerateAsync(user, cancellationToken);
        var question = CreateQuestion(user, generatedQuestion);

        await _questionRepository.AddAsync(question, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Question created. QuestionId: {QuestionId}, UserId: {UserId}",
            question.Id,
            user.Id);

        return MapToResponse(question);
    }

    /// <summary>
    /// Registra a única alternativa permitida para uma questão.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="request">Índice da alternativa enviada pelo estudante.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Resultado da resposta e progresso atualizado.</returns>
    public async Task<QuestionAnswerResponseDTO> SubmitAnswerAsync(Guid questionId,
                                                                    SubmitQuestionAnswerRequestDTO request,
                                                                    CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var question = await GetOwnedQuestionAsync(userId, questionId, cancellationToken);

        if (question.AnsweredAt is not null)
        {
            throw new ConflictException("Esta questão já foi respondida.");
        }

        var isCorrect = request.AlternativeIndex == question.CorrectAlternativeIndex;
        var awardedXp = ApplyProgress(question.User, question.BaseXp, isCorrect);

        ApplyAnswer(question, request.AlternativeIndex, isCorrect, awardedXp);

        _questionRepository.Update(question);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Question answered. QuestionId: {QuestionId}, UserId: {UserId}, " +
            "IsCorrect: {IsCorrect}, AwardedXp: {AwardedXp}",
            questionId,
            userId,
            isCorrect,
            awardedXp);

        return MapToAnswerResponse(question);
    }

    /// <summary>
    /// Obtém o usuário informado.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Usuário encontrado.</returns>
    private async Task<UserModel> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("O usuário autenticado não foi encontrado.");
    }

    /// <summary>
    /// Garante que o usuário não possui uma questão pendente.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a verificação assíncrona.</returns>
    private async Task EnsureThereIsNoPendingQuestionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var currentQuestion = await _questionRepository.GetCurrentAsync(userId, cancellationToken);

        if (currentQuestion is not null)
        {
            throw new ConflictException(
                "Responda a questão atual antes de solicitar uma nova questão.");
        }
    }

    /// <summary>
    /// Obtém uma questão pertencente ao usuário informado.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão encontrada.</returns>
    private async Task<QuestionModel> GetOwnedQuestionAsync(Guid userId,
                                                            Guid questionId,
                                                            CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetOwnedAsync(questionId, userId, cancellationToken);

        return question ?? throw new NotFoundException("A questão não foi encontrada.");
    }

    /// <summary>
    /// Garante que os dados necessários para gerar questões foram preenchidos.
    /// </summary>
    /// <param name="user">Usuário que terá os dados verificados.</param>
    private static void EnsureLearningSettingsAreComplete(UserModel user)
    {
        var hasValidProficiency = user.Proficiency.HasValue &&
                                  Enum.IsDefined(user.Proficiency.Value);

        if (!hasValidProficiency || string.IsNullOrWhiteSpace(user.Bio))
        {
            throw new BadRequestException(
                "Preencha seu nível de proficiência e biografia antes de gerar uma questão.");
        }
    }

    /// <summary>
    /// Cria a entidade persistida a partir da questão gerada.
    /// </summary>
    /// <param name="user">Usuário que receberá a questão.</param>
    /// <param name="generatedQuestion">Dados internos da questão gerada.</param>
    /// <returns>Entidade de questão pronta para persistência.</returns>
    private static QuestionModel CreateQuestion(UserModel user, GeneratedQuestionDTO generatedQuestion)
    {
        return new QuestionModel
        {
            UserId = user.Id,
            User = user,
            Context = generatedQuestion.Context,
            Question = generatedQuestion.Question,
            QuestionTranslation = generatedQuestion.QuestionTranslation,
            ContextFingerprint = generatedQuestion.ContextFingerprint,
            Alternatives = generatedQuestion.Alternatives.Select(alternative => alternative.Text).ToArray(),
            AlternativeTranslations = generatedQuestion.Alternatives
                .Select(alternative => alternative.Translation)
                .ToArray(),
            CorrectAlternativeIndex = generatedQuestion.CorrectAlternativeIndex,
            BaseXp = 15
        };
    }

    /// <summary>
    /// Registra a resposta e o resultado diretamente na questão.
    /// </summary>
    /// <param name="question">Questão respondida.</param>
    /// <param name="alternativeIndex">Índice da alternativa enviada.</param>
    /// <param name="isCorrect">Valor que indica se a resposta está correta.</param>
    /// <param name="awardedXp">Experiência concedida pela resposta.</param>
    private static void ApplyAnswer(QuestionModel question,
                                    int alternativeIndex,
                                    bool isCorrect,
                                    int awardedXp)
    {
        question.SubmittedAlternativeIndex = alternativeIndex;
        question.IsCorrect = isCorrect;
        question.AwardedXp = awardedXp;
        question.StreakAfterAnswer = question.User.CurrentStreak;
        question.AnsweredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Atualiza a experiência e a sequência do usuário após uma resposta.
    /// </summary>
    /// <param name="user">Usuário cujo progresso será atualizado.</param>
    /// <param name="baseXp">Experiência base da questão.</param>
    /// <param name="isCorrect">Valor que indica se a resposta está correta.</param>
    /// <returns>Experiência concedida pela resposta.</returns>
    private static int ApplyProgress(UserModel user, int baseXp, bool isCorrect)
    {
        if (!isCorrect)
        {
            user.CurrentStreak = 0;

            return 0;
        }

        var awardedXp = user.CurrentStreak + 1 >= StreakMultiplierThreshold
            ? baseXp * 2
            : baseXp;

        user.CurrentStreak++;
        user.TotalXp += awardedXp;

        return awardedXp;
    }

    /// <summary>
    /// Converte as alternativas persistidas para a resposta pública.
    /// </summary>
    /// <param name="question">Questão que contém as alternativas.</param>
    /// <returns>Alternativas com índice e tradução.</returns>
    private static IReadOnlyList<QuestionAlternativeResponseDTO> MapAlternatives(QuestionModel question)
    {
        return question.Alternatives
            .Select((alternative, index) => new QuestionAlternativeResponseDTO
            {
                Index = index + 1,
                Text = alternative,
                Translation = question.AlternativeTranslations[index]
            })
            .ToArray();
    }

    /// <summary>
    /// Converte a alternativa correta para sua resposta pública.
    /// </summary>
    /// <param name="question">Questão respondida.</param>
    /// <returns>Alternativa correta.</returns>
    private static QuestionAlternativeResponseDTO MapCorrectAlternative(QuestionModel question)
    {
        var index = question.CorrectAlternativeIndex - 1;

        return new QuestionAlternativeResponseDTO
        {
            Index = question.CorrectAlternativeIndex,
            Text = question.Alternatives[index],
            Translation = question.AlternativeTranslations[index]
        };
    }

    /// <summary>
    /// Converte a questão para sua resposta pública.
    /// </summary>
    /// <param name="question">Questão que será convertida.</param>
    /// <returns>Dados públicos da questão.</returns>
    private static QuestionResponseDTO MapToResponse(QuestionModel question)
    {
        return new QuestionResponseDTO
        {
            Id = question.Id,
            Context = question.Context,
            Question = question.Question,
            Alternatives = MapAlternatives(question),
            BaseXp = question.BaseXp,
            CreatedAt = question.CreatedAt
        };
    }

    /// <summary>
    /// Converte uma questão para sua resposta detalhada.
    /// </summary>
    /// <param name="question">Questão que será convertida.</param>
    /// <returns>Dados públicos da questão.</returns>
    private static QuestionDetailsResponseDTO MapToDetailsResponse(QuestionModel question)
    {
        var wasAnswered = question.AnsweredAt is not null;

        return new QuestionDetailsResponseDTO
        {
            Id = question.Id,
            Context = question.Context,
            Question = question.Question,
            Alternatives = MapAlternatives(question),
            QuestionTranslation = wasAnswered ? question.QuestionTranslation : null,
            CorrectAlternative = wasAnswered ? MapCorrectAlternative(question) : null,
            BaseXp = question.BaseXp,
            SubmittedAlternativeIndex = question.SubmittedAlternativeIndex,
            IsCorrect = question.IsCorrect,
            AwardedXp = question.AwardedXp,
            CreatedAt = question.CreatedAt,
            AnsweredAt = question.AnsweredAt
        };
    }

    /// <summary>
    /// Converte a resposta registrada para seu resultado público.
    /// </summary>
    /// <param name="question">Questão respondida que será convertida.</param>
    /// <returns>Resultado público da resposta.</returns>
    private static QuestionAnswerResponseDTO MapToAnswerResponse(QuestionModel question)
    {
        return new QuestionAnswerResponseDTO
        {
            QuestionId = question.Id,
            IsCorrect = question.IsCorrect!.Value,
            AwardedXp = question.AwardedXp!.Value,
            TotalXp = question.User.TotalXp,
            CurrentStreak = question.StreakAfterAnswer!.Value,
            CorrectAlternative = MapCorrectAlternative(question),
            QuestionTranslation = question.QuestionTranslation,
            AnsweredAt = question.AnsweredAt!.Value
        };
    }
}
