namespace portfolio.Validation;

/// <summary>
/// Every regular expression the app uses, in one place.
/// The same rules are written as CHECK constraints in sql/schema.sql,
/// so the browser, the server and the database all agree.
/// </summary>
public static class Patterns
{
    // 1. IMAGE URL (checked line by line with Regex.IsMatch, case-insensitive)
    //    https://  then no spaces  then .png .jpg .jpeg .gif .webp .svg  then optional ?query
    //    DB: ck_project_image_url
    public const string ImageUrl = @"^https://\S+\.(?:png|jpe?g|gif|webp|svg)(?:\?\S*)?$";

    // 2. YOUTUBE LINK
    //    youtube.com/watch?v=ID · youtu.be/ID · youtube.com/shorts/ID  (ID = 11 characters)
    //    DB: ck_project_video_url
    public const string YouTubeUrl =
        @"^https://(?:www\.|m\.)?(?:youtube\.com/(?:watch\?(?:\S*&)?v=|shorts/)|youtu\.be/)[A-Za-z0-9_-]{11}(?:[?&#]\S*)?$";

    // 3. COURSE CODE (my choice)
    //    capital letters, optional space or hyphen, digits, optional letter: DBMWEB1, IT 101, CC-102
    //    DB: ck_course_code
    public const string CourseCode = @"^[A-Z]{2,10}[ -]?[0-9]{1,4}[A-Z]?$";

    // 4. SCHOOL YEAR: 2026-2027
    //    The database also checks that the 2nd year = 1st year + 1 (ck_course_school_year).
    public const string SchoolYear = @"^[0-9]{4}-[0-9]{4}$";

    // 5. REPOSITORY LINK: any https:// link without spaces
    //    DB: ck_project_repo_url
    public const string RepoUrl = @"^https://\S+$";
}
