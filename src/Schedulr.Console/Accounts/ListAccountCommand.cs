namespace Schedulr.Console.Accounts;

class ListAccountSettings : CommandSettings
{
  [CommandOption("-f|--filter <FILTER>")]
  public string Filter { get; init; } = string.Empty;
}

class ListAccountCommand(ISettingsManager settingsManager) : AsyncCommand<ListAccountSettings>
{
  readonly ISettingsManager _settingsManager = settingsManager;

  public override async Task<int> ExecuteAsync(CommandContext context, ListAccountSettings commandSettings)
  {
    var settings = await _settingsManager.ReadAsync();
    var accounts = settings.Accounts;

    if (string.IsNullOrWhiteSpace(commandSettings.Filter) is false)
    {
      accounts = [.. accounts.Where(
        a =>
          a.AccountName.Contains(commandSettings.Filter, StringComparison.OrdinalIgnoreCase) ||
          a.AccountEmail.Contains(commandSettings.Filter, StringComparison.OrdinalIgnoreCase)
      )];
    }

    if (accounts.Count == 0)
    {
      AnsiConsole.MarkupLine("[bold red]No accounts found.[/]");
      return 1;
    }

    var table = new Table();
    table.AddColumn("Name");
    table.AddColumn("Email");

    foreach (var account in accounts)
    {
      table.AddRow(account.AccountName, account.AccountEmail);
    }

    AnsiConsole.Write(table);
    return 0;
  }
}