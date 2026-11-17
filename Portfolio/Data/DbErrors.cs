using Npgsql;

namespace portfolio.Data;

/// <summary>
/// Turns a refusal from PostgreSQL into a message for the page.
/// Controllers catch PostgresException, then call
///     ModelState.AddModelError(DbErrors.FieldFor(ex), DbErrors.Message(ex));
/// so the message appears under the box it is about.
/// Every constraint in sql/schema.sql is named, so we can switch on ConstraintName.
/// </summary>
public static class DbErrors
{
    public const string NotNullViolation = "23502";
    public const string ForeignKeyViolation = "23503";
    public const string UniqueViolation = "23505";
    public const string CheckViolation = "23514";

    /// <summary>True for the refusals we show on the page. Anything else goes to the friendly error page.</summary>
    public static bool IsRefusal(PostgresException ex) =>
        ex.SqlState is NotNullViolation or ForeignKeyViolation or UniqueViolation or CheckViolation;

    /// <summary>The form field the message belongs under ("" = the summary at the top of the form).</summary>
    public static string FieldFor(PostgresException ex) => ex.ConstraintName switch
    {
        "uq_course_code" or "ck_course_code" => "Code",
        "ck_course_school_year" => "SchoolYear",
        "ck_course_semester" => "Semester",
        "ck_course_units" => "Units",

        "fk_project_course" => "CourseId",
        "uq_project_course_title" => "Title",
        "ck_project_video_url" => "VideoUrl",
        "ck_project_repo_url" => "RepoUrl",

        // On the project form the images are in the "ImageUrls" box; on the
        // single-image form they are in "ImageUrl". Controllers that need the
        // other name pass it themselves (see ProjectsController).
        "uq_project_image_url" or "ck_project_image_url" => "ImageUrl",
        "ck_project_image_sort_order" => "SortOrder",

        _ => string.Empty
    };

    /// <summary>Message for a refused INSERT or UPDATE.</summary>
    public static string Message(PostgresException ex)
    {
        var text = ex.ConstraintName switch
        {
            "uq_course_code" => "Another course already uses this code. Course codes must be unique.",
            "ck_course_code" => "The database refused this code. Use capital letters then digits, e.g. DBMWEB1 or IT 101.",
            "ck_course_school_year" => "The second year must come right after the first, e.g. 2026-2027.",
            "ck_course_semester" => "The semester must be 1st, 2nd or Summer.",
            "ck_course_units" => "Units must be from 1 to 6.",

            "fk_project_course" => "The selected course does not exist any more. Choose another course.",
            "uq_project_course_title" => "This course already has a project with this title.",
            "ck_project_video_url" => "The database refused this video link. Paste a YouTube link.",
            "ck_project_repo_url" => "The repository link must start with https://.",

            "fk_project_image_project" => "That project does not exist any more.",
            "uq_project_image_url" => "This image URL is already in this project. The same URL cannot be added twice.",
            "ck_project_image_url" => "The database refused this image URL. Use a public https link to a .png, .jpg, .gif, .webp or .svg file.",
            "ck_project_image_sort_order" => "The order must be 1 or more.",

            _ => ex.SqlState switch
            {
                NotNullViolation => "A required value is missing.",
                ForeignKeyViolation => "This refers to a row that does not exist.",
                UniqueViolation => "This value already exists.",
                CheckViolation => "A value breaks one of the database's rules.",
                _ => "The database refused the change."
            }
        };

        return WithCode(text, ex);
    }

    /// <summary>Message for a refused DELETE (DeleteConfirmed).</summary>
    public static string DeleteMessage(PostgresException ex)
    {
        if (ex.SqlState == ForeignKeyViolation && ex.ConstraintName == "fk_project_course")
        {
            return WithCode(
                "This course still has projects, so the database refused to delete it. " +
                "Delete its projects first, or move them to another course.", ex);
        }

        return Message(ex);
    }

    // Adds e.g. "[23505 · uq_course_code]" so the code is visible during the demo.
    private static string WithCode(string text, PostgresException ex) =>
        string.IsNullOrEmpty(ex.ConstraintName)
            ? $"{text} [{ex.SqlState}]"
            : $"{text} [{ex.SqlState} · {ex.ConstraintName}]";
}
