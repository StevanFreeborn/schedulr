using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Schedulr.Console.Generated;

using Spectre.Console;
using Spectre.Console.Cli;

try
{

  await Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(static (context, config) =>
    {
      config.SetBasePath(AppContext.BaseDirectory);
      config.AddJsonFile("appsettings.json");
    })
    .ConfigureServices(static (_, services) =>
      {
        services.AddSingleton(AnsiConsole.Console);
        services.AddSingleton<IAuthService, AuthService>();
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

interface IResourceManager
{
  string GetResource(string name);
}

class ResourceManager : IResourceManager
{
  public string GetResource(string name)
  {
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = assembly.GetName().Name + ".Resources." + name;
    using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}

interface IAuthService
{
  string GetOAuthUri();
  Task<TokenResponse> GetTokenAsync(string? code);
}

class AuthService : IAuthService
{
  const string BaseAuthUri = "https://accounts.google.com/o/oauth2/v2/auth";
  const string ClientId = Constants.ClientId;
  const string RedirectUri = Constants.RedirectUri;
  const string ResponseType = "code";
  const string Scope = "https://www.googleapis.com/auth/calendar";
  const string AccessType = "offline";

  const string BaseTokenUri = Constants.AuthUri;
  const string GrantType = "authorization_code";

  public string GetOAuthUri()
  {
    var authUriQueryParams = new Dictionary<string, string>
    {
      ["client_id"] = ClientId,
      ["redirect_uri"] = RedirectUri,
      ["response_type"] = ResponseType,
      ["scope"] = Scope,
      ["access_type"] = AccessType
    };
    var query = string.Join("&", authUriQueryParams.Select(static kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
    var authUri = $"{BaseAuthUri}?{query}";

    return authUri;
  }

  public async Task<TokenResponse> GetTokenAsync(string? code)
  {
    var uri = new Uri($"{BaseTokenUri}/auth/token", UriKind.Absolute);
    using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, uri)
    {
      Content = new FormUrlEncodedContent(new Dictionary<string, string?>
      {
        ["code"] = code,
        ["client_id"] = Constants.ClientId,
        ["redirect_uri"] = Constants.RedirectUri,
        ["grant_type"] = GrantType
      })
    };

    using var client = new HttpClient();
    var tokenResponse = await client.SendAsync(tokenRequest);

    if (tokenResponse.IsSuccessStatusCode is false)
    {
      throw new LoginException("Failed to get OAuth token");
    }

    var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

    return token ?? throw new LoginException("Failed to parse OAuth token");
  }
}

class LoginException : SchedulrException
{
  public LoginException()
  {
  }
  public LoginException(string message) : base(message)
  {
  }

  public LoginException(string message, Exception innerException) : base(message, innerException)
  {
  }
}

abstract class SchedulrException : Exception
{
  protected SchedulrException()
  {
  }

  protected SchedulrException(string message) : base(message)
  {
  }

  protected SchedulrException(string message, Exception innerException) : base(message, innerException)
  {
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
    return type is not null ? _host.Services.GetService(type) : null;
  }

  public void Dispose()
  {
    _host.Dispose();
  }
}