using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Models;
using Eventrian.Api.Features.Auth.Repository;
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

    private static RefreshTokenService CreateRefreshTokenService(AppDbContext dbContext)
    {
        var refreshTokenRepo = new RefreshTokenRepository(dbContext);
        var logger = Mock.Of<ILogger<RefreshTokenService>>();
        
        return new RefreshTokenService(refreshTokenRepo, logger);
    }


    // Creates and stores a token (positive case)
    [Fact]
    public async Task IssueRefreshTokenAsync_CreatesAndReturnsToken()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);

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

    [Fact]
    public async Task GetUserIdForToken_ReturnsUserId_IfTokenIsValid()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);

        var userId = "user-123";
        var token = await service.IssueRefreshTokenAsync(userId, isPersistent: false);

        // Act
        var result = await service.GetUserIdForToken(token);

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetUserIdForToken_ReturnsNull_IfTokenIsInvalid()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);

        // Act
        var result = await service.GetUserIdForToken("nonexistent-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_ReturnsNewToken_WhenTokenIsValid()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);

        var userId = "valid-user";
        var originalToken = await service.IssueRefreshTokenAsync(userId, isPersistent: false);

        // Act
        var result = await service.ValidateAndRotateAsync(originalToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.NewRefreshToken);
        Assert.Equal(userId, result.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.NewRefreshToken));
        Assert.NotEqual(originalToken, result.NewRefreshToken);
    }

    [Fact]
    public async Task RevokeRefreshTokensAsync_RemovesToken_WhenTokenExists()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);

        var userId = "user-1";
        var token = await service.IssueRefreshTokenAsync(userId, isPersistent: false);

        // Act
        await service.RevokeRefreshTokensAsync(token);

        // Assert
        var exists = await dbContext.RefreshTokens.AnyAsync(t => t.Token == token);
        Assert.False(exists);
    }

    [Fact]
    public async Task RevokeRefreshTokensAsync_DoesNothing_WhenTokenDoesNotExist()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);

        var fakeToken = "nonexistent-token";

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.RevokeRefreshTokensAsync(fakeToken));

        // Assert
        Assert.Null(exception);
    }

    // --- Dev Tasks ---

    [Fact]
    public async Task RunStartupCleanupAsync_RemovesExpiredAndUsedTokens_AndKeepsValidOnes()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);
        
        var now = DateTime.UtcNow;

        // Token 1: expired
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = "user1",
            Token = "expired-token",
            CreatedAt = now.AddDays(-10),
            ExpiresAt = now.AddDays(-1),
            IsPersistent = false
        });

        // Token 2: used and older than overlap window
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = "user2",
            Token = "used-old-token",
            CreatedAt = now.AddMinutes(-10),
            ExpiresAt = now.AddMinutes(10),
            UsedAt = now.AddMinutes(-5), // overlap window is 2 min
            IsPersistent = false
        });

        // Token 3: valid, unexpired, unused
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = "user3",
            Token = "valid-token",
            CreatedAt = now,
            ExpiresAt = now.AddDays(1),
            IsPersistent = false
        });

        await dbContext.SaveChangesAsync();

        // Act
        await service.RunStartupCleanupAsync();

        // Assert
        var tokens = await dbContext.RefreshTokens.Select(t => t.Token).ToListAsync();
        Assert.Contains("valid-token", tokens);
        Assert.DoesNotContain("expired-token", tokens);
        Assert.DoesNotContain("used-old-token", tokens);
    }

    [Fact]
    public async Task RunDevTokenCapCleanupAsync_RemovesOldestTokens_WhenOverLimit()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = CreateRefreshTokenService(dbContext);

        var userId = "overloaded-user";
        var now = DateTime.UtcNow;

        // Add 15 tokens for the same user (oldest first)
        for (int i = 0; i < 15; i++)
        {
            dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                Token = $"token-{i}",
                CreatedAt = now.AddMinutes(-i), // newer tokens have lower index
                ExpiresAt = now.AddDays(1),
                IsPersistent = false
            });
        }

        await dbContext.SaveChangesAsync();

        // Act
        await service.RunDevTokenCapCleanupAsync();

        // Assert
        var userTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        Assert.Equal(10, userTokens.Count);

        // Ensure the 10 most recent are kept (token-0 to token-9)
        var remainingTokens = userTokens.Select(t => t.Token).ToList();
        for (int i = 0; i < 10; i++)
            Assert.Contains($"token-{i}", remainingTokens);

        // Ensure oldest tokens (token-10 to token-14) are removed
        for (int i = 10; i < 15; i++)
            Assert.DoesNotContain($"token-{i}", remainingTokens);
    }

}
