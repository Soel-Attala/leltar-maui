using System.Globalization;

namespace Leltar.Core.Validation;

// Ellenőrzött termékadatok
public sealed record ProductInput(string Name, string Barcode, decimal Price, decimal Quantity);

// Validálás eredménye (mezőnkénti hibaüzenetekkel)
public sealed record ProductValidationResult(
    ProductInput? Input,
    string? NameError,
    string? BarcodeError,
    string? PriceError,
    string? QuantityError)
{
    public bool IsValid => Input is not null;
}

// Termék validátor
public static class ProductValidator
{
    public const int MaxNameLength = 200;
    public const int MaxBarcodeLength = 64;

    // Ezzel a vonalkóddal már létezik termék.
    public const string DuplicateBarcodeMessage = "A product with this barcode already exists.";

    public static ProductValidationResult Validate(string? name, string? barcode, string? price, string? quantity)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedBarcode = barcode?.Trim() ?? string.Empty;

        string? nameError = trimmedName.Length switch
        {
            0 => "Name is required.", // A név megadása kötelező.
            > MaxNameLength => $"Name can be at most {MaxNameLength} characters.", // A név legfeljebb 200 karakter lehet.
            _ => null,
        };

        string? barcodeError = null;
        if (trimmedBarcode.Length == 0)
        {
            barcodeError = "Barcode is required."; // A vonalkód megadása kötelező.
        }
        else if (trimmedBarcode.Length > MaxBarcodeLength)
        {
            barcodeError = $"Barcode can be at most {MaxBarcodeLength} characters."; // A vonalkód legfeljebb 64 karakter lehet.
        }
        else if (trimmedBarcode.Any(char.IsWhiteSpace))
        {
            barcodeError = "Barcode cannot contain spaces."; // A vonalkód nem tartalmazhat szóközt.
        }

        var priceError = ValidateNonNegative(
            price, out var parsedPrice,
            "Invalid price.", // Érvénytelen ár.
            "Price cannot be negative."); // Az ár nem lehet negatív.
        var quantityError = ValidateNonNegative(
            quantity, out var parsedQuantity,
            "Invalid quantity.", // Érvénytelen mennyiség.
            "Quantity cannot be negative."); // A mennyiség nem lehet negatív.

        var input = nameError is null && barcodeError is null && priceError is null && quantityError is null
            ? new ProductInput(trimmedName, trimmedBarcode, parsedPrice, parsedQuantity)
            : null;

        return new ProductValidationResult(input, nameError, barcodeError, priceError, quantityError);
    }

    private static string? ValidateNonNegative(string? text, out decimal value, string invalidMessage, string negativeMessage)
    {
        if (!DecimalInput.TryParse(text, out value))
        {
            return invalidMessage;
        }

        return value < 0 ? negativeMessage : null;
    }
}

// Számbevitel értelmezése: tizedesvesszőt és -pontot is elfogad, a szóközöket (ezres tagolás) figyelmen kívül hagyja
public static class DecimalInput
{
    public static bool TryParse(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var cleaned = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray()).Replace(',', '.');
        return decimal.TryParse(
            cleaned,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static string Format(decimal value) => value.ToString("0.####", CultureInfo.CurrentCulture);
}
