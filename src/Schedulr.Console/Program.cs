
using System.Buffers.Text;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Spectre.Console;
using Spectre.Console.Cli;

Console.WriteLine("Schedulr Console Application");

var host = Host.CreateDefaultBuilder()
  .ConfigureServices((_, services) =>
  {
    services.AddSingleton(AnsiConsole.Console);
  })
  .Build();

class LoginCommand(IAnsiConsole console, IConfiguration configuration) : Command
{
  private readonly IAnsiConsole _console = console;
  private readonly IConfiguration _configuration = configuration;

  public override int Execute(CommandContext context)
  {
    var googleConfig = _configuration.GetSection("Google");
    var redirectUri = googleConfig["RedirectUri"];
    var clientId = googleConfig["ClientId"];
    var clientSecret = googleConfig["ClientSecret"];

    var baseAuthUri = "https://accounts.google.com/o/oauth2/v2/auth";
    var authUriQueryParams = new Dictionary<string, string>
    {
      ["client_id"] = clientId!,
      ["redirect_uri"] = redirectUri!,
      ["response_type"] = "code",
      ["scope"] = "https://www.googleapis.com/auth/calendar",
      ["access_type"] = "offline"
    };

    var authUri = $"{baseAuthUri}?{string.Join("&", authUriQueryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"))}";

    Process.Start(new ProcessStartInfo
    {
      FileName = authUri,
      UseShellExecute = true
    });

    var oauthCode = string.Empty;

    _console.Status()
      .Start("[bold]Waiting for login...[/]", ctx =>
      {
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri!);
        listener.Start();

        ctx.Spinner(Spinner.Known.Dots);

        var listenerContext = listener.GetContext();
        oauthCode = listenerContext.Request.QueryString["code"];

        if (!string.IsNullOrEmpty(oauthCode))
        {
          // TODO: Proper HTML response
          string responseHtml = @"
                <html>
                <body>
                    <script>
                        alert('Login successful! You can close this window now.');
                    </script>
                </body>
                </html>";
          byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
          listenerContext.Response.ContentLength64 = buffer.Length;
          listenerContext.Response.OutputStream.Write(buffer, 0, buffer.Length);
          listenerContext.Response.Close();
        }

        listener.Stop();

        _console.MarkupLine($"[bold]Received OAuth code: {oauthCode}[/]");
      });

    if (string.IsNullOrEmpty(oauthCode))
    {
      _console.MarkupLine("[bold red]Failed to get OAuth code[/]");
      return 1;
    }

    // TODO: This probably needs to be a request to
    // my own server to prevent asking people to use their
    // own clients.
    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
    {
      Content = new FormUrlEncodedContent(new Dictionary<string, string>
      {
        ["code"] = oauthCode,
        ["client_id"] = clientId!,
        ["client_secret"] = clientSecret!,
        ["redirect_uri"] = redirectUri!,
        ["grant_type"] = "authorization_code"
      })
    };

    var tokenResponse = new HttpClient().Send(tokenRequest);

    if (!tokenResponse.IsSuccessStatusCode)
    {
      _console.MarkupLine("[bold red]Failed to get OAuth token[/]");
      return 1;
    }

    // TODO: Parse and persist tokens
    var tokenResponseJson = tokenResponse.Content.ReadAsStringAsync().Result;

    _console.MarkupLine($"[bold]OAuth token response: {tokenResponseJson}[/]");

    return 0;
  }
}