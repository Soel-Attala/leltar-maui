using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Leltar.Core.Data;
using Leltar.Core.Messages;
using Leltar.Core.Models;
using Leltar.Core.Services;
using Leltar.Core.Validation;

namespace Leltar.Core.ViewModels;

// Termék részletező / módosító ViewModel
public sealed partial class ProductDetailViewModel(
    IProductRepository repository,
    INavigationService navigation,
    IDialogService dialogs,
    IMessenger messenger)
    : ProductFormViewModel(repository, navigation, dialogs, messenger)
{
    public override string Title => "Product details"; // Termék adatai

    // Az adatbázisban tárolt (utoljára mentett) állapot
    [ObservableProperty]
    private Product? _product;

    protected override bool CanSaveCore => Product is not null;

    partial void OnProductChanged(Product? value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    protected override void OnBusyChanged() => DeleteCommand.NotifyCanExecuteChanged();

    // Termék betöltése azonosító alapján
    public async Task LoadAsync(int productId)
    {
        IsBusy = true;
        try
        {
            var product = await Repository.GetByIdAsync(productId);
            if (product is null)
            {
                await Dialogs.ShowErrorAsync(
                    "Product not found", // A termék nem található
                    "It may have been deleted in the meantime."); // Lehet, hogy időközben törölték.
                await Navigation.GoBackAsync();
                return;
            }

            Product = product;
            Fill(product);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Could not load product", ex.Message); // Nem sikerült betölteni a terméket
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override async Task PersistAsync(ProductInput input)
    {
        var current = Product ?? throw new InvalidOperationException("No product loaded.");
        var updated = new Product
        {
            Id = current.Id,
            Name = input.Name,
            Barcode = input.Barcode,
            Price = input.Price,
            Quantity = input.Quantity,
        };

        await Repository.UpdateAsync(updated);
        Product = updated;
        Messenger.Send(new ProductUpdatedMessage(updated));
    }

    // Törlés
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (Product is not { } product)
        {
            return;
        }

        var confirmed = await Dialogs.ConfirmAsync(
            "Delete product", // Termék törlése
            $"Are you sure you want to delete \"{product.Name}\"?", // Biztosan törlöd ezt a terméket?
            "Delete", // Törlés
            "Cancel"); // Mégse
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Repository.DeleteAsync(product.Id);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Delete failed", ex.Message); // A törlés nem sikerült
            return;
        }
        finally
        {
            IsBusy = false;
        }

        Messenger.Send(new ProductDeletedMessage(product.Id));
        await Navigation.GoBackAsync();
    }

    private bool CanDelete() => Product is not null && !IsBusy;
}
