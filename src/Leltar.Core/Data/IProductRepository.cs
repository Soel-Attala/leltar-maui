using Leltar.Core.Models;

namespace Leltar.Core.Data;

// Termék adattár
public interface IProductRepository
{
    // Keresés név vagy vonalkód alapján, lapozva, név szerint rendezve (üres kifejezés = összes termék)
    Task<IReadOnlyList<Product>> SearchAsync(string? searchTerm, int skip, int take);

    // Találatok száma
    Task<int> CountAsync(string? searchTerm);

    // Lekérés azonosító alapján
    Task<Product?> GetByIdAsync(int id);

    // Új termék felvétele – DuplicateBarcodeException, ha a vonalkód már létezik
    Task AddAsync(Product product);

    // Tömeges felvétel egy tranzakcióban, a duplikált vonalkódokat kihagyja; a beszúrt sorok számát adja vissza
    Task<int> AddRangeAsync(IEnumerable<Product> products);

    // Módosítás – DuplicateBarcodeException vagy KeyNotFoundException, ha a termék már nem létezik
    Task UpdateAsync(Product product);

    // Törlés
    Task DeleteAsync(int id);
}
