namespace Schedulr.Console.Commands;

class LoginCommand(
  IAnsiConsole console,
  IAuthService authService,
  IResourceManager resourceManager
) : AsyncCommand
{
  readonly IAnsiConsole _console = console;
  readonly IAuthService _authService = authService;
  readonly IResourceManager _resourceManager = resourceManager;

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
          // TODO: Save tokenResponse to a file
          _console.MarkupLine("[bold green]Successfully logged in![/]");
        }
        catch (Exception ex) when (ex is LoginException)
        {
          returnCode = 1;
          responseHtml = errorResponse;
          _console.MarkupLine("[bold red]Failed to log in![/]");
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
