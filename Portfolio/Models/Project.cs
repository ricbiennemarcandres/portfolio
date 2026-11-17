namespace portfolio.Models;

/// <summary>
/// One row of portfolio.project, plus a few read-only values joined in
/// for display (the course's code and title, the cover image, the image count).
/// Forms use ViewModels/ProjectFormViewModel instead.
/// </summary>
public class Project
{
    public int ProjectId { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Tools { get; set; }
    public string? VideoUrl { get; set; }
    public string? RepoUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    // From the joined course row
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";

    // Derived, for cards: first image and number of images
    public string? CoverImageUrl { get; set; }
    public int ImageCount { get; set; }

    // Filled on the details page
    public List<ProjectImage> Images { get; set; } = new();

    /// <summary>"C#, ASP.NET Core, Supabase" → ["C#", "ASP.NET Core", "Supabase"] for the chips.</summary>
    public IEnumerable<string> ToolList =>
        (Tools ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
