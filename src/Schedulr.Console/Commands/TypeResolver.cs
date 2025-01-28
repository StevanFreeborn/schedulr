namespace Schedulr.Console.Commands;

class TypeResolver(IHost provider) : ITypeResolver, IDisposable
{
  readonly IHost _host = provider ?? throw new ArgumentNullException(nameof(provider));

  public object? Resolve(Type? type)
  {
    return type is not null ? _host.Services.GetService(type) : null;
  }

  public void Dispose()
  {
    _host.Dispose();
  }
}