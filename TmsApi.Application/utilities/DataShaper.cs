using System.Reflection;
using TmsApi.Application.Common; // M7 Session 4 - Exercise 7: BadRequestException for unknown fields

namespace TmsApi.Application.Utilities;

/// <summary>
/// Utility for shaping DTO responses based on client-requested fields.
/// Uses a whitelist approach for security - only explicitly allowed fields can be requested.
/// </summary>
public static class DataShaper
{
    /// <summary>
    /// Shapes a collection of entities into dictionaries containing only the requested/allowed fields.
    /// </summary>
    /// <typeparam name="T">The DTO type to shape</typeparam>
    /// <param name="source">The collection of entities to shape</param>
    /// <param name="fields">Comma-separated list of field names requested by the client</param>
    /// <param name="allowedFields">Whitelist of field names that clients are allowed to request</param>
    /// <returns>Collection of dictionaries with field names and values</returns>
    /// <exception cref="BadRequestException">Thrown when client requests fields not in the whitelist</exception>
    public static IEnumerable<Dictionary<string, object?>> ShapeData<T>(
        this IEnumerable<T> source,
        string? fields,
        ISet<string> allowedFields)
    {
        // Get all public instance properties from the DTO that are in the whitelist
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => allowedFields.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // If no fields specified, return all allowed properties
        if (string.IsNullOrWhiteSpace(fields))
        {
            return source.Select(e => properties.ToDictionary(
                p => p.Name,
                p => p.GetValue(e)));
        }

        // Parse requested fields
        var requested = fields
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        // Security check: ensure all requested fields are in the whitelist
        var unknown = requested
            .Where(f => !allowedFields.Contains(f, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (unknown.Count > 0)
        {
            throw new BadRequestException(
                $"Unknown field(s): {string.Join(", ", unknown)}. " +
                $"Allowed fields: {string.Join(", ", allowedFields)}.");
        }

        // Filter to only the requested properties that are also allowed
        var picked = properties
            .Where(p => requested.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return source.Select(e => picked.ToDictionary(
            p => p.Name,
            p => p.GetValue(e)));
    }
}