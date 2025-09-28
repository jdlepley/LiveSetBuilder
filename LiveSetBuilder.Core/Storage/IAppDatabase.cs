// LiveSetBuilder.Core/Storage/IAppDatabase.cs
using Microsoft.Data.Sqlite;

namespace LiveSetBuilder.Core.Storage;

public interface IAppDatabase
{
    string DbPath { get; }
    Task InitializeAsync();
    SqliteConnection CreateConnection(); // caller opens/uses/disposes
}
