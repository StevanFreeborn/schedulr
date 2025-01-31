namespace Schedulr.Console.Commands;

static class HostBuilderExtensions
{
  public static CommandApp BuildApp(this IHostBuilder builder)
  {
    var registrar = new TypeRegistrar(builder);
    var app = new CommandApp(registrar);

    app.Configure(static c =>
    {
      c.AddCommand<LoginCommand>("login");
      c.PropagateExceptions();
    });

    return app;
  }
}