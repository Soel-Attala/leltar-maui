using Leltar.Core.ViewModels;

namespace Leltar.Views;

// Terméklista oldal (főoldal)
public partial class ProductListPage : ContentPage
{
	private readonly ProductListViewModel _viewModel;

	public ProductListPage(ProductListViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeCommand.ExecuteAsync(null);
	}
}
