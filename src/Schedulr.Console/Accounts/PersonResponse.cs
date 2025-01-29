namespace Schedulr.Console.Accounts;

record PersonResponse(string ResourceName, string ETag, List<Name> Names);

record Name(
  Metadata Metadata,
  string DisplayName,
  string FamilyName,
  string GivenName,
  string DisplayNameLastFirst,
  string UnstructuredName
);

record Metadata(bool Primary, Source Source);

record Source(string Type, string Id);