namespace Schedulr.Console.Auth;

class AuthService(HttpClient client) : IAuthService
{
  const string BaseAuthUri = "https://accounts.google.com/o/oauth2/v2/auth";
  const string ClientId = Constants.ClientId;
  const string RedirectUri = Constants.RedirectUri;
  const string ResponseType = "code";
  const string Scope = "https://www.googleapis.com/auth/calendar";
  const string AccessType = "offline";
  const string BaseTokenUri = Constants.AuthUri;
  const string GrantType = "authorization_code";
  readonly HttpClient _client = client;


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

    var tokenResponse = await _client.SendAsync(tokenRequest);

    if (tokenResponse.IsSuccessStatusCode is false)
    {
      throw new LoginException("Failed to get OAuth token");
    }

    var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

    return token ?? throw new LoginException("Failed to parse OAuth token");
  }
}
