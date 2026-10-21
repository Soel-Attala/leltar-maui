namespace Leltar.Core.Services;

// Navigációs szolgáltatás – a ViewModelek így függetlenek a MAUI Shelltől
public interface INavigationService
{
    // Új termék oldal
    Task GoToAddProductAsync();

    // Termék részletező oldal
    Task GoToProductDetailAsync(int productId);

    // Vissza
    Task GoBackAsync();
}

// Párbeszédablak szolgáltatás
public interface IDialogService
{
    // Hibaüzenet
    Task ShowErrorAsync(string title, string message);

    // Megerősítés kérése
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
