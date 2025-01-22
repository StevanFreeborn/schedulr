using System.Data;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Schedulr.Console.Generated;

using Spectre.Console;
using Spectre.Console.Cli;

Console.WriteLine("Schedulr Console Application");

await Host.CreateDefaultBuilder()
  .ConfigureAppConfiguration(static (context, config) =>
  {
    config.SetBasePath(AppContext.BaseDirectory);
    config.AddJsonFile("appsettings.json");
  })
  .ConfigureServices(static (_, services) => services.AddSingleton(AnsiConsole.Console))
  .BuildApp()
  .RunAsync(args);

class LoginCommand(IAnsiConsole console, IConfiguration configuration) : Command
{
  readonly IAnsiConsole _console = console;
  readonly IConfiguration _configuration = configuration;

  public override int Execute(CommandContext context)
  {
    var googleConfig = _configuration.GetSection("Google");
    var redirectUri = googleConfig["RedirectUri"];
    var clientId = googleConfig["ClientId"];
    var clientSecret = googleConfig["ClientSecret"];

    if (string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
      _console.MarkupLine("[bold red]Google configuration is missing[/]");
      return 1;
    }

    var baseAuthUri = "https://accounts.google.com/o/oauth2/v2/auth";
    var authUriQueryParams = new Dictionary<string, string>
    {
      ["client_id"] = clientId,
      ["redirect_uri"] = redirectUri,
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
          var responseHtml = @"
                <html>
                <body>
                    <script>
                        alert('Login successful! You can close this window now.');
                    </script>
                </body>
                </html>";
          var buffer = Encoding.UTF8.GetBytes(responseHtml);
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

interface IGoogleAuthService
{
  string GetOAuthUri();
  Task<TokenResponse> GetTokenAsync(string code);
}

class GoogleAuthService : IGoogleAuthService
{
  const string BaseAuthUri = "https://accounts.google.com/o/oauth2/v2/auth";
  const string TokenUri = "https://oauth2.googleapis.com/token";
  const string Scope = "https://www.googleapis.com/auth/calendar";
  const string AccessType = "offline";
  const string GrantType = "authorization_code";
  const string ResponseType = "code";

  public string GetOAuthUri()
  {
    var authUriQueryParams = new Dictionary<string, string>
    {
      ["client_id"] = Constants.ClientId,
      ["redirect_uri"] = Constants.RedirectUri,
      ["response_type"] = ResponseType,
      ["scope"] = Scope,
      ["access_type"] = AccessType
    };
    var query = string.Join("&", authUriQueryParams.Select(static kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
    var authUri = $"{BaseAuthUri}?{query}";

    return authUri;
  }

  public Task<TokenResponse> GetTokenAsync(string code)
  {
    throw new NotImplementedException();
  }
}

record TokenResponse(
  [property: JsonPropertyName("access_token")]
  string AccessToken,
  [property: JsonPropertyName("expires_in")]
  int ExpiresIn,
  [property: JsonPropertyName("token_type")]
  string TokenType,
  [property: JsonPropertyName("scope")]
  string Scope,
  [property: JsonPropertyName("refresh_token")]
  string RefreshToken
);

static class HostBuilderExtensions
{
  public static CommandApp BuildApp(this IHostBuilder builder)
  {
    var registrar = new TypeRegistrar(builder);
    var app = new CommandApp(registrar);

    app.Configure(static c => c.AddCommand<LoginCommand>("login"));

    return app;
  }
}

class TypeRegistrar(IHostBuilder builder) : ITypeRegistrar
{
  readonly IHostBuilder _builder = builder;

  public ITypeResolver Build()
  {
    return new TypeResolver(_builder.Build());
  }

  public void Register(Type service, Type implementation)
  {
    _builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
  }

  public void RegisterInstance(Type service, object implementation)
  {
    _builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
  }

  public void RegisterLazy(Type service, Func<object> func)
  {
    ArgumentNullException.ThrowIfNull(func);

    _builder.ConfigureServices((_, services) => services.AddSingleton(service, _ => func()));
  }
}

class TypeResolver(IHost provider) : ITypeResolver, IDisposable
{
  readonly IHost _host = provider ?? throw new ArgumentNullException(nameof(provider));

  public object? Resolve(Type? type)
  {
    return type != null ? _host.Services.GetService(type) : null;
  }

  public void Dispose()
  {
    _host.Dispose();
  }
}