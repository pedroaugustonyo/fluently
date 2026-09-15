using System.ComponentModel.DataAnnotations;

using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Questions;
using Fluently.API.DTOs.Users;
using Fluently.API.Enums;

namespace Fluently.API.UnitTests;

public sealed class DTOValidationTests
{
    [Theory]
    [InlineData("short", "A senha deve ter entre 8 e 128 caracteres.")]
    [InlineData("lowercase1!", "A senha deve conter pelo menos uma letra maiúscula.")]
    [InlineData("NoNumber!", "A senha deve conter pelo menos um número.")]
    [InlineData("NoSymbol1", "A senha deve conter pelo menos um símbolo.")]
    public void RegisterRequest_InvalidPasswordRule_ReturnsPortugueseValidationError(string password,
                                                                                     string expectedMessage)
    {
        var request = CreateRegisterRequest(password, password);

        var results = Validate(request);

        Assert.Contains(expectedMessage, results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void RegisterRequest_ConfirmationDoesNotMatch_ReturnsPortugueseValidationError()
    {
        var request = CreateRegisterRequest("Valid123!", "Other123!");

        var results = Validate(request);

        Assert.Contains(
            "A confirmação da senha deve ser igual à senha.",
            results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void RegisterRequest_InvalidPasswordAndConfirmation_ReturnsEveryValidationError()
    {
        var request = CreateRegisterRequest("pass", "different");

        var results = Validate(request);
        var errorMessages = results.Select(result => result.ErrorMessage!).ToArray();

        Assert.Contains("A senha deve ter entre 8 e 128 caracteres.", errorMessages);
        Assert.Contains("A senha deve conter pelo menos uma letra maiúscula.", errorMessages);
        Assert.Contains("A senha deve conter pelo menos um número.", errorMessages);
        Assert.Contains("A senha deve conter pelo menos um símbolo.", errorMessages);
        Assert.Contains("A confirmação da senha deve ser igual à senha.", errorMessages);
    }

    [Fact]
    public void RegisterRequest_PasswordMeetingEveryRule_ReturnsNoValidationErrors()
    {
        var request = CreateRegisterRequest("Valid123!", "Valid123!");

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("FirstName", "O primeiro nome é obrigatório.")]
    [InlineData("LastName", "O sobrenome é obrigatório.")]
    [InlineData("Email", "O e-mail é obrigatório.")]
    [InlineData("Password", "A senha é obrigatória.")]
    [InlineData("PasswordConfirmation", "A confirmação da senha é obrigatória.")]
    public void RegisterRequest_MissingRequiredField_ReturnsPortugueseValidationError(string field,
                                                                                      string expectedMessage)
    {
        var request = new CreateUserRequestDTO
        {
            FirstName = field == nameof(CreateUserRequestDTO.FirstName) ? string.Empty : "Pedro",
            LastName = field == nameof(CreateUserRequestDTO.LastName) ? string.Empty : "Oliveira",
            Email = field == nameof(CreateUserRequestDTO.Email) ? string.Empty : "pedro@example.com",
            Password = field == nameof(CreateUserRequestDTO.Password) ? string.Empty : "Valid123!",
            PasswordConfirmation = field == nameof(CreateUserRequestDTO.PasswordConfirmation)
                ? string.Empty
                : "Valid123!"
        };

        var results = Validate(request);

        Assert.Contains(expectedMessage, results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void UpdateUserProfileRequest_CompleteProfile_ReturnsNoValidationErrors()
    {
        var request = CreateUpdateUserProfileRequest();

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Fact]
    public void UpdateUserProfileRequest_UndefinedProficiency_ReturnsPortugueseValidationError()
    {
        var request = new UpdateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Proficiency = (ProficiencyLevelEnum)0,
            Bio = "Quero praticar para uma viagem."
        };

        var results = Validate(request);

        Assert.Contains(
            "Informe um nível de proficiência válido.",
            results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void UpdateUserRequest_EmptyBio_ReturnsPortugueseValidationError()
    {
        var request = new UpdateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Proficiency = ProficiencyLevelEnum.B1,
            Bio = string.Empty
        };

        var results = Validate(request);

        Assert.Contains(
            "A biografia não pode estar vazia.",
            results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void UpdateUserProfileRequest_BioExceedsMaximumLength_ReturnsPortugueseValidationError()
    {
        var request = new UpdateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Proficiency = ProficiencyLevelEnum.B1,
            Bio = new string('a', 2001)
        };

        var results = Validate(request);

        Assert.Contains(
            "A biografia deve ter até 2000 caracteres.",
            results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void UpdateUserCredentialsRequest_InvalidFields_ReturnsEveryValidationError()
    {
        var request = new UpdateUserCredentialsRequestDTO
        {
            Email = "invalid-email",
            Password = "pass",
            PasswordConfirmation = "different"
        };

        var results = Validate(request);
        var errorMessages = results.Select(result => result.ErrorMessage!).ToArray();

        Assert.Contains("Informe um endereço de e-mail válido.", errorMessages);
        Assert.Contains("A senha deve ter entre 8 e 128 caracteres.", errorMessages);
        Assert.Contains("A senha deve conter pelo menos uma letra maiúscula.", errorMessages);
        Assert.Contains("A senha deve conter pelo menos um número.", errorMessages);
        Assert.Contains("A senha deve conter pelo menos um símbolo.", errorMessages);
        Assert.Contains("A confirmação da senha deve ser igual à senha.", errorMessages);
    }

    [Theory]
    [InlineData(0, 20, "A página deve ser maior ou igual a 1.")]
    [InlineData(1, 0, "A quantidade por página deve estar entre 1 e 100.")]
    [InlineData(1, 101, "A quantidade por página deve estar entre 1 e 100.")]
    public void PaginationRequest_InvalidBoundary_ReturnsPortugueseValidationError(int page,
                                                                                   int pageSize,
                                                                                   string expectedMessage)
    {
        var request = new PaginationRequestDTO { Page = page, PageSize = pageSize };

        var results = Validate(request);

        Assert.Contains(expectedMessage, results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void SubmitQuestionAnswerRequest_IndexBelowRange_ReturnsPortugueseValidationError()
    {
        var request = new SubmitQuestionAnswerRequestDTO { AlternativeIndex = 0 };

        var results = Validate(request);

        Assert.Contains(
            "Informe um índice de alternativa entre 1 e 5.",
            results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void SubmitQuestionAnswerRequest_IndexAboveRange_ReturnsPortugueseValidationError()
    {
        var request = new SubmitQuestionAnswerRequestDTO { AlternativeIndex = 6 };

        var results = Validate(request);

        Assert.Contains(
            "Informe um índice de alternativa entre 1 e 5.",
            results.Select(result => result.ErrorMessage!));
    }

    [Fact]
    public void SubmitQuestionAnswerRequest_ValidAlternativeIndex_ReturnsNoValidationErrors()
    {
        var request = new SubmitQuestionAnswerRequestDTO { AlternativeIndex = 1 };

        var results = Validate(request);

        Assert.Empty(results);
    }

    private static CreateUserRequestDTO CreateRegisterRequest(string password, string confirmation)
    {
        return new CreateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            Password = password,
            PasswordConfirmation = confirmation
        };
    }

    private static UpdateUserRequestDTO CreateUpdateUserProfileRequest()
    {
        return new UpdateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Proficiency = ProficiencyLevelEnum.B1,
            Bio = "Quero praticar para uma viagem."
        };
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        return results;
    }
}
