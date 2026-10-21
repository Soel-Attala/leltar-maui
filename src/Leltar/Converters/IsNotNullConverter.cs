using System.Globalization;

namespace Leltar.Converters;

// Igaz, ha az érték nem null (hibaüzenetek megjelenítéséhez)
public sealed class IsNotNullConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
