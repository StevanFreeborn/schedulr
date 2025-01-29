namespace Schedulr.Console.Auth;

interface ISettingsManager
{
  Task<Settings> ReadAsync();
  Task WriteAsync(Settings settings);
}