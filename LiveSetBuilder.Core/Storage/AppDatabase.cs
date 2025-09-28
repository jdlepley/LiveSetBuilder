// LiveSetBuilder.Core/Storage/AppDatabase.cs
using Microsoft.Data.Sqlite;
using LiveSetBuilder.Core.Models;

namespace LiveSetBuilder.Core.Storage;

public sealed class AppDatabase : IAppDatabase
{
    public string DbPath { get; }
    public AppDatabase(string dbPath) => DbPath = dbPath;

    public SqliteConnection CreateConnection()
        => new($"Data Source={DbPath}");

    public async Task InitializeAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
PRAGMA journal_mode=WAL;
CREATE TABLE IF NOT EXISTS Show(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Title TEXT NOT NULL,
  DefaultBpm REAL NULL,
  DefaultTimeSig TEXT NULL,
  UpdatedAt TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_Show_UpdatedAt ON Show(UpdatedAt DESC);

CREATE TABLE IF NOT EXISTS Song(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  ShowId INTEGER NOT NULL,
  Title TEXT NOT NULL,
  Bpm REAL NULL,
  TimeSig TEXT NULL,
  StartGapBars REAL NOT NULL DEFAULT 0,
  OrderIndex INTEGER NOT NULL DEFAULT 0,
  Notes TEXT NULL,
  FOREIGN KEY(ShowId) REFERENCES Show(Id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS IX_Song_Show_Order ON Song(ShowId, OrderIndex);

CREATE TABLE IF NOT EXISTS Asset(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  SongId INTEGER NOT NULL,
  Kind INTEGER NOT NULL,
  SourcePath TEXT NOT NULL,
  SampleRate INTEGER NOT NULL,
  BitDepth INTEGER NOT NULL,
  DurationSec REAL NOT NULL,
  MetadataJson TEXT NULL,
  FOREIGN KEY(SongId) REFERENCES Song(Id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS IX_Asset_Song ON Asset(SongId);

CREATE TABLE IF NOT EXISTS MixItem(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  SongId INTEGER NOT NULL,
  AssetId INTEGER NOT NULL,
  StartBeat REAL NOT NULL,
  LengthBeats REAL NOT NULL,
  GainDb REAL NOT NULL,
  Pan REAL NOT NULL,
  Role INTEGER NOT NULL,
  FOREIGN KEY(SongId) REFERENCES Song(Id) ON DELETE CASCADE,
  FOREIGN KEY(AssetId) REFERENCES Asset(Id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS IX_MixItem_Song ON MixItem(SongId);

CREATE TABLE IF NOT EXISTS ExportConfig(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  ShowId INTEGER NOT NULL,
  Target INTEGER NOT NULL,
  PanningPreset TEXT NOT NULL,
  SampleRate INTEGER NOT NULL,
  HeadroomDb REAL NOT NULL,
  FOREIGN KEY(ShowId) REFERENCES Show(Id) ON DELETE CASCADE
);
";
        await cmd.ExecuteNonQueryAsync();
    }
}
