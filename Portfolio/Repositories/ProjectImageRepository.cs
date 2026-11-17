using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Repositories;

/// <summary>All SQL for the portfolio.project_image table.</summary>
public class ProjectImageRepository
{
    private readonly NpgsqlDataSource _db;

    public ProjectImageRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    private const string SelectColumns = @"
        select i.image_id, i.project_id, i.image_url, i.caption, i.sort_order, p.title
        from portfolio.project_image i
        join portfolio.project p on p.project_id = i.project_id";

    public async Task<List<ProjectImage>> GetByProjectAsync(int projectId)
    {
        await using var cmd = _db.CreateCommand(SelectColumns + @"
            where i.project_id = @project_id
            order by i.sort_order, i.image_id");
        cmd.Parameters.AddWithValue("project_id", projectId);

        var images = new List<ProjectImage>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            images.Add(Map(reader));
        }
        return images;
    }

    public async Task<ProjectImage?> GetByIdAsync(int id)
    {
        await using var cmd = _db.CreateCommand(SelectColumns + " where i.image_id = @id");
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CountAsync()
    {
        await using var cmd = _db.CreateCommand("select count(*)::int from portfolio.project_image");
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>The order number for a new image: one after the last one.</summary>
    public async Task<short> NextSortOrderAsync(int projectId)
    {
        await using var cmd = _db.CreateCommand(@"
            select (coalesce(max(sort_order), 0) + 1)::int
            from portfolio.project_image where project_id = @project_id");
        cmd.Parameters.AddWithValue("project_id", projectId);
        var next = (int)(await cmd.ExecuteScalarAsync())!;
        return (short)Math.Min(next, 999);
    }

    /// <summary>Adds ONE image (Images editor). The same URL twice in a project → 23505.</summary>
    public async Task<int> AddAsync(ProjectImage image)
    {
        const string sql = @"
            insert into portfolio.project_image (project_id, image_url, caption, sort_order)
            values (@project_id, @image_url, @caption, @sort_order)
            returning image_id";

        await using var cmd = _db.CreateCommand(sql);
        cmd.Parameters.AddWithValue("project_id", image.ProjectId);
        cmd.Parameters.AddWithValue("image_url", image.ImageUrl.Trim());
        cmd.Parameters.Add(DbHelpers.NullableText("caption", image.Caption));
        cmd.Parameters.AddWithValue("sort_order", image.SortOrder);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<bool> UpdateAsync(ProjectImage image)
    {
        const string sql = @"
            update portfolio.project_image
               set image_url = @image_url, caption = @caption, sort_order = @sort_order
             where image_id = @id";

        await using var cmd = _db.CreateCommand(sql);
        cmd.Parameters.AddWithValue("image_url", image.ImageUrl.Trim());
        cmd.Parameters.Add(DbHelpers.NullableText("caption", image.Caption));
        cmd.Parameters.AddWithValue("sort_order", image.SortOrder);
        cmd.Parameters.AddWithValue("id", image.ImageId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var cmd = _db.CreateCommand("delete from portfolio.project_image where image_id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // -----------------------------------------------------------------
    //  Used INSIDE a transaction opened by ProjectRepository.
    //  They take the caller's connection and transaction, so they are
    //  committed or rolled back together with the project.
    // -----------------------------------------------------------------

    internal async Task InsertAsync(NpgsqlConnection conn, NpgsqlTransaction tx,
                                    int projectId, string imageUrl, string? caption, short sortOrder)
    {
        const string sql = @"
            insert into portfolio.project_image (project_id, image_url, caption, sort_order)
            values (@project_id, @image_url, @caption, @sort_order)";

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("project_id", projectId);
        cmd.Parameters.AddWithValue("image_url", imageUrl);
        cmd.Parameters.Add(DbHelpers.NullableText("caption", caption));
        cmd.Parameters.AddWithValue("sort_order", sortOrder);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>URL → caption of a project's current images, so captions survive an edit.</summary>
    internal async Task<Dictionary<string, string?>> GetCaptionsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int projectId)
    {
        await using var cmd = new NpgsqlCommand(
            "select image_url, caption from portfolio.project_image where project_id = @project_id", conn, tx);
        cmd.Parameters.AddWithValue("project_id", projectId);

        var captions = new Dictionary<string, string?>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            captions[reader.GetString(0)] = reader.GetNullableString(1);
        }
        return captions;
    }

    internal async Task DeleteByProjectAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int projectId)
    {
        await using var cmd = new NpgsqlCommand(
            "delete from portfolio.project_image where project_id = @project_id", conn, tx);
        cmd.Parameters.AddWithValue("project_id", projectId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static ProjectImage Map(NpgsqlDataReader r) => new()
    {
        ImageId = r.GetInt32(0),
        ProjectId = r.GetInt32(1),
        ImageUrl = r.GetString(2),
        Caption = r.GetNullableString(3),
        SortOrder = r.GetInt16(4),
        ProjectTitle = r.GetString(5)
    };
}
