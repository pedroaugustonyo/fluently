using Fluently.API.Controllers.v1;
using Fluently.API.DTOs.Tasks;
using Fluently.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fluently.API.UnitTests;

public sealed class TaskControllersTests
{
    [Fact]
    public async Task CreateAsync_TaskServiceResponse_ReturnsCreatedAtRoute()
    {
        var service = new Mock<ITaskService>();
        var response = new TaskResponseDTO { Id = Guid.NewGuid(), Title = "Estudar" };
        service
            .Setup(item => item.CreateAsync(It.IsAny<CreateTaskRequestDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var controller = new TasksController(service.Object);

        var action = await controller.CreateAsync(new CreateTaskRequestDTO(), CancellationToken.None);
        var result = Assert.IsType<CreatedAtRouteResult>(action.Result);

        Assert.Equal("GetTaskById", result.RouteName);
        Assert.Equal(response, result.Value);
    }

    [Fact]
    public async Task GetAsync_DailyXpGoalServiceResponse_ReturnsOk()
    {
        var service = new Mock<IDailyXpGoalService>();
        var response = new DailyXpGoalResponseDTO { TargetXp = 10, EarnedXp = 5 };
        service.Setup(item => item.GetAsync(CancellationToken.None)).ReturnsAsync(response);
        var controller = new DailyXpGoalController(service.Object);

        var action = await controller.GetAsync(CancellationToken.None);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Equal(response, result.Value);
    }
}
