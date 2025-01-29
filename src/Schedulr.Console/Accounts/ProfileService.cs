namespace Schedulr.Console.Accounts;

class ProfileService(HttpClient client, ISerializer serializer) : IProfileService
{
  const string BaseUri = "https://people.googleapis.com/v1/people/me";
  readonly HttpClient _client = client;
  readonly ISerializer _serializer = serializer;

  public async Task<PersonResponse> GetUserInfoAsync(string accessToken)
  {
    const string userInfoUri = $"{BaseUri}?personFields=names";
    using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUri);
    userInfoRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

    var userInfoResponse = await _client.SendAsync(userInfoRequest);
    var userInfoResponseContent = await userInfoResponse.Content.ReadAsStringAsync();

    if (userInfoResponse.IsSuccessStatusCode is false)
    {
      throw new LoginException("Failed to get user info");
    }

    return _serializer.Deserialize<PersonResponse>(userInfoResponseContent) ?? throw new LoginException("Failed to parse user info");
  }
}