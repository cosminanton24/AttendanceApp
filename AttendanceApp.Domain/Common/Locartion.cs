using System.Globalization;

namespace AttendanceApp.Domain.Common;

public readonly record struct Location
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double Accuracy { get; init; }

    public Location(double latitude, double longitude, double accuracy)
    {
        Latitude = latitude;
        Longitude = longitude;
        Accuracy = accuracy;
    }

    public override string ToString()
    {
        return $"{Latitude},{Longitude},{Accuracy}";
    }

    public decimal DistanceTo(Location other)
    {
        const double EarthRadiusMeters = 6371000; // meters

        var lat1Rad = ToRadians(Latitude);
        var lat2Rad = ToRadians(other.Latitude);

        var deltaLat = ToRadians(other.Latitude - Latitude);
        var deltaLon = ToRadians(other.Longitude - Longitude);

        var a =
            Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
            Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
            Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (decimal)(EarthRadiusMeters * c);
    }

    private static double ToRadians(double degrees)
        => degrees * Math.PI / 180.0;

    public static Location FromString(string value)
    {
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