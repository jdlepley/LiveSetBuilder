// LiveSetBuilder.Core/Storage/ShowRepository.cs
using Microsoft.Data.Sqlite;
using LiveSetBuilder.Core.Models;

namespace LiveSetBuilder.Core.Storage;

public sealed class ShowRepository : IRepository<Show>
{
    private readonly IAppDatabase _db;
    public ShowRepository(IAppDatabase db) => _db = db;

    public async Task<Show?> GetAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,Title,DefaultBpm,DefaultTimeSig,UpdatedAt FROM Show WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync()) return Map(r);
        return null;
    }

    public async Task<List<Show>> GetAllAsync()
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,Title,DefaultBpm,DefaultTimeSig,UpdatedAt FROM Show ORDER BY UpdatedAt DESC";
        var list = new List<Show>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<int> InsertAsync(Show s)
    {
        s.UpdatedAt = DateTime.UtcNow;
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Show(Title,DefaultBpm,DefaultTimeSig,UpdatedAt)
VALUES(@t,@bpm,@ts,@u);
SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@t", s.Title);
        cmd.Parameters.AddWithValue("@bpm", (object?)s.DefaultBpm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ts", (object?)s.DefaultTimeSig ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", s.UpdatedAt.ToString("o"));
        var id = (long)await cmd.ExecuteScalarAsync();
        return (int)id;
    }

    public async Task<int> UpdateAsync(Show s)
    {
        s.UpdatedAt = DateTime.UtcNow;
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
UPDATE Show SET Title=@t, DefaultBpm=@bpm, DefaultTimeSig=@ts, UpdatedAt=@u
WHERE Id=@id";
        cmd.Parameters.AddWithValue("@t", s.Title);
        cmd.Parameters.AddWithValue("@bpm", (object?)s.DefaultBpm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ts", (object?)s.DefaultTimeSig ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", s.UpdatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@id", s.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM Show WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static Show Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Title = r.GetString(1),
        DefaultBpm = r.IsDBNull(2) ? null : r.GetDouble(2),
        DefaultTimeSig = r.IsDBNull(3) ? null : r.GetString(3),
        UpdatedAt = DateTime.Parse(r.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind)
    };
}
