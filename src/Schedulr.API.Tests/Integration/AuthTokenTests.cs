namespace Schedulr.API.Tests.Integration;

public class AuthTokenTests : IntegrationTest
{
  public AuthTokenTests(AppFactory factory) : base(factory)
  {
    Factory.MockHandler.Clear();
  }

  [Fact]
  public async Task AuthTokenEndpoint_WhenCalledWithValidRequestButGoogleReturnsNullResponse_ItShouldReturnProblemDetailWith500StatusCode()
  {
    var uri = new Uri("/auth/token", UriKind.Relative);
    using var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
      ["client_id"] = "client_id",
      ["redirect_uri"] = "redirect_uri",
      ["grant_type"] = "grant_type",
      ["code"] = "code"
    });

    Factory.MockHandler
      .When(HttpMethod.Post, "https://oauth2.googleapis.com/token")
      .Respond(HttpStatusCode.OK, "application/json", "null");

    var response = await Client.PostAsync(uri, content);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    problem!.Detail.Should().Be("Failed to parse OAuth token");
  }

  [Fact]
  public async Task AuthTokenEndpoint_WhenCalledWithValidRequestButGoogleReturnsError_ItShouldReturnProblemDetailWith500StatusCode()
  {
    var uri = new Uri("/auth/token", UriKind.Relative);
    using var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
      ["client_id"] = "client_id",
      ["redirect_uri"] = "redirect_uri",
      ["grant_type"] = "grant_type",
      ["code"] = "code",
    });

    Factory.MockHandler
      .When(HttpMethod.Post, "https://oauth2.googleapis.com/token")
      .Respond(HttpStatusCode.BadRequest);

    var response = await Client.PostAsync(uri, content);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    problem!.Detail.Should().Be("Failed to get OAuth token");
  }

  [Theory]
  [ClassData(typeof(InvalidRequestTestData))]
  public async Task AuthTokenEndpoint_WhenCalledWithInvalidRequest_ItShouldReturnProblemDetailWith400StatusCode(IEnumerable<KeyValuePair<string, string>> formFields, Dictionary<string, string[]> expectedErrors)
  {
    var uri = new Uri("/auth/token", UriKind.Relative);
    using var content = new FormUrlEncodedContent(formFields);

    var response = await Client.PostAsync(uri, content);
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    problem!.Errors.Should().BeEquivalentTo(expectedErrors);
  }

  [Theory]
  [ClassData(typeof(ValidRequestTestData))]
  public async Task AuthTokenEndpoint_WhenCalledWithValidRequest_ItShouldReturnOk(IEnumerable<KeyValuePair<string, string>> formFields)
  {
    var mockTokenResponse = new TokenResponse(
      "access_token",
      3600,
      "Bearer",
      "scope",
      "refresh_token"
    );

    Factory.MockHandler
      .When(HttpMethod.Post, "https://oauth2.googleapis.com/token")
      .Respond(
        "application/json",
        JsonSerializer.Serialize(mockTokenResponse)
      );

    var uri = new Uri("/auth/token", UriKind.Relative);
    using var content = new FormUrlEncodedContent(formFields);

    var response = await Client.PostAsync(uri, content);
    var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    tokenResponse.Should().BeEquivalentTo(mockTokenResponse);
  }

  class ValidRequestTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new Dictionary<string, string>
        {
          ["client_id"] = "client_id",
          ["redirect_uri"] = "redirect_uri",
          ["grant_type"] = "grant_type",
          ["code"] = "code"
        }
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }

  class InvalidRequestTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new Dictionary<string, string>
        {
          ["client_id"] = "",
          ["redirect_uri"] = "",
          ["grant_type"] = "",
          ["code"] = ""
        },
        new Dictionary<string, string[]>
        {
          ["ClientId"] = ["ClientId is required"],
          ["RedirectUri"] = ["RedirectUri is required"],
          ["GrantType"] = ["GrantType is required"],
          ["Code"] = ["Code is required"]
        }
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}