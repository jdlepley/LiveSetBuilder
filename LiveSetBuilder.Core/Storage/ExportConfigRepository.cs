// LiveSetBuilder.Core/Storage/ExportConfigRepository.cs
using Microsoft.Data.Sqlite;
using LiveSetBuilder.Core.Models;

namespace LiveSetBuilder.Core.Storage;

public sealed class ExportConfigRepository : IRepository<ExportConfig>
{
    private readonly IAppDatabase _db;
    public ExportConfigRepository(IAppDatabase db) => _db = db;

    public async Task<ExportConfig?> GetAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,ShowId,Target,PanningPreset,SampleRate,HeadroomDb FROM ExportConfig WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync()) return Map(r);
        return null;
    }

    public async Task<List<ExportConfig>> GetAllAsync()
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,ShowId,Target,PanningPreset,SampleRate,HeadroomDb FROM ExportConfig";
        var list = new List<ExportConfig>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<ExportConfig?> GetByShowAsync(int showId)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,ShowId,Target,PanningPreset,SampleRate,HeadroomDb FROM ExportConfig WHERE ShowId=@sid LIMIT 1";
        cmd.Parameters.AddWithValue("@sid", showId);
        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync()) return Map(r);
        return null;
    }

    public async Task<int> InsertAsync(ExportConfig e)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO ExportConfig(ShowId,Target,PanningPreset,SampleRate,HeadroomDb)
VALUES(@sid,@t,@p,@sr,@h);
SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@sid", e.ShowId);
        cmd.Parameters.AddWithValue("@t", (int)e.Target);
        cmd.Parameters.AddWithValue("@p", e.PanningPreset);
        cmd.Parameters.AddWithValue("@sr", e.SampleRate);
        cmd.Parameters.AddWithValue("@h", e.HeadroomDb);
        var id = (long)await cmd.ExecuteScalarAsync();
        return (int)id;
    }

    public async Task<int> UpdateAsync(ExportConfig e)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
UPDATE ExportConfig
SET ShowId=@sid, Target=@t, PanningPreset=@p, SampleRate=@sr, HeadroomDb=@h
WHERE Id=@id";
        cmd.Parameters.AddWithValue("@sid", e.ShowId);
        cmd.Parameters.AddWithValue("@t", (int)e.Target);
        cmd.Parameters.AddWithValue("@p", e.PanningPreset);
        cmd.Parameters.AddWithValue("@sr", e.SampleRate);
        cmd.Parameters.AddWithValue("@h", e.HeadroomDb);
        cmd.Parameters.AddWithValue("@id", e.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM ExportConfig WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static ExportConfig Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        ShowId = r.GetInt32(1),
        Target = (Target)r.GetInt32(2),
        PanningPreset = r.GetString(3),
        SampleRate = r.GetInt32(4),
        HeadroomDb = r.GetDouble(5)
    };
}
