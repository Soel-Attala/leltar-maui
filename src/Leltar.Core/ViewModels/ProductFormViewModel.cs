using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Leltar.Core.Data;
using Leltar.Core.Models;
using Leltar.Core.Services;
using Leltar.Core.Validation;

namespace Leltar.Core.ViewModels;

// Termék űrlap közös alaposztálya (felvétel és módosítás)
public abstract partial class ProductFormViewModel : ObservableObject
{
    protected ProductFormViewModel(
        IProductRepository repository,
        INavigationService navigation,
        IDialogService dialogs,
        IMessenger messenger)
    {
        Repository = repository;
        Navigation = navigation;
        Dialogs = dialogs;
        Messenger = messenger;
    }

    protected IProductRepository Repository { get; }
    protected INavigationService Navigation { get; }
    protected IDialogService Dialogs { get; }
    protected IMessenger Messenger { get; }

    // Oldal címe
    public abstract string Title { get; }

    // Név
    [ObservableProperty]
    private string _name = string.Empty;

    // Vonalkód
    [ObservableProperty]
    private string _barcode = string.Empty;

    // Ár (szövegként, a beviteli mezőhöz)
    [ObservableProperty]
    private string _priceText = string.Empty;

    // Mennyiség (szövegként, a beviteli mezőhöz)
    [ObservableProperty]
    private string _quantityText = string.Empty;

    // Mezőnkénti hibaüzenetek
    [ObservableProperty]
    private string? _nameError;

    [ObservableProperty]
    private string? _barcodeError;

    [ObservableProperty]
    private string? _priceError;

    [ObservableProperty]
    private string? _quantityError;

    // Foglalt (mentés / betöltés folyamatban)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    // Gépeléskor eltűnik az adott mező hibaüzenete
    partial void OnNameChanged(string value) => NameError = null;
    partial void OnBarcodeChanged(string value) => BarcodeError = null;
    partial void OnPriceTextChanged(string value) => PriceError = null;
    partial void OnQuantityTextChanged(string value) => QuantityError = null;
    partial void OnIsBusyChanged(bool value) => OnBusyChanged();

    // Mentés
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var result = ProductValidator.Validate(Name, Barcode, PriceText, QuantityText);
        NameError = result.NameError;
        BarcodeError = result.BarcodeError;
        PriceError = result.PriceError;
        QuantityError = result.QuantityError;
        if (result.Input is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await PersistAsync(result.Input);
        }
        catch (DuplicateBarcodeException)
        {
            BarcodeError = ProductValidator.DuplicateBarcodeMessage;
            return;
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Save failed", ex.Message); // A mentés nem sikerült
            return;
        }
        finally
        {
            IsBusy = false;
        }

        await Navigation.GoBackAsync();
    }

    // Az adatok tényleges mentése (leszármazott osztályban)
    protected abstract Task PersistAsync(ProductInput input);

    protected virtual bool CanSaveCore => true;

    protected virtual void OnBusyChanged()
    {
    }

    // Mezők kitöltése egy meglévő termékből
    protected void Fill(Product product)
    {
        Name = product.Name;
        Barcode = product.Barcode;
        PriceText = DecimalInput.Format(product.Price);
        QuantityText = DecimalInput.Format(product.Quantity);
        NameError = BarcodeError = PriceError = QuantityError = null;
    }

    private bool CanSave() => !IsBusy && CanSaveCore;
}
