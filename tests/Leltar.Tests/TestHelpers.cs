using Leltar.Core.Data;
using Leltar.Core.Models;
using Leltar.Core.Services;

namespace Leltar.Tests;

// Ideiglenes SQLite adatbázis tesztenként
public sealed class TestDatabase : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"leltar-test-{Guid.NewGuid():N}.db3");

    public SqliteProductRepository Repository { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Repository = new SqliteProductRepository(_path);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Repository.DisposeAsync();
        foreach (var file in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
        }
    }

    public static Product NewProduct(string name, string barcode, decimal price = 100, decimal quantity = 1) =>
        new() { Name = name, Barcode = barcode, Price = price, Quantity = quantity };
}

// Navigációs teszt-dublőr
public sealed class FakeNavigationService : INavigationService
{
    public int BackCount { get; private set; }
    public int AddProductCount { get; private set; }
    public int? LastDetailId { get; private set; }

    public Task GoToAddProductAsync()
    {
        AddProductCount++;
        return Task.CompletedTask;
    }

    public Task GoToProductDetailAsync(int productId)
    {
        LastDetailId = productId;
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        BackCount++;
        return Task.CompletedTask;
    }
}

// Párbeszédablak teszt-dublőr
public sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;
    public List<string> Errors { get; } = [];

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add($"{title}: {message}");
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        Task.FromResult(ConfirmResult);
}

