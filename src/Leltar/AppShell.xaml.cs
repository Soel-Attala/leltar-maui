using Leltar.Views;

namespace Leltar;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Útvonalak: új termék, termék részletei
		Routing.RegisterRoute(nameof(AddProductPage), typeof(AddProductPage));
		Routing.RegisterRoute(nameof(ProductDetailPage), typeof(ProductDetailPage));
	}
}
