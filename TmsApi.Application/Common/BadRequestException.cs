namespace TmsApi.Application.Common;

/// <summary>
/// Exception thrown when a client requests invalid or unsupported fields.
/// Used by DataShaper and other application-layer validation helpers.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }
}
