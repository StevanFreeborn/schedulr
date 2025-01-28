namespace Schedulr.API.Tests.Integration;

public class IntegrationTest(AppFactory factory) : IClassFixture<AppFactory>
{
  protected AppFactory Factory { get; } = factory;
  protected HttpClient Client { get; } = factory.CreateClient();
}