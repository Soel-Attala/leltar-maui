using Leltar.Core.ViewModels;

namespace Leltar.Views;

// Új termék oldal
public partial class AddProductPage : ContentPage
{
	public AddProductPage(AddProductViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
