namespace Schedulr.Console.Auth;

interface IEncryptor
{
  Task<string> EncryptAsync(string value, string key);
  Task<string> DecryptAsync(string value, string key);
}