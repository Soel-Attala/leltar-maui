using Leltar.Core.Models;

namespace Leltar.Core.Data;

// Demo adatgenerátor – több ezer termék a lista kipróbálásához
public static class DemoDataGenerator
{
    private static readonly string[] Brands =
        ["Alföld", "Pannon", "Tisza", "Balaton", "Mátra", "Danube", "Highland", "Organic", "Premium", "Homemade"];

    private static readonly string[] Items =
    [
        "Milk", "Bread", "Butter", "Cheese", "Yogurt", "Apple", "Pear", "Banana", "Tomato", "Pepper",
        "Potato", "Onion", "Coffee", "Tea", "Sugar", "Flour", "Rice", "Pasta", "Cooking Oil", "Mineral Water",
        "Orange Juice", "Chocolate", "Biscuits", "Salami", "Ham", "Eggs", "Mustard", "Ketchup", "Detergent",
        "Soap", "Toothpaste", "Shampoo", "Paper Towel", "Battery", "Light Bulb",
    ];

    private static readonly string[] Sizes =
        ["100 g", "250 g", "500 g", "1 kg", "0.5 l", "1 l", "1.5 l", "2 l", "6 pcs", "10 pcs"];

    public static IReadOnlyList<Product> Generate(int count, int? seed = null)
    {
        var random = seed is null ? new Random() : new Random(seed.Value);
        var barcodes = new HashSet<string>();
        var products = new List<Product>(count);

        while (products.Count < count)
        {
            var barcode = CreateEan13(random);
            if (!barcodes.Add(barcode))
            {
                continue;
            }

            products.Add(new Product
            {
                Name = $"{Pick(Brands, random)} {Pick(Items, random)} {Pick(Sizes, random)}",
                Barcode = barcode,
                Price = random.Next(99, 5000),
                // Kb. minden tizedik termék elfogyott, hogy a kiemelés látszódjon
                Quantity = random.Next(10) == 0 ? 0 : random.Next(1, 500),
            });
        }

        return products;
    }

    // EAN-13 vonalkód magyar GS1 előtaggal (599) és érvényes ellenőrző számjeggyel
    internal static string CreateEan13(Random random)
    {
        Span<char> digits = stackalloc char[13];
        digits[0] = '5';
        digits[1] = '9';
        digits[2] = '9';
        for (var i = 3; i < 12; i++)
        {
            digits[i] = (char)('0' + random.Next(10));
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        digits[12] = (char)('0' + (10 - sum % 10) % 10);
        return new string(digits);
    }

    private static string Pick(string[] values, Random random) => values[random.Next(values.Length)];
}
