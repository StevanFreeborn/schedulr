namespace Schedulr.API.Auth;

class GoogleOptionsSetup(IConfiguration config) : IConfigureOptions<GoogleOptions>
{
  const string SectionName = nameof(GoogleOptions);
  readonly IConfiguration _config = config;

  public void Configure(GoogleOptions options)
  {
    _config.GetSection(SectionName).Bind(options);
  }
}