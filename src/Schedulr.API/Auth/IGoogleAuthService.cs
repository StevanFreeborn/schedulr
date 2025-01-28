namespace Schedulr.API.Auth;

interface IGoogleAuthService
{
  Task<TokenResponse> GetAccessToken(AuthTokenRequest request);
}
