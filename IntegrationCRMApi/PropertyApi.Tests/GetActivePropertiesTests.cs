using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyApi.Common;
using PropertyApi.Exceptions;
using PropertyApi.Models;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PropertyApi.Tests;

public class GetActivePropertiesTests
{
    [Fact]
    public async Task Returns_200_with_the_cached_json()
    {
        var function = FunctionReturning("{\"page\":2,\"pageSize\":2,\"total\":115}");

        var result = await function.Run(Request("?page=2&pageSize=2"), CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);

        Assert.Equal("application/json; charset=utf-8", file.ContentType);

        var body = JsonSerializer.Deserialize<PagedResult<Property>>(
            file.FileContents, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(2, body.Page);
        Assert.Equal(2, body.PageSize);
        Assert.Equal(115, body.Total);
    }

    [Fact]
    public async Task Asks_for_the_page_from_the_query_string()
    {
        var response = ResponseReturning("{}");
        var function = Function(response.Object);

        await function.Run(Request("?page=7&pageSize=25"), CancellationToken.None);

        response.Verify(
            r => r.GetActiveJsonAsync(
                It.Is<PageRequest>(p => p.Page == 7 && p.Size == 25),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Defaults_to_the_first_page()
    {
        var response = ResponseReturning("{}");
        var function = Function(response.Object);

        await function.Run(Request(), CancellationToken.None);

        response.Verify(
            r => r.GetActiveJsonAsync(
                It.Is<PageRequest>(p => p.Page == 1 && p.Size == PagingExtensions.DefaultPageSize),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Returns_400_when_the_page_size_is_over_the_limit()
    {
        var response = new Mock<IPropertyResponse>();
        var function = Function(response.Object);

        var result = await function.Run(Request("?pageSize=999999"), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);

        response.Verify(
            r => r.GetActiveJsonAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Returns_503_when_Dataverse_is_unavailable()
    {
        var function = FunctionThrowing(
            new DataverseUnavailableException("The Dataverse connection is not ready."));

        var result = await function.Run(Request(), CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal("The Dataverse connection is not ready.", problem.Detail);
    }

    [Fact]
    public async Task Returns_500_and_hides_the_error_detail()
    {
        var function = FunctionThrowing(new InvalidOperationException("connection string user=admin"));

        var result = await function.Run(Request(), CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public async Task Returns_499_when_the_caller_cancels()
    {
        var function = FunctionThrowing(new OperationCanceledException());

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await function.Run(Request(), cancellation.Token);

        var response = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, response.StatusCode);
    }

    private static Mock<IPropertyResponse> ResponseReturning(string json)
    {
        var response = new Mock<IPropertyResponse>();

        response
            .Setup(r => r.GetActiveJsonAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyPageJson { Json = Encoding.UTF8.GetBytes(json) });

        return response;
    }

    private static GetActiveProperties FunctionReturning(string json) =>
        Function(ResponseReturning(json).Object);

    private static GetActiveProperties FunctionThrowing(Exception exception)
    {
        var response = new Mock<IPropertyResponse>();

        response
            .Setup(r => r.GetActiveJsonAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        return Function(response.Object);
    }

    private static GetActiveProperties Function(IPropertyResponse response) =>
        new(response, NullLogger<GetActiveProperties>.Instance);

    private static HttpRequest Request(string queryString = null)
    {
        var request = new DefaultHttpContext().Request;

        if (queryString is not null) {
            request.QueryString = new QueryString(queryString);
        }

        return request;
    }
}
