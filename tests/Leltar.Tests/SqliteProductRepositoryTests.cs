using Leltar.Core.Data;
using static Leltar.Tests.TestDatabase;

namespace Leltar.Tests;

// SQLite adattár tesztek
public sealed class SqliteProductRepositoryTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private SqliteProductRepository Repository => _db.Repository;

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    [Fact]
    public async Task Add_then_GetById_returns_saved_values()
    {
        var product = NewProduct("  Milk 1 l ", " 5990000000001 ", 459.9m, 12.5m);

        await Repository.AddAsync(product);
        var loaded = await Repository.GetByIdAsync(product.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Milk 1 l", loaded.Name);
        Assert.Equal("5990000000001", loaded.Barcode);
        Assert.Equal(459.9m, loaded.Price);
        Assert.Equal(12.5m, loaded.Quantity);
    }

    [Fact]
    public async Task Add_with_existing_barcode_throws_DuplicateBarcodeException()
    {
        await Repository.AddAsync(NewProduct("Milk", "123"));

        await Assert.ThrowsAsync<DuplicateBarcodeException>(() => Repository.AddAsync(NewProduct("Bread", "123")));
    }

    [Theory]
    [InlineData("milk")]
    [InlineData("MILK")]
    [InlineData("ásvány")]
    [InlineData("asvany")]
    [InlineData("5990001")]
    public async Task Search_matches_name_case_and_accent_insensitively_and_barcode(string term)
    {
        await Repository.AddAsync(NewProduct("Ásványvíz Milk Edition", "5990001112223"));
        await Repository.AddAsync(NewProduct("Bread", "1111111111111"));

        var results = await Repository.SearchAsync(term, 0, 50);

        var match = Assert.Single(results);
        Assert.Equal("5990001112223", match.Barcode);
        Assert.Equal(1, await Repository.CountAsync(term));
    }

    [Fact]
    public async Task Search_treats_like_wildcards_literally()
    {
        await Repository.AddAsync(NewProduct("Discount 50% off", "1"));
        await Repository.AddAsync(NewProduct("Plain", "2"));

        Assert.Single(await Repository.SearchAsync("%", 0, 50));
        Assert.Empty(await Repository.SearchAsync("_x", 0, 50));
    }

    [Fact]
    public async Task Search_pages_in_name_order()
    {
        await Repository.AddRangeAsync(Enumerable.Range(0, 25).Select(i => NewProduct($"Item {i:D2}", $"B{i}")));

        var first = await Repository.SearchAsync(null, 0, 10);
        var last = await Repository.SearchAsync(null, 20, 10);

        Assert.Equal(10, first.Count);
        Assert.Equal("Item 00", first[0].Name);
        Assert.Equal(5, last.Count);
        Assert.Equal("Item 24", last[^1].Name);
        Assert.Equal(25, await Repository.CountAsync(""));
    }

    [Fact]
    public async Task AddRange_skips_duplicate_barcodes()
    {
        await Repository.AddAsync(NewProduct("Existing", "1"));

        var inserted = await Repository.AddRangeAsync([NewProduct("A", "1"), NewProduct("B", "2")]);

        Assert.Equal(1, inserted);
        Assert.Equal(2, await Repository.CountAsync(null));
    }

    [Fact]
    public async Task Update_persists_changes_and_refreshes_search_name()
    {
        var product = NewProduct("Old name", "1");
        await Repository.AddAsync(product);

        product.Name = "Kávé";
        product.Quantity = 0;
        await Repository.UpdateAsync(product);

        var loaded = await Repository.GetByIdAsync(product.Id);
        Assert.Equal("Kávé", loaded!.Name);
        Assert.True(loaded.IsOutOfStock);
        Assert.Single(await Repository.SearchAsync("kave", 0, 10));
    }

    [Fact]
    public async Task Update_to_existing_barcode_throws_DuplicateBarcodeException()
    {
        await Repository.AddAsync(NewProduct("A", "1"));
        var second = NewProduct("B", "2");
        await Repository.AddAsync(second);

        second.Barcode = "1";

        await Assert.ThrowsAsync<DuplicateBarcodeException>(() => Repository.UpdateAsync(second));
    }

    [Fact]
    public async Task Update_of_missing_product_throws_KeyNotFoundException()
    {
        var ghost = NewProduct("Ghost", "9");
        ghost.Id = 999;

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Repository.UpdateAsync(ghost));
    }

    [Fact]
    public async Task Delete_removes_product()
    {
        var product = NewProduct("A", "1");
        await Repository.AddAsync(product);

        await Repository.DeleteAsync(product.Id);

        Assert.Null(await Repository.GetByIdAsync(product.Id));
    }
}
