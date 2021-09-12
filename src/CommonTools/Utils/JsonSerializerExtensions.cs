using System.Text.Json;

namespace CommonTools.Utils
{
    public static class JsonSerializerExtensions
    {
        public static JsonSerializerOptions DefaultOptions => new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = false,
            IgnoreNullValues = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = false,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static string Serialize<T>(this T obj, JsonSerializerOptions options = null) where T : class, new()
        {
            return JsonSerializer.Serialize(obj, options ?? DefaultOptions).Replace("\r\n", "\n");
        }

        public static T Deserialize<T>(this string json, JsonSerializerOptions options = null) where T : class, new()
        {
            return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
        }
    }
}