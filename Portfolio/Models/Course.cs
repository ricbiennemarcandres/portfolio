using System.ComponentModel.DataAnnotations;
using portfolio.Helpers;
using portfolio.Validation;

namespace portfolio.Models;

/// <summary>One row of portfolio.course. The rules match the columns in sql/schema.sql.</summary>
public class Course
{
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Enter the course code.")]
    [StringLength(16, ErrorMessage = "The code can have at most 16 characters.")]
    [RegularExpression(Patterns.CourseCode, ErrorMessage = "Use capital letters then digits, e.g. DBMWEB1, IT 101 or CC-102.")]
    [Display(Name = "Course code")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Enter the course title.")]
    [StringLength(120, ErrorMessage = "The title can have at most 120 characters.")]
    [Display(Name = "Course title")]
    public string Title { get; set; } = "";

    [StringLength(1000, ErrorMessage = "The description can have at most 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Enter the school year.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "Write the school year as 2026-2027.")]
    [RegularExpression(Patterns.SchoolYear, ErrorMessage = "Write the school year as 2026-2027.")]
    [Display(Name = "School year")]
    public string SchoolYear { get; set; } = "";

    [Range(1, 3, ErrorMessage = "Choose 1st Semester, 2nd Semester or Summer.")]
    public short Semester { get; set; } = 1;

    [Range(1, 6, ErrorMessage = "Units must be from 1 to 6.")]
    public short Units { get; set; } = 3;

    [StringLength(80, ErrorMessage = "The instructor's name can have at most 80 characters.")]
    public string? Instructor { get; set; }

    // DERIVED attribute (dashed oval in the ERD): counted with COUNT(*), never stored.
    public int ProjectCount { get; set; }

    public string SemesterName => TextHelper.SemesterName(Semester);
}
