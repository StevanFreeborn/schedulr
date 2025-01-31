namespace Schedulr.Console.Auth;

class Settings
{
  readonly List<Account> _accounts = [];

  public IReadOnlyList<Account> Accounts
  {
    get => _accounts;
    init => _accounts.AddRange(value);
  }

  public void AddAccount(Account account)
  {
    var existing = _accounts.FirstOrDefault(a => a.AccountEmail == account.AccountEmail);

    if (existing is not null)
    {
      _accounts.Remove(existing);
    }

    _accounts.Add(account);
  }
}

class Account(string accountName, string accountEmail, TokenResponse? token)
{
  public string AccountName { get; init; } = accountName;
  public string AccountEmail { get; init; } = accountEmail;
  public TokenResponse? Token { get; init; } = token;
}