
try
{
  await Host.CreateDefaultBuilder()
    .ConfigureLogging(static logging => logging.ClearProviders())
    .ConfigureServices(static (_, services) =>
      {
        services.AddSingleton(AnsiConsole.Console);
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<ISerializer, Serializer>();
        services.AddSingleton<IEncryptor, Encryptor>();
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
  AnsiConsole.MarkupLine(CultureInfo.InvariantCulture, "[bold red]{StackTrace}[/]", ex.StackTrace ?? string.Empty);
}