using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;

namespace Eventrian.Client.UnitTests.Helpers;

public static class HttpMockExtensions
{
    public static ISetup<HttpMessageHandler, Task<HttpResponseMessage>> SetupRequest(
        this Mock<HttpMessageHandler> handler,
        HttpMethod method,
        string url)
    {
        return handler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == method && req.RequestUri!.ToString() == url),
            ItExpr.IsAny<CancellationToken>());
    }

    public static void ReturnsJson<T>(this ISetup<HttpMessageHandler, Task<HttpResponseMessage>> setup, T content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(content)
        };
        setup.ReturnsAsync(response);
    }

    public static void VerifyNoOutstandingRequest(this Mock<HttpMessageHandler> handler)
    {
        handler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
