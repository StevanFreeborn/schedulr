namespace Schedulr.Console.Auth;

class SettingsManager(IFileSystem fileSystem, ISerializer serializer, IEncryptor encryptor) : ISettingsManager
{
  const string SettingsFile = "schedulr.json";
  static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, SettingsFile);
  static readonly string Key = GetMachineInfo();
  readonly IFileSystem _fileSystem = fileSystem;
  readonly ISerializer _serializer = serializer;
  readonly IEncryptor _encryptor = encryptor;

  public async Task<Settings> ReadAsync()
  {
    if (File.Exists(SettingsPath) is false)
    {
      return new Settings();
    }

    var encryptedJson = await _fileSystem.File.ReadAllTextAsync(SettingsPath);
    var json = await _encryptor.DecryptAsync(encryptedJson, Key);
    var settings = _serializer.Deserialize<Settings>(json) ?? throw new JsonException("Failed to parse settings");
    return settings;
  }

  public async Task WriteAsync(Settings settings)
  {
    var json = _serializer.Serialize(settings);
    var encryptedJson = await _encryptor.EncryptAsync(json, Key);
    await _fileSystem.File.WriteAllTextAsync(SettingsPath, encryptedJson);
  }

  static string GetMachineInfo()
  {
    var machineName = Environment.MachineName;
    var processorCount = Environment.ProcessorCount;
    var userName = Environment.UserName;
    return $"{machineName}{processorCount}{userName}";
  }
}