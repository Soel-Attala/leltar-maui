using CommunityToolkit.Mvvm.Messaging;
using Leltar.Core.Messages;
using Leltar.Core.Validation;
using Leltar.Core.ViewModels;
using static Leltar.Tests.TestDatabase;

namespace Leltar.Tests;

// ViewModel tesztek valódi (ideiglenes) adatbázissal
public sealed class ViewModelTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeDialogService _dialogs = new();
    // Saját messenger példány, hogy a tesztek ne hassanak egymásra
    private readonly IMessenger _messenger = new StrongReferenceMessenger();

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    private ProductListViewModel CreateList() =>
        new(_db.Repository, _navigation, _dialogs, _messenger) { SearchDelay = Timeout.InfiniteTimeSpan };

    [Fact]
    public async Task List_loads_pages_until_all_items_are_shown()
    {
        await _db.Repository.AddRangeAsync(Enumerable.Range(0, 120).Select(i => NewProduct($"P{i:D3}", $"B{i}")));
        var list = CreateList();

        await list.InitializeCommand.ExecuteAsync(null);
        Assert.Equal(ProductListViewModel.PageSize, list.Products.Count);
        Assert.Equal(120, list.TotalCount);

        await list.LoadMoreCommand.ExecuteAsync(null);
        await list.LoadMoreCommand.ExecuteAsync(null);
        await list.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(120, list.Products.Count);
        Assert.Equal(120, list.Products.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public async Task List_search_filters_results()
    {
        await _db.Repository.AddRangeAsync([NewProduct("Milk", "1"), NewProduct("Bread", "2")]);
        var list = CreateList();

        list.SearchText = "bre";
        await list.RefreshAsync();

        Assert.Equal("Bread", Assert.Single(list.Products).Name);
        Assert.Equal(1, list.TotalCount);
    }

    [Fact]
    public async Task List_reacts_to_update_and_delete_messages()
    {
        var milk = NewProduct("Milk", "1");
        await _db.Repository.AddRangeAsync([milk, NewProduct("Bread", "2")]);
        var list = CreateList();
        await list.RefreshAsync();

        var updated = NewProduct("Milk", "1", quantity: 0);
        updated.Id = milk.Id;
        _messenger.Send(new ProductUpdatedMessage(updated));
        Assert.Same(updated, list.Products.Single(p => p.Id == milk.Id));

        _messenger.Send(new ProductDeletedMessage(milk.Id));
        Assert.DoesNotContain(list.Products, p => p.Id == milk.Id);
        Assert.Equal(1, list.TotalCount);
    }

    [Fact]
    public async Task Add_with_invalid_input_shows_errors_and_saves_nothing()
    {
        var add = new AddProductViewModel(_db.Repository, _navigation, _dialogs, _messenger)
        {
            Name = "", Barcode = "1", PriceText = "abc", QuantityText = "1",
        };

        await add.SaveCommand.ExecuteAsync(null);

        Assert.NotNull(add.NameError);
        Assert.NotNull(add.PriceError);
        Assert.Equal(0, await _db.Repository.CountAsync(null));
        Assert.Equal(0, _navigation.BackCount);
    }

    [Fact]
    public async Task Add_saves_product_notifies_list_and_navigates_back()
    {
        ProductAddedMessage? received = null;
        _messenger.Register<ProductAddedMessage>(this, (_, m) => received = m);
        var add = new AddProductViewModel(_db.Repository, _navigation, _dialogs, _messenger)
        {
            Name = "Coffee", Barcode = "5990000000001", PriceText = "1299,5", QuantityText = "0",
        };

        await add.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(await _db.Repository.SearchAsync("coffee", 0, 10));
        Assert.Equal(1299.5m, saved.Price);
        Assert.Equal(saved.Id, received?.Product.Id);
        Assert.Equal(1, _navigation.BackCount);
    }

    [Fact]
    public async Task Add_with_duplicate_barcode_shows_barcode_error()
    {
        await _db.Repository.AddAsync(NewProduct("Existing", "123"));
        var add = new AddProductViewModel(_db.Repository, _navigation, _dialogs, _messenger)
        {
            Name = "New", Barcode = "123", PriceText = "1", QuantityText = "1",
        };

        await add.SaveCommand.ExecuteAsync(null);

        Assert.Equal(ProductValidator.DuplicateBarcodeMessage, add.BarcodeError);
        Assert.Equal(0, _navigation.BackCount);
    }

    [Fact]
    public async Task Detail_loads_and_updates_product()
    {
        var product = NewProduct("Tea", "7", 500, 3);
        await _db.Repository.AddAsync(product);
        var detail = new ProductDetailViewModel(_db.Repository, _navigation, _dialogs, _messenger);

        await detail.LoadAsync(product.Id);
        Assert.Equal("Tea", detail.Name);
        Assert.True(detail.SaveCommand.CanExecute(null));

        detail.Name = "Green Tea";
        detail.QuantityText = "0";
        await detail.SaveCommand.ExecuteAsync(null);

        var loaded = await _db.Repository.GetByIdAsync(product.Id);
        Assert.Equal("Green Tea", loaded!.Name);
        Assert.Equal(0m, loaded.Quantity);
        Assert.Equal(1, _navigation.BackCount);
    }

    [Fact]
    public async Task Detail_delete_removes_product_after_confirmation()
    {
        var product = NewProduct("Tea", "7");
        await _db.Repository.AddAsync(product);
        var detail = new ProductDetailViewModel(_db.Repository, _navigation, _dialogs, _messenger);
        await detail.LoadAsync(product.Id);

        await detail.DeleteCommand.ExecuteAsync(null);

        Assert.Null(await _db.Repository.GetByIdAsync(product.Id));
        Assert.Equal(1, _navigation.BackCount);
    }

    [Fact]
    public async Task Detail_for_missing_product_shows_error_and_goes_back()
    {
        var detail = new ProductDetailViewModel(_db.Repository, _navigation, _dialogs, _messenger);

        await detail.LoadAsync(12345);

        Assert.Single(_dialogs.Errors);
        Assert.Equal(1, _navigation.BackCount);
        Assert.False(detail.SaveCommand.CanExecute(null));
    }
}
