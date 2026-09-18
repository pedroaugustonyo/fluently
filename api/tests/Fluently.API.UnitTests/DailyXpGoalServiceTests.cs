using Fluently.API.DTOs.Tasks;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Fluently.API.Services;
using Moq;

namespace Fluently.API.UnitTests;

public sealed class DailyXpGoalServiceTests
{
    private readonly Mock<ICurrentUserService> currentUserService = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IQuestionRepository> questionRepository = new();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task UpdateAsync_ValidGoal_PersistsGoalAndReturnsDailyProgress()
    {
        var user = TestData.CreateUser();
        currentUserService.Setup(service => service.GetUserId()).Returns(user.Id);
        userRepository.Setup(item => item.GetByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        userRepository.Setup(item => item.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        questionRepository
            .Setup(item =>
                item.GetAwardedXpAsync(
                    user.Id,
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(),
                    cancellationToken
                )
            )
            .ReturnsAsync(20);

        var response = await CreateService()
            .UpdateAsync(new UpdateDailyXpGoalRequestDTO { TargetXp = 20 }, cancellationToken);

        Assert.Equal(20, user.DailyXpGoal);
        Assert.Equal(20, response.EarnedXp);
        Assert.True(response.IsCompleted);
    }

    private DailyXpGoalService CreateService() =>
        new(
            currentUserService.Object,
            userRepository.Object,
            questionRepository.Object,
            new FixedTimeProvider(TestData.Now)
        );
}
