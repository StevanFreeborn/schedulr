namespace Schedulr.Console.Common;

abstract class SchedulrException : Exception
{
  protected SchedulrException()
  {
  }

  protected SchedulrException(string message) : base(message)
  {
  }

  protected SchedulrException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
