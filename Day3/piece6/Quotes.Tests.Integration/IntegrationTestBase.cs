using Quotes.Tests.Integration.Infrastructure;

namespace Quotes.Tests.Integration;

/// <summary>
/// Base class that owns one QuotesApiFactory + HttpClient per test.
/// xUnit constructs a fresh test-class instance for each [Fact], so each test
/// automatically gets its own factory, its own temp SQLite file, and therefore
/// a completely isolated database state.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly QuotesApiFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase()
    {
        Factory = new QuotesApiFactory();
        Client  = Factory.CreateClient();
    }

    public void Dispose() => Factory.Dispose();
}
