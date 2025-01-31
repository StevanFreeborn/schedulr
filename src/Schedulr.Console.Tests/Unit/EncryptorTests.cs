namespace Schedulr.Console.Tests.Unit;

public class EncryptorTests
{
  readonly Encryptor _sut = new();

  [Fact]
  public void EncryptAsync_WhenValueIsProvided_ReturnsEncryptedValue()
  {
    var value = "test";
    var key = "key";

    var result = _sut.EncryptAsync(value, key);

    result.Should().NotBe(value);
  }

  [Fact]
  public async Task DecryptAsync_WhenValueIsProvided_ReturnsDecryptedValue()
  {
    var value = "test";
    var key = "key";

    var encryptedValue = await _sut.EncryptAsync(value, key);

    var result = await _sut.DecryptAsync(encryptedValue, key);

    result.Should().Be(value);
  }
}