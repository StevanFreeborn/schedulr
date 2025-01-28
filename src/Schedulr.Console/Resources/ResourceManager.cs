namespace Schedulr.Console.Resources;

class ResourceManager : IResourceManager
{
  public string GetResource(string name)
  {
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = assembly.GetName().Name + ".Resources.Files." + name;
    using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}