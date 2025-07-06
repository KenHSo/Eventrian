using Eventrian.Client.Features.Auth.Services;
using Xunit;

namespace Eventrian.Client.UnitTests.Features.Auth;

public class AccessTokenStorageTests
{
    [Fact]
    public void GetAccessToken_ReturnsNull_ByDefault()
    {
        // Arrange
        var storage = new AccessTokenStorage();

        // Act
        var token = storage.GetAccessToken();

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void SetAccessToken_DoesNotStore_WhenTokenUpdatesBlocked()
    {
        // Arrange
        var storage = new AccessTokenStorage();

        // Act
        storage.SetAccessToken("blocked-token"); // Default: _tokenUpdatesBlocked = true
        var token = storage.GetAccessToken();

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void SetAccessToken_Stores_WhenAllowed()
    {
        // Arrange
        var storage = new AccessTokenStorage();
        storage.AllowTokenUpdates();

        // Act
        storage.SetAccessToken("valid-token");
        var token = storage.GetAccessToken();

        // Assert
        Assert.Equal("valid-token", token);
    }

    [Fact]
    public void SetAccessToken_Ignores_WhenTokenIsNullOrWhitespace()
    {
        // Arrange
        var storage = new AccessTokenStorage();
        storage.AllowTokenUpdates();

        // Act
        storage.SetAccessToken("   "); // Invalid
        var token = storage.GetAccessToken();

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void ClearAccessToken_RemovesToken()
    {
        // Arrange
        var storage = new AccessTokenStorage();
        storage.AllowTokenUpdates();
        storage.SetAccessToken("valid-token");

        // Act
        storage.ClearAccessToken();
        var token = storage.GetAccessToken();

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void BlockTokenUpdates_PreventsFutureChanges()
    {
        // Arrange
        var storage = new AccessTokenStorage();
        storage.AllowTokenUpdates();
        storage.SetAccessToken("allowed");

        storage.BlockTokenUpdates();

        // Act
        storage.SetAccessToken("blocked");
        var token = storage.GetAccessToken();

        // Assert
        Assert.Equal("allowed", token); // Should remain unchanged
    }
}
