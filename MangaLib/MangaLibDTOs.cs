using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaLib
{
    public static class JsonCfg
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static double ParseDoubleOrZero(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;

            if (double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                return v;

            return 0;
        }
        public sealed class StringOrNumberToStringConverter : JsonConverter<string?>
        {
            public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.String => reader.GetString(),
                    JsonTokenType.Number => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                    JsonTokenType.Null => null,
                    _ => JsonDocument.ParseValue(ref reader).RootElement.ToString()
                };
            }

            public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
                => writer.WriteStringValue(value);
        }

        public sealed class FlexibleIntConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var n))
                    return n;

                if (reader.TokenType == JsonTokenType.String &&
                    int.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out n))
                    return n;

                return 0;
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value);
        }
    }
    public static class MangaParsers
    {
        public static MangaTitle? ParseTitle(string json) =>
            JsonSerializer.Deserialize<ApiEnvelope<MangaTitle>>(json, JsonCfg.Options)?.Data;

        public static async Task<MangaTitle?> ParseTitleAsync(Stream stream) =>
            (await JsonSerializer.DeserializeAsync<ApiEnvelope<MangaTitle>>(stream, JsonCfg.Options))?.Data;

        public static MangaTitle[] ParseProfile(string json) =>
            JsonSerializer.Deserialize<ApiEnvelope<List<MangaTitle>>>(json, JsonCfg.Options)?.Data?.ToArray()
            ?? Array.Empty<MangaTitle>();
    }
    public sealed class ApiEnvelope<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    public sealed class MangaTitle
    {
        public string? Name { get; set; }
        public string? Rus_Name { get; set; }
        public string? Slug_Url { get; set; }

        public MangaViews? Views { get; set; }
        public MangaRating? Rating { get; set; }
        public MangaItemsCount? Items_Count { get; set; }
        public MangaUser? User { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }
    public sealed class MangaViews
    {
        public int Total { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    public sealed class MangaRating
    {
        public string? Average { get; set; }
        public int Votes { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    public sealed class MangaItemsCount
    {
        public int Uploaded { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    public sealed class MangaUser
    {
        public int Id { get; set; }
        public string? Username { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }
    public sealed class Me
    {
        public ulong Id { get; set; }
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    public sealed class User
    {
        [JsonPropertyName("points_info")]
        public PointsInfo? LevelInfo { get; set; }
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

    }

    public sealed class PointsInfo
    {
        [JsonPropertyName("total_points")]
        public int Exp { get; set; }
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

    }

    public sealed class Chapter
    {
        public int Id { get; set; }
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

    }
}
