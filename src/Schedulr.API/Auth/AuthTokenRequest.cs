namespace Schedulr.API.Auth;

record AuthTokenRequest(
  [FromForm(Name = "client_id")]
  string ClientId,
  [FromForm(Name = "redirect_uri")]
  string RedirectUri,
  [FromForm(Name = "grant_type")]
  string GrantType,
  [FromForm(Name = "code")]
  string Code
) : IValidatableObject
{
  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (string.IsNullOrWhiteSpace(ClientId))
    {
      yield return new ValidationResult($"{nameof(ClientId)} is required", [nameof(ClientId)]);
    }

    if (string.IsNullOrWhiteSpace(RedirectUri))
    {
      yield return new ValidationResult($"{nameof(RedirectUri)} is required", [nameof(RedirectUri)]);
    }

    if (string.IsNullOrWhiteSpace(GrantType))
    {
      yield return new ValidationResult($"{nameof(GrantType)} is required", [nameof(GrantType)]);
    }

    if (string.IsNullOrWhiteSpace(Code))
    {
      yield return new ValidationResult($"{nameof(Code)} is required", [nameof(Code)]);
    }
  }
}