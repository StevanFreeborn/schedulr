using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.ConfigureOptions<GoogleOptionsSetup>();
builder.Services.AddSingleton(static sp => sp.GetRequiredService<IOptions<GoogleOptions>>().Value);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseStatusCodePages();

app.UseHttpsRedirection();

app
  .MapPost("/auth/token", static async ([AsParameters] AuthTokenRequest request, [FromServices] IGoogleAuthService authService) =>
  {
    var validationResults = request.Validate(new ValidationContext(request));

    if (validationResults.Any())
    {
      return Results.ValidationProblem(validationResults.ToErrors());
    }

    var token = await authService.GetAccessToken(request);

    return Results.Ok(token);
  })
  .DisableAntiforgery();

app.Run();

interface IGoogleAuthService
{
  Task<TokenResponse> GetAccessToken(AuthTokenRequest request);
}

class GoogleAuthService(HttpClient client, GoogleOptions options) : IGoogleAuthService
{
  const string TokenUri = "https://oauth2.googleapis.com/token";
  const string GrantType = "authorization_code";
  readonly HttpClient _client = client;
  readonly GoogleOptions _options = options;


  public async Task<TokenResponse> GetAccessToken(AuthTokenRequest request)
  {
    using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUri)
    {
      Content = new FormUrlEncodedContent(new Dictionary<string, string?>
      {
        ["code"] = request.Code,
        ["client_id"] = request.ClientId,
        ["client_secret"] = _options.ClientSecret,
        ["redirect_uri"] = request.RedirectUri,
        ["grant_type"] = GrantType
      })
    };

    var tokenResponse = await _client.SendAsync(tokenRequest);

    if (tokenResponse.IsSuccessStatusCode is false)
    {
      throw new InvalidOperationException("Failed to get OAuth token");
    }

    var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

    return token ?? throw new InvalidOperationException("Failed to parse OAuth token");
  }
}

class GoogleOptionsSetup(IConfiguration config) : IConfigureOptions<GoogleOptions>
{
  const string SectionName = nameof(GoogleOptions);
  readonly IConfiguration _config = config;

  public void Configure(GoogleOptions options)
  {
    _config.GetSection(SectionName).Bind(options);
  }
}

class GoogleOptions
{
  public string ClientSecret { get; set; } = string.Empty;
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

record AuthTokenRequest(
  [FromForm(Name = "client_id")]
  string ClientId,
  [FromForm(Name = "redirect_uri")]
  string RedirectUri,
  [FromForm(Name = "grant_type")]
  string GrantType,
  [FromForm(Name = "code")]
  string Code
) : IValidatableObject
{
  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (string.IsNullOrWhiteSpace(ClientId))
    {
      yield return new ValidationResult($"{nameof(ClientId)} is required", [nameof(ClientId)]);
    }

    if (string.IsNullOrWhiteSpace(RedirectUri))
    {
      yield return new ValidationResult($"{nameof(RedirectUri)} is required", [nameof(RedirectUri)]);
    }

    if (string.IsNullOrWhiteSpace(GrantType))
    {
      yield return new ValidationResult($"{nameof(GrantType)} is required", [nameof(GrantType)]);
    }

    if (string.IsNullOrWhiteSpace(Code))
    {
      yield return new ValidationResult($"{nameof(Code)} is required", [nameof(Code)]);
    }
  }
}

static class ValidationResultsExtensions
{
  public static Dictionary<string, string[]> ToErrors(this IEnumerable<ValidationResult> results)
  {
    return results
      .GroupBy(static r => r.MemberNames.First())
      .ToDictionary(static g => g.Key, static g => g.Select(static r => r.ErrorMessage!).ToArray());
  }
}

[ExcludeFromCodeCoverage]
public partial class Program { }