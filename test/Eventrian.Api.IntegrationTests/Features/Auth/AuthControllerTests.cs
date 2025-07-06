using Eventrian.Api.IntegrationTests.Setup;
using Eventrian.Shared.Dtos.Auth;
using System.Net;
using System.Net.Http.Json;

namespace Eventrian.Api.IntegrationTests.Features.Auth;

public class AuthControllerTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldSucceed_WithValidData()
    {
        // Arrange
        var email = $"user_{Guid.NewGuid()}@example.com";
        var registerDto = new RegisterRequestDto
        {
            Email = email,
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldFail_WhenEmailAlreadyExists()
    {
        // Arrange
        var dto = new RegisterRequestDto
        {
            Email = "dupe@example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var first = await _client.PostAsJsonAsync("/api/auth/register", dto);
        var second = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnTokens_WithValidCredentials()
    {
        // Arrange
        var email = $"user_{Guid.NewGuid()}@example.com";
        var password = "Secure123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Login",
            LastName = "User"
        });

        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(tokens?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens?.RefreshToken));
    }

    [Fact]
    public async Task Login_ShouldReturn401_WithInvalidCredentials()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = "wrong@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldReturnNewAccessToken_WithValidRefreshToken()
    {
        // Arrange
        var email = $"user_{Guid.NewGuid()}@example.com";
        var password = "Refresh123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Refresh",
            LastName = "User"
        });

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        var tokens = await login.Content.ReadFromJsonAsync<LoginResponseDto>();

        // Act
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = tokens!.RefreshToken });
        var newTokens = await refreshResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotEqual(tokens.AccessToken, newTokens?.AccessToken);
    }

}
