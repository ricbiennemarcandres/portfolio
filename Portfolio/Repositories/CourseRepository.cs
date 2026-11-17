using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Repositories;

/// <summary>All SQL for the portfolio.course table.</summary>
public class CourseRepository
{
    private readonly NpgsqlDataSource _db;

    public CourseRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    // project_count is the DERIVED attribute: counted here, never stored.
    private const string SelectColumns = @"
        select c.course_id, c.code, c.title, c.description, c.school_year,
               c.semester, c.units, c.instructor,
               (select count(*) from portfolio.project p where p.course_id = c.course_id)::int as project_count
        from portfolio.course c";

    public async Task<List<Course>> GetAllAsync(string? search = null)
    {
        var sql = SelectColumns + @"
        where @search = ''
           or c.code  ilike @pattern
           or c.title ilike @pattern
        order by c.school_year desc, c.semester desc, c.code";

        var clean = DbHelpers.CleanSearch(search);
        await using var cmd = _db.CreateCommand(sql);
        cmd.Parameters.AddWithValue("search", clean);
        cmd.Parameters.AddWithValue("pattern", "%" + clean + "%");

        var courses = new List<Course>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            courses.Add(Map(reader));
        }
        return courses;
    }

    /// <summary>Returns null when no course has this id (the controller then returns 404).</summary>
    public async Task<Course?> GetByIdAsync(int id)
    {
        await using var cmd = _db.CreateCommand(SelectColumns + " where c.course_id = @id");
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    /// <summary>Inserts a course and returns its new id. A refusal (23505, 23514…) is thrown as PostgresException.</summary>
    public async Task<int> AddAsync(Course course)
    {
        const string sql = @"
            insert into portfolio.course (code, title, description, school_year, semester, units, instructor)
            values (@code, @title, @description, @school_year, @semester, @units, @instructor)
            returning course_id";

        await using var cmd = _db.CreateCommand(sql);
        AddParameters(cmd, course);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>Returns false when no course has this id.</summary>
    public async Task<bool> UpdateAsync(Course course)
    {
        const string sql = @"
            update portfolio.course
               set code = @code, title = @title, description = @description,
                   school_year = @school_year, semester = @semester,
                   units = @units, instructor = @instructor
             where course_id = @id";

        await using var cmd = _db.CreateCommand(sql);
        AddParameters(cmd, course);
        cmd.Parameters.AddWithValue("id", course.CourseId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Returns false when no course has this id.
    /// A course that still has projects is refused by fk_project_course (ON DELETE RESTRICT → 23503).
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        await using var cmd = _db.CreateCommand("delete from portfolio.course where course_id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static void AddParameters(NpgsqlCommand cmd, Course c)
    {
        cmd.Parameters.AddWithValue("code", c.Code.Trim());
        cmd.Parameters.AddWithValue("title", c.Title.Trim());
        cmd.Parameters.Add(DbHelpers.NullableText("description", c.Description));
        cmd.Parameters.AddWithValue("school_year", c.SchoolYear.Trim());
        cmd.Parameters.AddWithValue("semester", c.Semester);
        cmd.Parameters.AddWithValue("units", c.Units);
        cmd.Parameters.Add(DbHelpers.NullableText("instructor", c.Instructor));
    }

    private static Course Map(NpgsqlDataReader r) => new()
    {
        CourseId = r.GetInt32(0),
        Code = r.GetString(1),
        Title = r.GetString(2),
        Description = r.GetNullableString(3),
        SchoolYear = r.GetString(4),
        Semester = r.GetInt16(5),
        Units = r.GetInt16(6),
        Instructor = r.GetNullableString(7),
        ProjectCount = r.GetInt32(8)
    };
}
