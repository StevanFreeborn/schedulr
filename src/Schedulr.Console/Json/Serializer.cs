namespace Schedulr.Console.Json;

class Serializer : ISerializer
{
  static readonly JsonSerializerOptions Options = new()
  {
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
  };

  public T Deserialize<T>(string json)
  {
    return JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException("Failed to parse JSON");
  }

  public string Serialize<T>(T value)
  {
    return JsonSerializer.Serialize(value, Options);
  }
}