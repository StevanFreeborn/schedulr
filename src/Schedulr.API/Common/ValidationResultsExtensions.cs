namespace Schedulr.API.Common;

static class ValidationResultsExtensions
{
  public static Dictionary<string, string[]> ToErrors(this IEnumerable<ValidationResult> results)
  {
    return results
      .GroupBy(static r => r.MemberNames.First())
      .ToDictionary(static g => g.Key, static g => g.Select(static r => r.ErrorMessage!).ToArray());
  }
}