namespace Schedulr.API.Tests.Integration;

public class AuthTokenTests(AppFactory factory) : IntegrationTest(factory)
{
  [Fact]
  public async Task AuthTokenEndpoint_WhenCalledWithoutRequiredInformation_ItShouldReturnProblemDetailWith400StatusCode()
  {
    var uri = new Uri("/auth/token", UriKind.Relative);
    using var content = new FormUrlEncodedContent([]);

    var response = await Client.PostAsync(uri, content);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    response.Content.As<ValidationProblemDetails>().Errors.Should().HaveCount(4);
  }
}