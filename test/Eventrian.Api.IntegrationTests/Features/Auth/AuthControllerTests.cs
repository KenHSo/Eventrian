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
    public async Task Can_Register_Successfully()
    {
        var registerDto = new RegisterRequestDto
        {
            Email = "newuser@example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Register failed ({(int)response.StatusCode}): {body}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


}
