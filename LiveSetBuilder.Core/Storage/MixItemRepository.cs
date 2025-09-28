// LiveSetBuilder.Core/Storage/MixItemRepository.cs
using Microsoft.Data.Sqlite;
using LiveSetBuilder.Core.Models;

namespace LiveSetBuilder.Core.Storage;

public sealed class MixItemRepository : IRepository<MixItem>
{
    private readonly IAppDatabase _db;
    public MixItemRepository(IAppDatabase db) => _db = db;

    public async Task<MixItem?> GetAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,SongId,AssetId,StartBeat,LengthBeats,GainDb,Pan,Role FROM MixItem WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync()) return Map(r);
        return null;
    }

    public async Task<List<MixItem>> GetAllAsync()
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,SongId,AssetId,StartBeat,LengthBeats,GainDb,Pan,Role FROM MixItem";
        var list = new List<MixItem>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<List<MixItem>> GetBySongAsync(int songId)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,SongId,AssetId,StartBeat,LengthBeats,GainDb,Pan,Role FROM MixItem WHERE SongId=@sid ORDER BY StartBeat";
        cmd.Parameters.AddWithValue("@sid", songId);
        var list = new List<MixItem>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<int> InsertAsync(MixItem m)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO MixItem(SongId,AssetId,StartBeat,LengthBeats,GainDb,Pan,Role)
VALUES(@sid,@aid,@sb,@lb,@g,@p,@r);
SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@sid", m.SongId);
        cmd.Parameters.AddWithValue("@aid", m.AssetId);
        cmd.Parameters.AddWithValue("@sb", m.StartBeat);
        cmd.Parameters.AddWithValue("@lb", m.LengthBeats);
        cmd.Parameters.AddWithValue("@g", m.GainDb);
        cmd.Parameters.AddWithValue("@p", m.Pan);
        cmd.Parameters.AddWithValue("@r", (int)m.Role);
        var id = (long)await cmd.ExecuteScalarAsync();
        return (int)id;
    }

    public async Task<int> UpdateAsync(MixItem m)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
UPDATE MixItem
SET SongId=@sid, AssetId=@aid, StartBeat=@sb, LengthBeats=@lb, GainDb=@g, Pan=@p, Role=@r
WHERE Id=@id";
        cmd.Parameters.AddWithValue("@sid", m.SongId);
        cmd.Parameters.AddWithValue("@aid", m.AssetId);
        cmd.Parameters.AddWithValue("@sb", m.StartBeat);
        cmd.Parameters.AddWithValue("@lb", m.LengthBeats);
        cmd.Parameters.AddWithValue("@g", m.GainDb);
        cmd.Parameters.AddWithValue("@p", m.Pan);
        cmd.Parameters.AddWithValue("@r", (int)m.Role);
        cmd.Parameters.AddWithValue("@id", m.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM MixItem WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static MixItem Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        SongId = r.GetInt32(1),
        AssetId = r.GetInt32(2),
        StartBeat = r.GetDouble(3),
        LengthBeats = r.GetDouble(4),
        GainDb = r.GetDouble(5),
        Pan = r.GetDouble(6),
        Role = (MixRole)r.GetInt32(7)
    };
}
