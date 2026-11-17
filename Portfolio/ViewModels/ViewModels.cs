using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using portfolio.Models;
using portfolio.Validation;

namespace portfolio.ViewModels;

/// <summary>
/// The add / edit project form. ImageUrls is a textarea: one public URL per line.
/// Saving it runs ONE transaction (ProjectRepository.AddWithImagesAsync / UpdateWithImagesAsync).
/// </summary>
public class ProjectFormViewModel
{
    public int ProjectId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose the course this project belongs to.")]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Enter the project title.")]
    [StringLength(120, ErrorMessage = "The title can have at most 120 characters.")]
    public string Title { get; set; } = "";

    [StringLength(2000, ErrorMessage = "The description can have at most 2000 characters.")]
    public string? Description { get; set; }

    [StringLength(200, ErrorMessage = "The list of tools can have at most 200 characters.")]
    [Display(Name = "Tools and languages")]
    public string? Tools { get; set; }

    [StringLength(200, ErrorMessage = "The video link can have at most 200 characters.")]
    [RegularExpression(Patterns.YouTubeUrl,
        ErrorMessage = "Paste a YouTube link: youtube.com/watch?v=…, youtu.be/… or youtube.com/shorts/…")]
    [Display(Name = "YouTube video link")]
    public string? VideoUrl { get; set; }

    [StringLength(300, ErrorMessage = "The repository link can have at most 300 characters.")]
    [RegularExpression(Patterns.RepoUrl, ErrorMessage = "Paste the full link, starting with https://")]
    [Display(Name = "Repository link")]
    public string? RepoUrl { get; set; }

    [ImageUrl(OnePerLine = true)]
    [Display(Name = "Image URLs (one per line)")]
    public string? ImageUrls { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Courses { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>The non-blank lines of the textarea, trimmed, in order.</summary>
    public List<string> ImageUrlList() =>
        (ImageUrls ?? "")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

    public Project ToProject() => new()
    {
        ProjectId = ProjectId,
        CourseId = CourseId,
        Title = Title.Trim(),
        Description = Description,
        Tools = Tools,
        VideoUrl = VideoUrl,
        RepoUrl = RepoUrl
    };

    public static ProjectFormViewModel From(Project p) => new()
    {
        ProjectId = p.ProjectId,
        CourseId = p.CourseId,
        Title = p.Title,
        Description = p.Description,
        Tools = p.Tools,
        VideoUrl = p.VideoUrl,
        RepoUrl = p.RepoUrl,
        ImageUrls = string.Join("\n", p.Images.Select(i => i.ImageUrl))
    };
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter the editor password.")]
    [StringLength(200)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

/// <summary>Home page (viewer) and dashboard (editor).</summary>
public class HomeViewModel
{
    public List<Course> Courses { get; set; } = new();
    public List<Project> LatestProjects { get; set; } = new();
    public int ProjectCount { get; set; }
    public int ImageCount { get; set; }
}

public class CourseDetailsViewModel
{
    public Course Course { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}

public class ProjectListViewModel
{
    public List<Project> Projects { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public string? Search { get; set; }
    public int? CourseId { get; set; }
}
