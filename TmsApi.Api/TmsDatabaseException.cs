// --- Session 3 - Exercise 6: Custom Exception for ProblemDetails ---
// Defining a custom business/data-layer exception that we can throw to simulate failures.
// This exception will be intercepted by the UseExceptionHandler middleware to return
// structured RFC 9457 ProblemDetails JSON.
public class TmsDatabaseException : Exception
{
    public TmsDatabaseException(string message) : base(message)
    {
    }
}
