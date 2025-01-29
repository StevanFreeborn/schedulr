namespace Schedulr.Console.Accounts;

interface IProfileService
{
  Task<PersonResponse> GetUserInfoAsync(string accessToken);
}