namespace Schedulr.Console.Auth;

interface IAuthService
{
  string GetOAuthUri();
  Task<TokenResponse> GetTokenAsync(string? code);
}
