var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();

app
  .MapPost("/auth/token", static ([AsParameters] AuthTokenRequest request) =>
  {
    var result = request.Validate(new ValidationContext(request));

    if (result.Any())
    {
      return Results.BadRequest(result);
    }

    return Results.Ok();
  })
  .DisableAntiforgery();

app.Run();

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
      yield return new ValidationResult("Client ID is required", [nameof(ClientId)]);
    }

    if (string.IsNullOrWhiteSpace(RedirectUri))
    {
      yield return new ValidationResult("Redirect URI is required", [nameof(RedirectUri)]);
    }

    if (string.IsNullOrWhiteSpace(GrantType))
    {
      yield return new ValidationResult("Grant type is required", [nameof(GrantType)]);
    }

    if (string.IsNullOrWhiteSpace(Code))
    {
      yield return new ValidationResult("Code is required", [nameof(Code)]);
    }
  }
}


[ExcludeFromCodeCoverage]
public partial class Program { }