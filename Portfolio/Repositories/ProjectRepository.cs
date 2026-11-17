using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Repositories;

/// <summary>
/// All SQL for the portfolio.project table.
/// Saving a project together with its images uses ONE transaction
/// (same pattern as BorrowAsync in the Library System).
/// </summary>
public class ProjectRepository
{
    private readonly NpgsqlDataSource _db;
    private readonly ProjectImageRepository _images;

    public ProjectRepository(NpgsqlDataSource db, ProjectImageRepository images)
    {
        _db = db;
        _images = images;
    }

    private const string SelectColumns = @"
        select p.project_id, p.course_id, p.title, p.description, p.tools,
               p.video_url, p.repo_url, p.created_at,
               c.code, c.title,
               (select i.image_url from portfolio.project_image i
                 where i.project_id = p.project_id
                 order by i.sort_order, i.image_id limit 1)                               as cover_url,
               (select count(*) from portfolio.project_image i
                 where i.project_id = p.project_id)::int                                  as image_count
        from portfolio.project p
        join portfolio.course c on c.course_id = p.course_id";

    private const string InsertSql = @"
        insert into portfolio.project (course_id, title, description, tools, video_url, repo_url)
        values (@course_id, @title, @description, @tools, @video_url, @repo_url)
        returning project_id";

    private const string UpdateSql = @"
        update portfolio.project
           set course_id = @course_id, title = @title, description = @description,
               tools = @tools, video_url = @video_url, repo_url = @repo_url
         where project_id = @id";

    /// <summary>Search (title, description, tools, course) and/or filter by course. Newest first.</summary>
    public async Task<List<Project>> GetAllAsync(string? search = null, int? courseId = null, int? limit = null)
    {
        var sql = SelectColumns + @"
        where (@search = ''
               or p.title       ilike @pattern
               or p.description ilike @pattern
               or p.tools       ilike @pattern
               or c.code        ilike @pattern
               or c.title       ilike @pattern)
          and (@course_id = 0 or p.course_id = @course_id)
        order by p.created_at desc, p.project_id desc
        limit @limit";

        var clean = DbHelpers.CleanSearch(search);
        await using var cmd = _db.CreateCommand(sql);
        cmd.Parameters.AddWithValue("search", clean);
        cmd.Parameters.AddWithValue("pattern", "%" + clean + "%");
        cmd.Parameters.AddWithValue("course_id", courseId ?? 0);
        cmd.Parameters.AddWithValue("limit", limit ?? 1000);

        var projects = new List<Project>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            projects.Add(Map(reader));
        }
        return projects;
    }

    /// <summary>One project with its images, or null (→ 404).</summary>
    public async Task<Project?> GetByIdAsync(int id)
    {
        Project? project;

        await using (var cmd = _db.CreateCommand(SelectColumns + " where p.project_id = @id"))
        {
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync();
            project = await reader.ReadAsync() ? Map(reader) : null;
        }

        if (project is not null)
        {
            project.Images = await _images.GetByProjectAsync(id);
        }
        return project;
    }

    public async Task<int> CountAsync()
    {
        await using var cmd = _db.CreateCommand("select count(*)::int from portfolio.project");
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>
    /// ADD a project and all its images in ONE transaction.
    /// If any INSERT is refused (for example the same URL twice → 23505 uq_project_image_url),
    /// everything is rolled back: no project and no images are saved.
    /// The PostgresException is re-thrown so the controller can show the message.
    /// </summary>
    public async Task<int> AddWithImagesAsync(Project project, IReadOnlyList<string> imageUrls)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            int projectId;
            await using (var cmd = new NpgsqlCommand(InsertSql, conn, tx))
            {
                AddParameters(cmd, project);
                projectId = (int)(await cmd.ExecuteScalarAsync())!;
            }

            for (var i = 0; i < imageUrls.Count; i++)
            {
                await _images.InsertAsync(conn, tx, projectId, imageUrls[i], null, (short)(i + 1));
            }

            await tx.CommitAsync();
            return projectId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// EDIT a project and REPLACE its images in ONE transaction:
    /// update the project row, delete its old images, insert the new list.
    /// Captions of URLs that stay are kept. Any refusal rolls everything back,
    /// so the old images are still there. Returns false when no project has this id.
    /// </summary>
    public async Task<bool> UpdateWithImagesAsync(Project project, IReadOnlyList<string> imageUrls)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await using (var cmd = new NpgsqlCommand(UpdateSql, conn, tx))
            {
                AddParameters(cmd, project);
                cmd.Parameters.AddWithValue("id", project.ProjectId);
                if (await cmd.ExecuteNonQueryAsync() == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var captions = await _images.GetCaptionsAsync(conn, tx, project.ProjectId);
            await _images.DeleteByProjectAsync(conn, tx, project.ProjectId);

            for (var i = 0; i < imageUrls.Count; i++)
            {
                captions.TryGetValue(imageUrls[i], out var caption);
                await _images.InsertAsync(conn, tx, project.ProjectId, imageUrls[i], caption, (short)(i + 1));
            }

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>Deletes a project. Its images go too (fk_project_image_project ON DELETE CASCADE).</summary>
    public async Task<bool> DeleteAsync(int id)
    {
        await using var cmd = _db.CreateCommand("delete from portfolio.project where project_id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static void AddParameters(NpgsqlCommand cmd, Project p)
    {
        cmd.Parameters.AddWithValue("course_id", p.CourseId);
        cmd.Parameters.AddWithValue("title", p.Title.Trim());
        cmd.Parameters.Add(DbHelpers.NullableText("description", p.Description));
        cmd.Parameters.Add(DbHelpers.NullableText("tools", p.Tools));
        cmd.Parameters.Add(DbHelpers.NullableText("video_url", p.VideoUrl));
        cmd.Parameters.Add(DbHelpers.NullableText("repo_url", p.RepoUrl));
    }

    private static Project Map(NpgsqlDataReader r) => new()
    {
        ProjectId = r.GetInt32(0),
        CourseId = r.GetInt32(1),
        Title = r.GetString(2),
        Description = r.GetNullableString(3),
        Tools = r.GetNullableString(4),
        VideoUrl = r.GetNullableString(5),
        RepoUrl = r.GetNullableString(6),
        CreatedAt = r.GetDateTime(7),
        CourseCode = r.GetString(8),
        CourseTitle = r.GetString(9),
        CoverImageUrl = r.GetNullableString(10),
        ImageCount = r.GetInt32(11)
    };
}
