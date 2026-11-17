namespace portfolio.Helpers;

public static class TextHelper
{
    /// <summary>Cuts long text and adds "…" (used on cards and in messages).</summary>
    public static string Shorten(string? text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max)
        {
            return text ?? "";
        }

        return text[..max].TrimEnd() + "…";
    }

    public static string SemesterName(short semester) => semester switch
    {
        1 => "1st Semester",
        2 => "2nd Semester",
        3 => "Summer",
        _ => "—"
    };
}
