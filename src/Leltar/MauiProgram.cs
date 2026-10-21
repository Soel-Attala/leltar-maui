using CommunityToolkit.Mvvm.Messaging;
using Leltar.Core.Data;
using Leltar.Core.Services;
using Leltar.Core.ViewModels;
using Leltar.Services;
using Leltar.Views;
using Microsoft.Extensions.Logging;

namespace Leltar;

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

		// Szolgáltatások: adatbázis, üzenetküldő, navigáció, párbeszédablakok
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, "leltar.db3");
		builder.Services.AddSingleton<IProductRepository>(_ => new SqliteProductRepository(databasePath));
		builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
		builder.Services.AddSingleton<IDialogService, ShellDialogService>();

		// Terméklista: egyetlen példány, így megőrzi a keresést és a görgetési pozíciót
		builder.Services.AddSingleton<ProductListViewModel>();
		builder.Services.AddSingleton<ProductListPage>();

		// Űrlap oldalak: minden navigációnál új példány
		builder.Services.AddTransient<AddProductViewModel>();
		builder.Services.AddTransient<AddProductPage>();
		builder.Services.AddTransient<ProductDetailViewModel>();
		builder.Services.AddTransient<ProductDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
