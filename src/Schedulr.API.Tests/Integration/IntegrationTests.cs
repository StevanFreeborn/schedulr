namespace Schedulr.API.Tests.Integration;

public class IntegrationTest(AppFactory factory) : IClassFixture<AppFactory>
{
  protected HttpClient Client { get; } = factory.CreateClient();
}