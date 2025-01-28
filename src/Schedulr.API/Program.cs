var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.ConfigureOptions<GoogleOptionsSetup>();
builder.Services.AddSingleton(static sp => sp.GetRequiredService<IOptions<GoogleOptions>>().Value);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseStatusCodePages();

app.UseHttpsRedirection();

app
  .MapPost("/auth/token", static async ([AsParameters] AuthTokenRequest request, [FromServices] IGoogleAuthService authService) =>
  {
    var validationResults = request.Validate(new ValidationContext(request));

    if (validationResults.Any())
    {
      return Results.ValidationProblem(validationResults.ToErrors());
    }

    var token = await authService.GetAccessToken(request);

    return Results.Ok(token);
  })
  .DisableAntiforgery();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }