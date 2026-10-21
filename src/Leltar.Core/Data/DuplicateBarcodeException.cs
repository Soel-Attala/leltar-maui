namespace Leltar.Core.Data;

// Duplikált vonalkód kivétel
public sealed class DuplicateBarcodeException(string barcode, Exception? innerException = null)
    : Exception($"A product with barcode '{barcode}' already exists.", innerException)
{
    public string Barcode { get; } = barcode;
}
