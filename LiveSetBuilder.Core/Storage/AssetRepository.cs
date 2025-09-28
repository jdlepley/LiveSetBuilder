// LiveSetBuilder.Core/Storage/AssetRepository.cs
using Microsoft.Data.Sqlite;
using LiveSetBuilder.Core.Models;

namespace LiveSetBuilder.Core.Storage;

public sealed class AssetRepository : IRepository<Asset>
{
    private readonly IAppDatabase _db;
    public AssetRepository(IAppDatabase db) => _db = db;

    public async Task<Asset?> GetAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,SongId,Kind,SourcePath,SampleRate,BitDepth,DurationSec,MetadataJson FROM Asset WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync()) return Map(r);
        return null;
    }

    public async Task<List<Asset>> GetAllAsync()
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,SongId,Kind,SourcePath,SampleRate,BitDepth,DurationSec,MetadataJson FROM Asset";
        var list = new List<Asset>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<List<Asset>> GetBySongAsync(int songId)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,SongId,Kind,SourcePath,SampleRate,BitDepth,DurationSec,MetadataJson FROM Asset WHERE SongId=@sid";
        cmd.Parameters.AddWithValue("@sid", songId);
        var list = new List<Asset>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<int> InsertAsync(Asset a)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Asset(SongId,Kind,SourcePath,SampleRate,BitDepth,DurationSec,MetadataJson)
VALUES(@sid,@k,@p,@sr,@bd,@dur,@meta);
SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@sid", a.SongId);
        cmd.Parameters.AddWithValue("@k", (int)a.Kind);
        cmd.Parameters.AddWithValue("@p", a.SourcePath);
        cmd.Parameters.AddWithValue("@sr", a.SampleRate);
        cmd.Parameters.AddWithValue("@bd", a.BitDepth);
        cmd.Parameters.AddWithValue("@dur", a.DurationSec);
        cmd.Parameters.AddWithValue("@meta", (object?)a.MetadataJson ?? DBNull.Value);
        var id = (long)await cmd.ExecuteScalarAsync();
        return (int)id;
    }

    public async Task<int> UpdateAsync(Asset a)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
UPDATE Asset SET SongId=@sid, Kind=@k, SourcePath=@p, SampleRate=@sr, BitDepth=@bd, DurationSec=@dur, MetadataJson=@meta
WHERE Id=@id";
        cmd.Parameters.AddWithValue("@sid", a.SongId);
        cmd.Parameters.AddWithValue("@k", (int)a.Kind);
        cmd.Parameters.AddWithValue("@p", a.SourcePath);
        cmd.Parameters.AddWithValue("@sr", a.SampleRate);
        cmd.Parameters.AddWithValue("@bd", a.BitDepth);
        cmd.Parameters.AddWithValue("@dur", a.DurationSec);
        cmd.Parameters.AddWithValue("@meta", (object?)a.MetadataJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", a.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var c = _db.CreateConnection(); await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM Asset WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static Asset Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        SongId = r.GetInt32(1),
        Kind = (AssetKind)r.GetInt32(2),
        SourcePath = r.GetString(3),
        SampleRate = r.GetInt32(4),
        BitDepth = r.GetInt32(5),
        DurationSec = r.GetDouble(6),
        MetadataJson = r.IsDBNull(7) ? null : r.GetString(7)
    };
}
