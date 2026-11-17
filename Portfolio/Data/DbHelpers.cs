using Npgsql;
using NpgsqlTypes;

namespace portfolio.Data;

/// <summary>Small helpers shared by the repositories.</summary>
public static class DbHelpers
{
    /// <summary>A text parameter that may be null (sends SQL NULL with a known type).</summary>
    public static NpgsqlParameter NullableText(string name, string? value) =>
        new(name, NpgsqlDbType.Text) { Value = string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim() };

    /// <summary>Reads a column that may be NULL.</summary>
    public static string? GetNullableString(this NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    /// <summary>
    /// Makes a search box value safe for ILIKE: % and _ are matched literally.
    /// Returns "" when there is nothing to search for.
    /// </summary>
    public static string CleanSearch(string? search) =>
        (search ?? "").Trim()
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");
}
