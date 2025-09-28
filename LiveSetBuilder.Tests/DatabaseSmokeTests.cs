using LiveSetBuilder.Core.Models;
using LiveSetBuilder.Core.Storage;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace LiveSetBuilder.Tests;

public class DatabaseSmokeTests
{
    [Fact]
    public async Task Can_Create_Show_And_Query_Back()
    {
        // Use a temp DB path
        var dbPath = Path.Combine(Path.GetTempPath(), $"lsb_test_{Guid.NewGuid():N}.db");
        var db = new AppDatabase(dbPath);
        await db.InitializeAsync();

        var shows = new ShowRepository(db);
        var id = await shows.InsertAsync(new Show { Title = "Test Show", DefaultBpm = 120, DefaultTimeSig = "4/4" });

        var got = await shows.GetAsync(id);
        Assert.NotNull(got);
        Assert.Equal("Test Show", got!.Title);

        // cleanup
        try { File.Delete(dbPath); } catch { /* ignore */ }
    }
}
