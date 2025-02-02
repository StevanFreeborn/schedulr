namespace Schedulr.Console.Commands;

static class HostBuilderExtensions
{
  public static CommandApp BuildApp(this IHostBuilder builder)
  {
    var registrar = new TypeRegistrar(builder);
    var app = new CommandApp(registrar);

    app.Configure(static c =>
    {
      c.AddBranch("account", static c =>
      {
        c.AddCommand<AddAccountCommand>("add");
        c.AddCommand<ListAccountCommand>("list");
        c.AddCommand<RemoveAccountCommand>("remove");
      });
    });

    return app;
  }
}