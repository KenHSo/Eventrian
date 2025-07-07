using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Features.Auth.Services;
using Microsoft.AspNetCore.Components;
using Moq;

namespace Eventrian.Client.UnitTests.Features.Auth;

public class UserSessionTerminatorTests
{
    [Fact]
    public async Task TerminateUserSessionAsync_CallsLogoutAndNavigatesToRoot()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockNavManager = new TestNavigationManager();

        var terminator = new UserSessionTerminator(mockAuthService.Object, mockNavManager);

        // Act
        await terminator.TerminateUserSessionAsync(fromBroadcast: true);

        // Assert
        mockAuthService.Verify(x => x.LogoutAsync(true), Times.Once);
        Assert.Equal("http://localhost/", mockNavManager.Uri); // Should have navigated to root
    }

    // Helper class to simulate NavigationManager
    private class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/initial");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}
