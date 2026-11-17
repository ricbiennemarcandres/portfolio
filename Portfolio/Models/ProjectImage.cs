using System.ComponentModel.DataAnnotations;
using portfolio.Validation;

namespace portfolio.Models;

/// <summary>One row of portfolio.project_image (a project has many).</summary>
public class ProjectImage
{
    public int ImageId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a project.")]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Paste the image's public URL.")]
    [StringLength(ImageUrlAttribute.MaxLength, ErrorMessage = "The URL can have at most 500 characters.")]
    [ImageUrl]
    [Display(Name = "Image URL")]
    public string ImageUrl { get; set; } = "";

    [StringLength(150, ErrorMessage = "The caption can have at most 150 characters.")]
    public string? Caption { get; set; }

    [Range(1, 999, ErrorMessage = "The order must be from 1 to 999.")]
    [Display(Name = "Order")]
    public short SortOrder { get; set; } = 1;

    // For display only (joined from project)
    public string? ProjectTitle { get; set; }
}
