namespace Schedulr.Console.Auth;

class LoginException : SchedulrException
{
  public LoginException()
  {
  }
  public LoginException(string message) : base(message)
  {
  }

  public LoginException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
