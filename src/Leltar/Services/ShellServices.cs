using Leltar.Core.Services;
using Leltar.Views;

namespace Leltar.Services;

// Shell alapú navigáció
public sealed class ShellNavigationService : INavigationService
{
	public Task GoToAddProductAsync() => Shell.Current.GoToAsync(nameof(AddProductPage));

	public Task GoToProductDetailAsync(int productId) =>
		Shell.Current.GoToAsync(
			nameof(ProductDetailPage),
			new ShellNavigationQueryParameters { [ProductDetailPage.ProductIdParameter] = productId });

	public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}

// Shell alapú párbeszédablakok
public sealed class ShellDialogService : IDialogService
{
	public Task ShowErrorAsync(string title, string message) =>
		Shell.Current.DisplayAlertAsync(title, message, "OK");

	public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
		Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
}
