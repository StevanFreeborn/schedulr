namespace Schedulr.Console.Json;

interface ISerializer
{
  T Deserialize<T>(string json);
  string Serialize<T>(T value);
}