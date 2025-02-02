
namespace Schedulr.Console.Accounts;

class RemoveAccountSettings : CommandSettings
{
  [CommandArgument(0, "<ACCOUNT>")]
  public string Account { get; init; } = string.Empty;
}

class RemoveAccountCommand(ISettingsManager settingsManager) : AsyncCommand<RemoveAccountSettings>
{
  readonly ISettingsManager _settingsManager = settingsManager;

  public override async Task<int> ExecuteAsync(CommandContext context, RemoveAccountSettings commandSettings)
  {
    var settings = await _settingsManager.ReadAsync();
    var account = settings.FindAccount(commandSettings.Account);

    if (account is null)
    {
      AnsiConsole.MarkupLine("[bold red]Account not found.[/]");
      return 1;
    }

    settings.RemoveAccount(account);
    await _settingsManager.WriteAsync(settings);

    AnsiConsole.MarkupLine("[bold green]Account removed successfully.[/]");
    return 0;
  }
}