namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Shares a single <see cref="CustomWebApplicationFactory"/> (and therefore one Postgres
/// container and one booted host) across every integration test in the assembly.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}