using SQLite;

namespace Leltar.Core.Models;

// Termék
[Table("Products")]
public class Product
{
    // Azonosító
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Név
    [NotNull, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // Vonalkód (egyedi)
    [NotNull, Unique, MaxLength(64)]
    public string Barcode { get; set; } = string.Empty;

    // Ár
    public decimal Price { get; set; }

    // Készletmennyiség
    public decimal Quantity { get; set; }

    // Keresési név: kisbetűs, ékezet nélküli név – kereséshez és rendezéshez, a repository tölti ki
    [NotNull, Indexed]
    public string SearchName { get; set; } = string.Empty;

    // Elfogyott (készlet = 0)
    [Ignore]
    public bool IsOutOfStock => Quantity <= 0;
}
