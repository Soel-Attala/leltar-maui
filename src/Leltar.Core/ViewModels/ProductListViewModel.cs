using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Leltar.Core.Data;
using Leltar.Core.Messages;
using Leltar.Core.Models;
using Leltar.Core.Services;

namespace Leltar.Core.ViewModels;

// Terméklista ViewModel – lapozott betöltés, késleltetett (debounce) keresés
public partial class ProductListViewModel : ObservableObject,
    IRecipient<ProductAddedMessage>,
    IRecipient<ProductUpdatedMessage>,
    IRecipient<ProductDeletedMessage>
{
    public const int PageSize = 50;
    public const int DemoProductCount = 5000;

    private readonly IProductRepository _repository;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    private CancellationTokenSource? _searchDebounce;
    // Lekérdezés verziószám: az elavult (régebbi keresésből érkező) eredményeket eldobjuk
    private int _queryVersion;
    private bool _hasMoreItems;
    private bool _isInitialized;

    public ProductListViewModel(
        IProductRepository repository,
        INavigationService navigation,
        IDialogService dialogs,
        IMessenger messenger)
    {
        _repository = repository;
        _navigation = navigation;
        _dialogs = dialogs;
        messenger.RegisterAll(this);
    }

    // Termékek
    public ObservableCollection<Product> Products { get; } = [];

    // Keresési idő késleltetés
    public TimeSpan SearchDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    // Keresett szöveg
    [ObservableProperty]
    private string _searchText = string.Empty;

    // Frissítés folyamatban
    [ObservableProperty]
    private bool _isRefreshing;

    // További elemek betöltése folyamatban
    [ObservableProperty]
    private bool _isLoadingMore;

    // Találatok száma
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCountText))]
    private int _totalCount;

    // "1 product" / "N products" (1 termék / N termék)
    public string TotalCountText => TotalCount == 1 ? "1 product" : $"{TotalCount} products";

    // Üres lista üzenete
    [ObservableProperty]
    private string _emptyMessage = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = new CancellationTokenSource();
        _ = RefreshAfterDelayAsync(_searchDebounce.Token);
    }

    // Első megjelenéskor tölt be
    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await RefreshAsync();
    }

    // Első oldal újratöltése
    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task RefreshAsync()
    {
        var version = ++_queryVersion;
        var term = SearchText;
        IsRefreshing = true;
        try
        {
            var total = await _repository.CountAsync(term);
            var page = await _repository.SearchAsync(term, 0, PageSize);
            if (version != _queryVersion)
            {
                return;
            }

            Products.Clear();
            foreach (var product in page)
            {
                Products.Add(product);
            }

            TotalCount = total;
            _hasMoreItems = page.Count == PageSize;
            EmptyMessage = string.IsNullOrWhiteSpace(term)
                ? "No products yet. Add one or load demo data." // Még nincs termék. Vegyél fel egyet, vagy tölts be demo adatokat.
                : "No products match your search."; // Nincs a keresésnek megfelelő termék.
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Could not load products", ex.Message); // Nem sikerült betölteni a termékeket
        }
        finally
        {
            if (version == _queryVersion)
            {
                IsRefreshing = false;
            }
        }
    }

    // Következő oldal betöltése (görgetéskor, a lista végéhez közeledve)
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!_hasMoreItems || IsLoadingMore || IsRefreshing)
        {
            return;
        }

        var version = _queryVersion;
        IsLoadingMore = true;
        try
        {
            var page = await _repository.SearchAsync(SearchText, Products.Count, PageSize);
            if (version != _queryVersion)
            {
                return;
            }

            foreach (var product in page)
            {
                Products.Add(product);
            }

            _hasMoreItems = page.Count == PageSize;
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Could not load products", ex.Message); // Nem sikerült betölteni a termékeket
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    // Új termék
    [RelayCommand]
    private Task AddProductAsync() => _navigation.GoToAddProductAsync();

    // Termék kiválasztása -> részletező oldal
    [RelayCommand]
    private Task SelectProductAsync(Product? product) =>
        product is null ? Task.CompletedTask : _navigation.GoToProductDetailAsync(product.Id);

    // Demo adatok generálása
    [RelayCommand]
    private async Task GenerateDemoDataAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Demo data", // Demo adatok
            $"{DemoProductCount} random products will be added to the database. Continue?", // 5000 véletlenszerű termék kerül az adatbázisba. Folytatod?
            "Add", // Hozzáadás
            "Cancel"); // Mégse
        if (!confirmed)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            await _repository.AddRangeAsync(DemoDataGenerator.Generate(DemoProductCount));
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Could not add demo data", ex.Message); // Nem sikerült a demo adatok hozzáadása
        }

        await RefreshAsync();
    }

    // Új termék: a rendezett listában a helye nem ismert, ezért újratöltjük
    public void Receive(ProductAddedMessage message) => _ = RefreshAsync();

    // Módosított termék: helyben cseréljük, a görgetési pozíció megmarad
    public void Receive(ProductUpdatedMessage message)
    {
        var index = IndexOf(message.Product.Id);
        if (index >= 0)
        {
            Products[index] = message.Product;
        }
    }

    // Törölt termék: eltávolítjuk a listából
    public void Receive(ProductDeletedMessage message)
    {
        var index = IndexOf(message.ProductId);
        if (index >= 0)
        {
            Products.RemoveAt(index);
            TotalCount--;
        }
    }

    private async Task RefreshAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SearchDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await RefreshAsync();
    }

    private int IndexOf(int productId)
    {
        for (var i = 0; i < Products.Count; i++)
        {
            if (Products[i].Id == productId)
            {
                return i;
            }
        }

        return -1;
    }
}
