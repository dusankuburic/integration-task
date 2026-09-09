using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyApi.Dataverse;
using PropertyApi.Exceptions;
using PropertyApi.Models;
using Xunit;

namespace PropertyApi.Tests;

public class GetActivePropertiesTests
{
    [Fact]
    public async Task Returns_200_with_the_active_properties()
    {
        IReadOnlyList<Property> properties = [
            new Property { Name = "Villa 0" },
            new Property { Name = "Villa 1" }
        ];

        var function = FunctionReturning(properties);

        var result = await function.Run(Request(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<Property>>(ok.Value);

        Assert.Equal(new[] { "Villa 0", "Villa 1" }, body.Select(p => p.Name));
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

    private static GetActiveProperties FunctionReturning(IReadOnlyList<Property> properties)
    {
        var repository = new Mock<IPropertyRepository>();

        repository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(properties);

        return Function(repository.Object);
    }

    private static GetActiveProperties FunctionThrowing(Exception exception)
    {
        var repository = new Mock<IPropertyRepository>();

        repository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        return Function(repository.Object);
    }

    private static GetActiveProperties Function(IPropertyRepository repository) =>
        new(repository, NullLogger<GetActiveProperties>.Instance);

    private static HttpRequest Request() => new DefaultHttpContext().Request;
}
