using Leltar.Core.Models;
using Leltar.Core.Text;
using SQLite;

namespace Leltar.Core.Data;

// SQLite alapú termék adattár
public sealed class SqliteProductRepository : IProductRepository, IAsyncDisposable
{
    private const string SearchFilter = "WHERE SearchName LIKE ? ESCAPE '\\' OR Barcode LIKE ? ESCAPE '\\'";

    private readonly SQLiteAsyncConnection _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _initialized;

    public SqliteProductRepository(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string? searchTerm, int skip, int take)
    {
        var db = await GetConnectionAsync();
        var (where, args) = BuildFilter(searchTerm);
        var sql = $"SELECT * FROM Products {where} ORDER BY SearchName, Id LIMIT ? OFFSET ?";
        return await db.QueryAsync<Product>(sql, [.. args, take, skip]);
    }

    public async Task<int> CountAsync(string? searchTerm)
    {
        var db = await GetConnectionAsync();
        var (where, args) = BuildFilter(searchTerm);
        return await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Products {where}", args);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<Product>(id);
    }

    public async Task AddAsync(Product product)
    {
        Prepare(product);
        var db = await GetConnectionAsync();
        try
        {
            await db.InsertAsync(product);
        }
        catch (SQLiteException ex) when (IsDuplicateBarcode(ex))
        {
            throw new DuplicateBarcodeException(product.Barcode, ex);
        }
    }

    public async Task<int> AddRangeAsync(IEnumerable<Product> products)
    {
        var prepared = products.Select(Prepare).ToList();
        var db = await GetConnectionAsync();
        var inserted = 0;
        await db.RunInTransactionAsync(connection =>
        {
            foreach (var product in prepared)
            {
                inserted += connection.Insert(product, "OR IGNORE");
            }
        });
        return inserted;
    }

    public async Task UpdateAsync(Product product)
    {
        Prepare(product);
        var db = await GetConnectionAsync();
        int updated;
        try
        {
            updated = await db.UpdateAsync(product);
        }
        catch (SQLiteException ex) when (IsDuplicateBarcode(ex))
        {
            throw new DuplicateBarcodeException(product.Barcode, ex);
        }

        if (updated == 0)
        {
            throw new KeyNotFoundException($"Product {product.Id} does not exist.");
        }
    }

    public async Task DeleteAsync(int id)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync<Product>(id);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseAsync();
        _initLock.Dispose();
    }

    // Lusta inicializálás: első használatkor WAL mód és táblalétrehozás
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_initialized)
        {
            return _connection;
        }

        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await _connection.EnableWriteAheadLoggingAsync();
                await _connection.CreateTableAsync<Product>();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }

        return _connection;
    }

    // Mentés előtti előkészítés: szóközök levágása, keresési név kitöltése
    private static Product Prepare(Product product)
    {
        product.Name = product.Name?.Trim() ?? string.Empty;
        product.Barcode = product.Barcode?.Trim() ?? string.Empty;
        product.SearchName = TextNormalizer.Normalize(product.Name);
        return product;
    }

    private static (string Where, object[] Args) BuildFilter(string? searchTerm)
    {
        var term = TextNormalizer.Normalize(searchTerm);
        if (term.Length == 0)
        {
            return (string.Empty, []);
        }

        var pattern = $"%{EscapeLike(term)}%";
        return (SearchFilter, [pattern, pattern]);
    }

    // A LIKE speciális karaktereinek (%, _) escape-elése
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static bool IsDuplicateBarcode(SQLiteException ex) =>
        ex.Result == SQLite3.Result.Constraint
        && ex.Message.Contains("Products.Barcode", StringComparison.OrdinalIgnoreCase);
}
