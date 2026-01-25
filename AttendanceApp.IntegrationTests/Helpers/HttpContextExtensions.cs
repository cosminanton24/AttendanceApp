using System.Text.Json;
 
namespace AttendanceApp.IntegrationTests.Helpers;
 
internal static class HttpContentExtensions
{
    public static JsonSerializerOptions options = new(){ PropertyNameCaseInsensitive = true };
    public static async Task<T> ReadAsAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        
        if (typeof(T) == typeof(string))
        {
            var trimmed = json.Trim();

            if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            {
                var s = JsonSerializer.Deserialize<string>(trimmed, options) ?? string.Empty;
                return (T)(object)s;
            }

            return (T)(object)json;
        }
        return JsonSerializer.Deserialize<T>(json, options)
            ?? throw new InvalidOperationException($"Failed to deserialize response as {typeof(T).Name}");
    }
}