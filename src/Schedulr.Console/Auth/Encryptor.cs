

namespace Schedulr.Console.Auth;

class Encryptor : IEncryptor
{
  public async Task<string> DecryptAsync(string value, string key)
  {
    var bytes = Convert.FromBase64String(value);

    using var aes = Aes.Create();
    aes.Key = GetKey(key);

    var iv = new byte[16];
    Array.Copy(bytes, iv, iv.Length);
    aes.IV = iv;

    using var decryptor = aes.CreateDecryptor();
    using var ms = new MemoryStream(bytes, iv.Length, bytes.Length - iv.Length);
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using var sr = new StreamReader(cs);

    return await sr.ReadToEndAsync();
  }

  public async Task<string> EncryptAsync(string value, string key)
  {
    using var aes = Aes.Create();
    aes.Key = GetKey(key);

    aes.GenerateIV();

    using var ms = new MemoryStream();
    await ms.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length));


    using var encryptor = aes.CreateEncryptor();
    using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

    var bytes = Encoding.UTF8.GetBytes(value);
    await cs.WriteAsync(bytes);
    await cs.FlushFinalBlockAsync();

    return Convert.ToBase64String(ms.ToArray());
  }

  static byte[] GetKey(string key)
  {
    return SHA256.HashData(Encoding.UTF8.GetBytes(key));
  }
}