namespace Schedulr.Console.Commands;

class LoginCommand(
  IAnsiConsole console,
  IAuthService authService,
  IProfileService profileService,
  IResourceManager resourceManager,
  ISettingsManager settingsManager
) : AsyncCommand
{
  readonly IAnsiConsole _console = console;
  readonly IAuthService _authService = authService;
  readonly IProfileService _profileService = profileService;
  readonly IResourceManager _resourceManager = resourceManager;
  readonly ISettingsManager _settingsManager = settingsManager;

  public override async Task<int> ExecuteAsync(CommandContext context)
  {
    var authUri = _authService.GetOAuthUri();

    Process.Start(new ProcessStartInfo
    {
      FileName = authUri,
      UseShellExecute = true
    });

    var successResponse = _resourceManager.GetResource("Success.html");
    var errorResponse = _resourceManager.GetResource("Failure.html");
    var responseHtml = successResponse;

    using var listener = new HttpListener();
    listener.Prefixes.Add(Constants.RedirectUri);
    listener.Start();

    var returnCode = 0;

    await _console.Status()
      .StartAsync("[bold]Waiting for login...[/]", async ctx =>
      {

        ctx.Spinner(Spinner.Known.Dots);

        var listenerContext = await listener.GetContextAsync();
        var oauthCode = listenerContext.Request.QueryString["code"];

        TokenResponse tokenResponse;

        try
        {
          tokenResponse = await _authService.GetTokenAsync(oauthCode);
          var userInfo = await _profileService.GetUserInfoAsync(tokenResponse.AccessToken);
          var settings = await _settingsManager.ReadAsync();
          var primaryName = userInfo.Names.FirstOrDefault(n => n.Metadata.Primary) ?? throw new LoginException("Failed to get primary name");
          var primaryEmail = userInfo.EmailAddresses.FirstOrDefault(e => e.Metadata.Primary) ?? throw new LoginException("Failed to get primary email");

          settings.AddAccount(new(primaryName.DisplayName, primaryEmail.Value, tokenResponse));

          await _settingsManager.WriteAsync(settings);

          _console.MarkupLine("[bold green]Successfully logged in![/]");
        }
        catch (Exception)
        {
          returnCode = 1;
          responseHtml = errorResponse;
          _console.MarkupLine("[bold red]Failed to log in![/]");
          throw;
        }
        finally
        {
          var buffer = Encoding.UTF8.GetBytes(responseHtml);
          listenerContext.Response.ContentLength64 = buffer.Length;
          await listenerContext.Response.OutputStream.WriteAsync(buffer);
          listenerContext.Response.Close();
          listener.Stop();
        }
      });

    return returnCode;
  }
}