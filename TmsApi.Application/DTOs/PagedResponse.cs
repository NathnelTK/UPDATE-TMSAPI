namespace TmsApi.Application.DTOs;

/// <summary>
/// Generic paginated response wrapper.
/// TotalPages, HasPrevious, HasNext are computed properties
/// so Angular pagination controls can render without a second round-trip.
/// </summary>
public record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
