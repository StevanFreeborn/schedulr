namespace Schedulr.Console.Accounts;

record PersonResponse(
  string ResourceName,
  string ETag,
  List<Name> Names,
  List<EmailAddress> EmailAddresses
);

record Name(
  Metadata Metadata,
  string DisplayName,
  string FamilyName,
  string GivenName,
  string DisplayNameLastFirst,
  string UnstructuredName
);

record EmailAddress(
  Metadata Metadata,
  string Value,
  string Type,
  string FormattedType,
  string DisplayName
);

record Metadata(bool Primary, bool SourcePrimary, bool Verified, Source Source);

record Source(string Type, string Id, string ETag, DateTimeOffset UpdateTime);