namespace Schedulr.API.Tests.Integration;

public class AppFactory : WebApplicationFactory<Program>
{
  public MockHttpMessageHandler MockHandler { get; } = new();

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureLogging(static l => l.ClearProviders());

    builder.ConfigureTestServices(services => services.AddHttpClient(string.Empty)
      .ConfigurePrimaryHttpMessageHandler(() => MockHandler));
  }
}