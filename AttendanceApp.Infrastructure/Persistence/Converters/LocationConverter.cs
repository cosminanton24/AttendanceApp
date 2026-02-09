using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AttendanceApp.Domain.Common;
using System.Globalization;

namespace AttendanceApp.Infrastructure.Persistence.Converters;

public sealed class LocationConverter
    : ValueConverter<Location, string>
{
    public LocationConverter()
        : base(
            location => Serialize(location),
            value => Deserialize(value))
    {
    }

    private static string Serialize(Location location)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{location.Latitude},{location.Longitude},{location.Accuracy}"
        );

    private static Location Deserialize(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
            return new Location(0, 0, 0);
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3)
            throw new InvalidOperationException($"Invalid Location value: '{value}'");

        return new Location(
            latitude: double.Parse(parts[0], CultureInfo.InvariantCulture),
            longitude: double.Parse(parts[1], CultureInfo.InvariantCulture),
            accuracy: double.Parse(parts[2], CultureInfo.InvariantCulture)
        );
    }
}
