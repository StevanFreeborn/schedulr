namespace Schedulr.Console.Auth;

class SettingsManager(ISerializer serializer) : ISettingsManager
{
  const string SettingsFile = "schedulr.json";
  readonly ISerializer _serializer = serializer;
  static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, SettingsFile);

  // TODO: Encrypt and decrypt settings...will use machine info to generate key
  // used to encrypt and decrypt settings
  public async Task<Settings> ReadAsync()
  {
    if (File.Exists(SettingsPath) is false)
    {
      return new Settings();
    }

    var json = await File.ReadAllTextAsync(SettingsPath);
    var settings = _serializer.Deserialize<Settings>(json) ?? throw new JsonException("Failed to parse settings");
    return settings;
  }

  public Task WriteAsync(Settings settings)
  {
    var json = _serializer.Serialize(settings);
    return File.WriteAllTextAsync(SettingsPath, json);
  }
}