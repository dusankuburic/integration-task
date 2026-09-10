using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PropertyApi.Common;
using PropertyApi.Dataverse;
using PropertyApi.Models;
using Xunit;

namespace PropertyApi.Tests;

public class CachedPropertyRepositoryTests
{
    [Fact]
    public async Task The_count_is_read_once_and_shared_by_every_page()
    {
        var inner = InnerRepository();
        var repository = new CachedPropertyRepository(inner.Object, NewCache(), CountLifetime);

        await repository.GetActiveAsync(Page(1), CancellationToken.None);
        var first = await repository.CountActiveAsync(CancellationToken.None);

        await repository.GetActiveAsync(Page(2), CancellationToken.None);
        var second = await repository.CountActiveAsync(CancellationToken.None);

        Assert.Equal(80115, first);
        Assert.Equal(80115, second);

        inner.Verify(r => r.CountActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task The_cache_is_shared_across_repository_instances()
    {
        var inner = InnerRepository();
        var cache = NewCache();

        await new CachedPropertyRepository(inner.Object, cache, CountLifetime).CountActiveAsync(CancellationToken.None);
        await new CachedPropertyRepository(inner.Object, cache, CountLifetime).CountActiveAsync(CancellationToken.None);

        inner.Verify(r => r.CountActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pages_are_not_cached_here()
    {
        var inner = InnerRepository();
        var repository = new CachedPropertyRepository(inner.Object, NewCache(), CountLifetime);

        await repository.GetActiveAsync(Page(1), CancellationToken.None);
        await repository.GetActiveAsync(Page(1), CancellationToken.None);

        inner.Verify(
            r => r.GetActiveAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static readonly TimeSpan CountLifetime = TimeSpan.FromMinutes(5);

    private static PageRequest Page(int number) => new(number, 10);

    private static Mock<IPropertyRepository> InnerRepository()
    {
        var repository = new Mock<IPropertyRepository>();

        repository
            .Setup(r => r.GetActiveAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PageRequest page, CancellationToken _) => new PropertyPage {
                Items = [new Property { Name = $"Villa on page {page.Page}" }],
                HasMore = true
            });

        repository
            .Setup(r => r.CountActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(80115);

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