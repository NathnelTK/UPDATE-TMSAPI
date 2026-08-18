namespace TmsApi.Application.Common;

/// <summary>
/// Exception thrown when a requested entity is not found.
/// Used by cached services to signal missing data.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}