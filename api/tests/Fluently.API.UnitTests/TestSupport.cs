using Fluently.API.Enums;
using Fluently.API.Models;

namespace Fluently.API.UnitTests;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    internal FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}

internal static class TestData
{
    internal static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    internal static UserModel CreateUser()
    {
        return new UserModel
        {
            Id = Guid.Parse("a3ee4427-b5ce-4984-8fa0-e437e03feaf7"),
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            NormalizedEmail = "PEDRO@EXAMPLE.COM",
            PasswordHash = "hashed-password",
            Proficiency = ProficiencyLevelEnum.B1,
            Bio = "Quero participar de reuniões internacionais.",
            CreatedAt = Now,
            UpdatedAt = Now,
        };
    }

    internal static QuestionModel CreateQuestion(UserModel? user = null)
    {
        user ??= CreateUser();

        return new QuestionModel
        {
            Id = Guid.Parse("0c761726-49f8-4d90-877c-e358982ec63a"),
            UserId = user.Id,
            User = user,
            Context = "Pedro está descrevendo sua rotina de estudos.",
            Question = "I ? English every day.",
            QuestionTranslation = "Eu estudo inglês todos os dias.",
            ContextFingerprint = "FINGERPRINT",
            Alternatives = ["study", "practice", "speak", "read", "write"],
            AlternativeTranslations = ["estudar", "praticar", "falar", "ler", "escrever"],
            CorrectAlternativeIndex = 1,
            BaseXp = 15,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
    }

    internal static TaskModel CreateTask(UserModel? user = null)
    {
        user ??= CreateUser();

        return new TaskModel
        {
            Id = Guid.Parse("c2f9f72c-c47e-48af-a8f9-a2664f236d9f"),
            UserId = user.Id,
            User = user,
            Title = "Revisar vocabulário",
            Priority = TaskPriorityEnum.High,
            CreatedAt = Now,
        };
    }
}
