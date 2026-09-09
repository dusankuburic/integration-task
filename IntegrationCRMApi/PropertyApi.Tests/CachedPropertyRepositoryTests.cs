using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PropertyApi.Dataverse;
using PropertyApi.Models;
using Xunit;

namespace PropertyApi.Tests;

public class CachedPropertyRepositoryTests
{
    [Fact]
    public async Task Second_read_does_not_reach_the_inner_repository()
    {
        var inner = InnerRepository();
        var repository = new CachedPropertyRepository(inner.Object, NewCache());

        await repository.GetActiveAsync(CancellationToken.None);
        await repository.GetActiveAsync(CancellationToken.None);

        inner.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task The_cache_is_shared_across_repository_instances()
    {
        var inner = InnerRepository();
        var cache = NewCache();

        await new CachedPropertyRepository(inner.Object, cache).GetActiveAsync(CancellationToken.None);
        await new CachedPropertyRepository(inner.Object, cache).GetActiveAsync(CancellationToken.None);

        inner.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_what_the_inner_repository_returned()
    {
        var repository = new CachedPropertyRepository(InnerRepository().Object, NewCache());

        var first = await repository.GetActiveAsync(CancellationToken.None);
        var second = await repository.GetActiveAsync(CancellationToken.None);

        Assert.Equal(2, first.Count);
        Assert.Equal("Villa 0", first[0].Name);
        Assert.Equal("Villa 1", first[1].Name);

        Assert.Equal(2, second.Count);
        Assert.Equal("Villa 0", second[0].Name);
        Assert.Equal("Villa 1", second[1].Name);
    }


    private static Mock<IPropertyRepository> InnerRepository()
    {
        IReadOnlyList<Property> properties = [
            new Property { Name = "Villa 0" },
            new Property { Name = "Villa 1" }
        ];

        var repository = new Mock<IPropertyRepository>();

        repository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(properties);

        return repository;
    }

    private static HybridCache NewCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();

        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}
