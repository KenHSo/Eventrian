using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eventrian.Api.UnitTests.Features.Auth;

public class RefreshTokenServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task IssueRefreshTokenAsync_CreatesAndReturnsToken()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<RefreshTokenService>>();
        var service = new RefreshTokenService(dbContext, loggerMock.Object);

        var userId = "test-user-id";
        var isPersistent = true;

        // Act
        var token = await service.IssueRefreshTokenAsync(userId, isPersistent);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
        Assert.NotNull(storedToken);
        Assert.Equal(userId, storedToken.UserId);
        Assert.Equal(isPersistent, storedToken.IsPersistent);
    }
}
