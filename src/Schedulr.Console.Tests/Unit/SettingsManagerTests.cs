namespace Schedulr.Console.Tests.Unit;

public class SettingsManagerTests
{
  readonly Mock<ISerializer> _serializer = new();
  readonly Mock<IFileSystem> _fileSystem = new();
  readonly Mock<IEncryptor> _encryptor = new();
  readonly SettingsManager _sut;

  public SettingsManagerTests()
  {
    _sut = new SettingsManager(_fileSystem.Object, _serializer.Object, _encryptor.Object);
  }

  [Fact]
  public async Task ReadAsync_WhenSettingsFileDoesNotExist_ReturnsDefaultSettings()
  {
    _fileSystem
      .Setup(static x => x.File.Exists(It.IsAny<string>()))
      .Returns(false);

    var result = await _sut.ReadAsync();

    result.Should().BeEquivalentTo(new Settings());
  }

  [Fact]
  public async Task ReadAsync_WhenSettingsFileExists_ReturnsSettings()
  {
    var settings = new Settings();
    var json = "json";
    var encryptedJson = "encryptedJson";

    _fileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _fileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(encryptedJson);

    _encryptor
      .Setup(x => x.DecryptAsync(encryptedJson, It.IsAny<string>()))
      .ReturnsAsync(json);

    _serializer
      .Setup(x => x.Deserialize<Settings>(json))
      .Returns(settings);

    var result = await _sut.ReadAsync();

    result.Should().BeEquivalentTo(settings);
  }
}
