using CommunityToolkit.Mvvm.DependencyInjection;
using LiveSetBuilder.App.Pages;
using LiveSetBuilder.App.Services;
using LiveSetBuilder.App.ViewModels;
using LiveSetBuilder.Core.Services;
using LiveSetBuilder.Core.Storage;
using LiveSetBuilder.Core.Util;

#if WINDOWS
using LiveSetBuilder.Platform.Audio;
#endif

namespace LiveSetBuilder.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        // Core services & DB
        builder.Services.AddSingleton<IAppDatabase>(sp =>
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "livesetbuilder.db");
            return new AppDatabase(path);
        });

        // Core repos
        builder.Services.AddSingleton<ShowRepository>();
        builder.Services.AddSingleton<SongRepository>();
        builder.Services.AddSingleton<AssetRepository>();
        builder.Services.AddSingleton<MixItemRepository>();
        builder.Services.AddSingleton<ExportConfigRepository>();

        // Core services
        builder.Services.AddSingleton<IngestService>();
        builder.Services.AddSingleton<AnalysisService>();
        builder.Services.AddSingleton<RenderService>(sp =>
            new RenderService(new LiveSetBuilder.Core.Util.FfmpegLocator().FfmpegPath));
        builder.Services.AddSingleton<ExportService>();
        builder.Services.AddSingleton<WaveformService>();

        #if WINDOWS
        builder.Services.AddSingleton<IAudioPreview, WasapiPreview>();
        #endif


        // App-layer helpers
        builder.Services.AddSingleton<AssetCopyService>(); // copies bundled assets to AppData
        builder.Services.AddSingleton<PresetService>(sp =>
            new PresetService(sp.GetRequiredService<AssetCopyService>().EnsureAssetCopiedAsync));

        // ViewModels
        builder.Services.AddTransient<ShowsViewModel>();
        builder.Services.AddTransient<SetEditorViewModel>();
        builder.Services.AddTransient<RunnerViewModel>();

        // Pages
        builder.Services.AddTransient<ShowsPage>();
        builder.Services.AddTransient<SetEditorPage>();
        builder.Services.AddTransient<RunnerPage>();

        var app = builder.Build();

        // Initialize DB & presets at startup
        Task.Run(async () =>
        {
            var db = app.Services.GetRequiredService<IAppDatabase>();
            await db.InitializeAsync();
            var presets = app.Services.GetRequiredService<PresetService>();
            await presets.InitializeAsync();
        });

        return app;
    }
}
