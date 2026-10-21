using Leltar.Core.ViewModels;

namespace Leltar.Views;

// Termék részletező oldal – a termék azonosítóját navigációs paraméterként kapja
public partial class ProductDetailPage : ContentPage, IQueryAttributable
{
	public const string ProductIdParameter = "id";

	private readonly ProductDetailViewModel _viewModel;

	public ProductDetailPage(ProductDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	public async void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue(ProductIdParameter, out var value) && value is int productId)
		{
			await _viewModel.LoadAsync(productId);
		}
	}
}
