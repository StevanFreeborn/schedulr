try
{
  await Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(static (context, config) =>
    {
      config.SetBasePath(AppContext.BaseDirectory);
      config.AddJsonFile(path: "appsettings.json", optional: true);
    })
    .ConfigureLogging(static logging => logging.ClearProviders())
    .ConfigureServices(static (_, services) =>
      {
        services.AddSingleton(AnsiConsole.Console);
        services.AddSingleton<ISerializer, Serializer>();
        services.AddSingleton<ISettingsManager, SettingsManager>();

        services.AddHttpClient();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IResourceManager, ResourceManager>();
      })
    .BuildApp()
    .RunAsync(args);
}
catch (Exception ex) when (ex is SchedulrException)
{
  AnsiConsole.MarkupLine("[bold red]An error occurred:[/]");
  AnsiConsole.MarkupLine(CultureInfo.InvariantCulture, "[bold red]{Message}[/]", ex.Message);
}