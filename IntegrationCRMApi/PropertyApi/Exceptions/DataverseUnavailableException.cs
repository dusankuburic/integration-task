namespace PropertyApi.Exceptions;

public class DataverseUnavailableException : Exception
{
    public DataverseUnavailableException(string message) : base(message)
    {
    }

    public DataverseUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
