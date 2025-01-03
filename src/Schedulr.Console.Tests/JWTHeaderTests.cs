namespace Schedulr.Console.Tests;

public class JWTHeaderTests
{
  [Fact]
  public void ToBase64UrlEncodedString_ReturnsExpectedValue()
  {
    var header = new JWTHeader("test");
    var actual = header.ToBase64UrlEncodedString();
    var expected = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InRlc3QifQ";
    actual.Should().Be(expected);
  }
}