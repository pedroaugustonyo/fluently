using Fluently.API.DTOs.Tasks;
using Fluently.API.Enums;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Fluently.API.Services;
using Moq;

namespace Fluently.API.UnitTests;

public sealed class TaskServiceTests
{
    private readonly Mock<ICurrentUserService> currentUserService = new();
    private readonly Mock<ITaskRepository> taskRepository = new();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task CreateAsync_AuthenticatedUser_PersistsTaskForCurrentUser()
    {
        var user = TestData.CreateUser();
        currentUserService.Setup(service => service.GetUserId()).Returns(user.Id);
        taskRepository
            .Setup(item => item.AddAsync(It.IsAny<TaskModel>(), cancellationToken))
            .Returns(Task.CompletedTask);
        taskRepository.Setup(item => item.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);

        var response = await CreateService()
            .CreateAsync(
                new CreateTaskRequestDTO { Title = "Praticar inglês", Priority = TaskPriorityEnum.High },
                cancellationToken
            );

        Assert.Equal("Praticar inglês", response.Title);
        taskRepository.Verify(
            item => item.AddAsync(It.Is<TaskModel>(task => task.UserId == user.Id), cancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task SetCompletionAsync_CompletedTask_UsesInjectedClock()
    {
        var task = TestData.CreateTask();
        currentUserService.Setup(service => service.GetUserId()).Returns(task.UserId);
        taskRepository.Setup(item => item.GetOwnedAsync(task.Id, task.UserId, cancellationToken)).ReturnsAsync(task);
        taskRepository.Setup(item => item.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);

        var response = await CreateService()
            .SetCompletionAsync(task.Id, new TaskCompletionRequestDTO { IsCompleted = true }, cancellationToken);

        Assert.True(response.IsCompleted);
        Assert.Equal(TestData.Now, response.CompletedAt);
    }

    private TaskService CreateService() =>
        new(currentUserService.Object, taskRepository.Object, new FixedTimeProvider(TestData.Now));
}
