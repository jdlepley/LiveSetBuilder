// LiveSetBuilder.Core/Storage/SongRepository.cs
using Microsoft.Data.Sqlite;
using LiveSetBuilder.Core.Models;

namespace LiveSetBuilder.Core.Storage;

public sealed class SongRepository : IRepository<Song>
{
    private readonly IAppDatabase _db;
    public SongRepository(IAppDatabase db) => _db = db;

    public async Task<Song?> GetAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,ShowId,Title,Bpm,TimeSig,StartGapBars,OrderIndex,Notes FROM Song WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync()) return Map(r);
        return null;
    }

    public async Task<List<Song>> GetAllAsync()
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,ShowId,Title,Bpm,TimeSig,StartGapBars,OrderIndex,Notes FROM Song ORDER BY ShowId, OrderIndex";
        var list = new List<Song>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<List<Song>> GetByShowAsync(int showId)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,ShowId,Title,Bpm,TimeSig,StartGapBars,OrderIndex,Notes FROM Song WHERE ShowId=@sid ORDER BY OrderIndex";
        cmd.Parameters.AddWithValue("@sid", showId);
        var list = new List<Song>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<int> InsertAsync(Song s)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Song(ShowId,Title,Bpm,TimeSig,StartGapBars,OrderIndex,Notes)
VALUES(@sid,@t,@bpm,@ts,@gap,@ord,@n);
SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@sid", s.ShowId);
        cmd.Parameters.AddWithValue("@t", s.Title);
        cmd.Parameters.AddWithValue("@bpm", (object?)s.Bpm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ts", (object?)s.TimeSig ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@gap", s.StartGapBars);
        cmd.Parameters.AddWithValue("@ord", s.OrderIndex);
        cmd.Parameters.AddWithValue("@n", (object?)s.Notes ?? DBNull.Value);
        var id = (long)await cmd.ExecuteScalarAsync();
        return (int)id;
    }

    public async Task<int> UpdateAsync(Song s)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
UPDATE Song
SET ShowId=@sid, Title=@t, Bpm=@bpm, TimeSig=@ts, StartGapBars=@gap, OrderIndex=@ord, Notes=@n
WHERE Id=@id";
        cmd.Parameters.AddWithValue("@sid", s.ShowId);
        cmd.Parameters.AddWithValue("@t", s.Title);
        cmd.Parameters.AddWithValue("@bpm", (object?)s.Bpm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ts", (object?)s.TimeSig ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@gap", s.StartGapBars);
        cmd.Parameters.AddWithValue("@ord", s.OrderIndex);
        cmd.Parameters.AddWithValue("@n", (object?)s.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", s.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM Song WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static Song Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        ShowId = r.GetInt32(1),
        Title = r.GetString(2),
        Bpm = r.IsDBNull(3) ? null : r.GetDouble(3),
        TimeSig = r.IsDBNull(4) ? null : r.GetString(4),
        StartGapBars = r.GetDouble(5),
        OrderIndex = r.GetInt32(6),
        Notes = r.IsDBNull(7) ? null : r.GetString(7)
    };
}
