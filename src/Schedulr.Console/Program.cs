
using System.Buffers.Text;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Schedulr Console Application");

var host = Host.CreateDefaultBuilder().Build();
var config = host.Services.GetRequiredService<IConfiguration>();

var privateKeyFilePath = config["PrivateKeyFilePath"];
var calendarId = config["TestCalendarId"];

if (string.IsNullOrWhiteSpace(privateKeyFilePath))
{
  Console.WriteLine("Private key file path is not set.");
  return;
}

if (string.IsNullOrWhiteSpace(calendarId))
{
  Console.WriteLine("Calendar Id is not set.");
  return;
}

var privateKeyFileContent = await File.ReadAllTextAsync(privateKeyFilePath);

var serviceAccount = JsonSerializer.Deserialize<ServiceAccount>(privateKeyFileContent, JsonOptions.Default);

if (serviceAccount is null)
{
  Console.WriteLine("Service account is not valid.");
  return;
}

var jwtHeader = new JWTHeader(serviceAccount.PrivateKeyId);
var jwtClaims = new JWTClaims(serviceAccount.ClientEmail);
var jwt = new JWT(jwtHeader, jwtClaims);
var jwtToken = jwt.CreateSignedToken(serviceAccount.PrivateKey);

var httpClient = new HttpClient();

var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
{
  ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
  ["assertion"] = jwtToken,
});

var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);

if (tokenResponse.IsSuccessStatusCode is false)
{
  Console.WriteLine("Failed to get access token.");
  return;
}

var tokenResponseContent = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

if (tokenResponseContent is null)
{
  Console.WriteLine("Failed to read token response content.");
  return;
}

httpClient.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponseContent.AccessToken);

var addCalendarRequest = new StringContent(JsonSerializer.Serialize(new
{
  id = calendarId,
}), Encoding.UTF8, "application/json");

var addCalendarResponse = await httpClient.PostAsync("https://www.googleapis.com/calendar/v3/users/me/calendarList", addCalendarRequest);

if (addCalendarResponse.IsSuccessStatusCode is false)
{
  Console.WriteLine("Failed to add calendar.");
  Console.WriteLine(addCalendarResponse.StatusCode);
  var errorContent = await addCalendarResponse.Content.ReadAsStringAsync();
  Console.WriteLine(errorContent);
  return;
}

var getCalendarsResponse = await httpClient.GetAsync("https://www.googleapis.com/calendar/v3/users/me/calendarList");

if (getCalendarsResponse.IsSuccessStatusCode is false)
{
  Console.WriteLine("Failed to get calendars: {0}", getCalendarsResponse.StatusCode);

  var errorContent = await getCalendarsResponse.Content.ReadAsStringAsync();
  Console.WriteLine(errorContent);

  return;
}

var calendarsResponseContent = await getCalendarsResponse.Content.ReadAsStringAsync();

Console.WriteLine(calendarsResponseContent);

var getCalendarEventsResponse = await httpClient.GetAsync($"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events");

if (getCalendarEventsResponse.IsSuccessStatusCode is false)
{
  Console.WriteLine("Failed to get calendar events: {0}", getCalendarEventsResponse.StatusCode);

  var errorContent = await getCalendarEventsResponse.Content.ReadAsStringAsync();
  Console.WriteLine(errorContent);

  return;
}

var calendarEventsResponseContent = await getCalendarEventsResponse.Content.ReadAsStringAsync();

Console.WriteLine(calendarEventsResponseContent);

record TokenResponse(
  [property: JsonPropertyName("access_token")]
  string AccessToken,
  [property: JsonPropertyName("scope")]
  string Scope,
  [property: JsonPropertyName("expires_in")]
  int ExpiresIn,
  [property: JsonPropertyName("token_type")]
  string TokenType
);

record ServiceAccount(
  [property: JsonPropertyName("client_email")]
  string ClientEmail,
  [property: JsonPropertyName("private_key_id")]
  string PrivateKeyId,
  [property: JsonPropertyName("private_key")]
  string PrivateKey
);

record JWT(JWTHeader Header, JWTClaims Claims)
{
  public string CreateSignedToken(string privateKey)
  {
    var header = Header.ToBase64UrlEncodedString();
    var claims = Claims.ToBase64UrlEncodedString();
    var unsignedToken = $"{header}.{claims}";

    var rsa = RSA.Create();
    rsa.ImportFromPem(privateKey);

    var signature = rsa.SignData(
      Encoding.UTF8.GetBytes(unsignedToken),
      HashAlgorithmName.SHA256,
      RSASignaturePadding.Pkcs1
    );

    var signatureBase64UrlEncoded = Base64Url.EncodeToString(signature);

    return $"{unsignedToken}.{signatureBase64UrlEncoded}";
  }
}

record JWTHeader(string KeyId) : EncodedJWTComponent
{
  [JsonPropertyName("alg")]
  public string Algorithm { get; } = "RS256";

  [JsonPropertyName("typ")]
  public string Type { get; } = "JWT";

  [JsonPropertyName("kid")]
  public string KeyId { get; init; } = KeyId;
}

record JWTClaims(
  [property: JsonPropertyName("iss")]
  string Issuer
) : EncodedJWTComponent()
{
  public string Scope { get; } = "https://www.googleapis.com/auth/calendar";

  [JsonPropertyName("aud")]
  public string Audience { get; } = "https://oauth2.googleapis.com/token";

  [JsonPropertyName("exp")]
  public long Expiry { get; } = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();

  [JsonPropertyName("iat")]
  public long IssuedAt { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

abstract record EncodedJWTComponent
{
  public virtual string ToBase64UrlEncodedString()
  {
    var json = JsonSerializer.Serialize(this, JsonOptions.Default);
    var bytes = Encoding.UTF8.GetBytes(json);
    return Base64Url.EncodeToString(bytes);
  }
}

static class JsonOptions
{
  public static JsonSerializerOptions Default = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
  };
}