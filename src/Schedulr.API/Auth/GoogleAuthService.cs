namespace Schedulr.API.Auth;

class GoogleAuthService(HttpClient client, GoogleOptions options) : IGoogleAuthService
{
  const string TokenUri = "https://oauth2.googleapis.com/token";
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
        ["grant_type"] = request.GrantType,
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