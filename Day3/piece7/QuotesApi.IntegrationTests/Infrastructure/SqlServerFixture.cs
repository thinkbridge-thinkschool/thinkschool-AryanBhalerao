using Testcontainers.MsSql;

namespace QuotesApi.IntegrationTests.Infrastructure;

/// <summary>
/// Manages a single SQL Server 2022 container for the entire test collection.
/// Start cost is ~10-30 s on first pull; subsequent runs reuse cached layers.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
