namespace Schedulr.API.Tests.Integration;

public class AuthTokenTests(AppFactory factory) : IntegrationTest(factory)
{
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
    var uri = new Uri("/auth/token", UriKind.Relative);
    using var content = new FormUrlEncodedContent(formFields);

    var response = await Client.PostAsync(uri, content);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
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