using Fluently.API.DTOs.Common;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Fluently.API.Services;

using Moq;

namespace Fluently.API.UnitTests;

public sealed class LeaderboardServiceTests
{
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task GetAsync_RepositoryOrderedUsers_MapsStableRankingAndFullNames()
    {
        var users = new[]
        {
            CreateRankedUser("Ana", "Silva", 90, 1),
            CreateRankedUser("Bruno", "Souza", 45, 2)
        };
        userRepository
            .Setup(repository => repository.CountLeaderboardAsync(cancellationToken))
            .ReturnsAsync(2);
        userRepository
            .Setup(repository => repository.GetLeaderboardPageAsync(0, 20, cancellationToken))
            .ReturnsAsync(users);
        var service = new LeaderboardService(userRepository.Object);

        var response = await service.GetAsync(
            new PaginationRequestDTO(),
            cancellationToken);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1, response.Items[0].Rank);
        Assert.Equal("Ana Silva", response.Items[0].FullName);
        Assert.Equal(90L, response.Items[0].TotalXp);
        Assert.Equal(45L, response.Items[1].TotalXp);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task GetAsync_SecondPage_RequestsCorrectSliceAndAssignsAbsoluteRanks()
    {
        var users = new[]
        {
            CreateRankedUser("User", "Eleven", 30, 11),
            CreateRankedUser("User", "Twelve", 15, 12)
        };
        userRepository
            .Setup(repository => repository.CountLeaderboardAsync(cancellationToken))
            .ReturnsAsync(12);
        userRepository
            .Setup(repository => repository.GetLeaderboardPageAsync(10, 10, cancellationToken))
            .ReturnsAsync(users);
        var service = new LeaderboardService(userRepository.Object);

        var response = await service.GetAsync(
            new PaginationRequestDTO { Page = 2, PageSize = 10 },
            cancellationToken);

        Assert.Equal(11, response.Items[0].Rank);
        Assert.Equal(12, response.Items[1].Rank);
        Assert.Equal(2, response.TotalPages);
        userRepository.Verify(repository =>
            repository.GetLeaderboardPageAsync(10, 10, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_EmptyLeaderboard_ReturnsEmptyPageMetadata()
    {
        userRepository
            .Setup(repository => repository.CountLeaderboardAsync(cancellationToken))
            .ReturnsAsync(0);
        userRepository
            .Setup(repository => repository.GetLeaderboardPageAsync(0, 20, cancellationToken))
            .ReturnsAsync([]);
        var service = new LeaderboardService(userRepository.Object);

        var response = await service.GetAsync(new PaginationRequestDTO(), cancellationToken);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalItems);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task GetAsync_PartialLastPage_RoundsTotalPagesUp()
    {
        userRepository
            .Setup(repository => repository.CountLeaderboardAsync(cancellationToken))
            .ReturnsAsync(21);
        userRepository
            .Setup(repository => repository.GetLeaderboardPageAsync(0, 20, cancellationToken))
            .ReturnsAsync([]);
        var service = new LeaderboardService(userRepository.Object);

        var response = await service.GetAsync(new PaginationRequestDTO(), cancellationToken);

        Assert.Equal(2, response.TotalPages);
    }

    private static UserModel CreateRankedUser(string firstName, string lastName, long totalXp, int idSuffix)
    {
        return new UserModel
        {
            Id = new Guid($"00000000-0000-0000-0000-{idSuffix:D12}"),
            FirstName = firstName,
            LastName = lastName,
            Email = $"user{idSuffix}@example.com",
            NormalizedEmail = $"USER{idSuffix}@EXAMPLE.COM",
            PasswordHash = "hash",
            TotalXp = totalXp,
            CreatedAt = TestData.Now.AddMinutes(idSuffix),
            UpdatedAt = TestData.Now.AddMinutes(idSuffix)
        };
    }
}
