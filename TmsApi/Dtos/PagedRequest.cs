namespace TmsApi.Dtos;

/// <summary>
/// Pagination and filtering request parameters bound from query string.
/// PageSize is capped at 50 server-side to prevent abuse.
/// Search supports case-insensitive matching via ILike (PostgreSQL).
/// OrderBy defaults to "Title" with whitelist validation.
/// </summary>
public record PagedRequest
{
    private const int MaxPageSize = 50;
    private int _pageSize = 20;

    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 20 : value > MaxPageSize ? MaxPageSize : value;
    }

    public string? Search { get; init; }

    public string OrderBy { get; init; } = "Title";

    public bool Descending { get; init; }
}