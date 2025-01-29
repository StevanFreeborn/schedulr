namespace Schedulr.API.Auth;

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
)
{
  const int ExpirationSkew = 60;
  public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
  public bool IsExpired => CreatedAt.AddSeconds(ExpiresIn) < DateTimeOffset.UtcNow + TimeSpan.FromSeconds(ExpirationSkew);
}