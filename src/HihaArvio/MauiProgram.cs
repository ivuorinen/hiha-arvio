using HihaArvio.Services;
using HihaArvio.Services.Interfaces;
using HihaArvio.ViewModels;
using Microsoft.Extensions.Logging;

namespace HihaArvio;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Services
		builder.Services.AddSingleton<IEstimateService, EstimateService>();
		builder.Services.AddSingleton<IShakeDetectionService, ShakeDetectionService>();

		// Storage service with app data path
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "hihaarvio.db");
		builder.Services.AddSingleton<IStorageService>(sp => new StorageService(dbPath));

		// Register ViewModels
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<HistoryViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Register Pages
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<HistoryPage>();
		builder.Services.AddTransient<SettingsPage>();

		return builder.Build();
	}
}
