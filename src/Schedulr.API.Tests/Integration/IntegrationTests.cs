namespace Schedulr.API.Tests.Integration;

class IntegrationTest(AppFactory factory) : IClassFixture<AppFactory>
{
  protected HttpClient Client { get; } = factory.CreateClient();
}