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
    var existing = _accounts.FirstOrDefault(a => a.AccountId == account.AccountId);

    if (existing is not null)
    {
      _accounts.Remove(existing);
    }

    _accounts.Add(account);
  }
}

class Account(string accountId, TokenResponse? token)
{
  public string AccountId { get; init; } = accountId;
  public TokenResponse? Token { get; init; } = token;
}