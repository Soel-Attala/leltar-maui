using Leltar.Core.Data;
using Leltar.Core.Validation;

namespace Leltar.Tests;

// Validátor és számbevitel tesztek
public sealed class ProductValidatorTests
{
    [Theory]
    [InlineData("12,5", 12.5)]
    [InlineData("12.5", 12.5)]
    [InlineData(" 12 500 ", 12500)]
    [InlineData("0", 0)]
    [InlineData("-3", -3)]
    public void DecimalInput_accepts_comma_point_and_spaces(string text, double expected)
    {
        Assert.True(DecimalInput.TryParse(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.234,5")]
    public void DecimalInput_rejects_invalid_text(string? text)
    {
        Assert.False(DecimalInput.TryParse(text, out _));
    }

    [Fact]
    public void Valid_input_is_trimmed_and_parsed()
    {
        var result = ProductValidator.Validate(" Milk ", " 599 ", "459,9", "0");

        Assert.True(result.IsValid);
        Assert.Equal(new ProductInput("Milk", "599", 459.9m, 0m), result.Input);
    }

    [Fact]
    public void Invalid_input_reports_every_field()
    {
        var result = ProductValidator.Validate("", "59 9", "x", "-1");

        Assert.False(result.IsValid);
        Assert.NotNull(result.NameError);
        Assert.NotNull(result.BarcodeError);
        Assert.NotNull(result.PriceError);
        Assert.NotNull(result.QuantityError);
    }

    [Fact]
    public void Demo_barcodes_are_valid_ean13()
    {
        var products = DemoDataGenerator.Generate(200, seed: 42);

        Assert.Equal(200, products.Select(p => p.Barcode).Distinct().Count());
        Assert.All(products, p =>
        {
            Assert.Equal(13, p.Barcode.Length);
            var sum = p.Barcode.Take(12).Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
            Assert.Equal((10 - sum % 10) % 10, p.Barcode[12] - '0');
        });
    }
}
