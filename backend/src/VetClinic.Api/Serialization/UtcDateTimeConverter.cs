using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VetClinic.Api.Serialization;

/// <summary>
/// Normaliza a UTC toda fecha recibida por la API.
/// Sin esto, System.Text.Json produce <see cref="DateTimeKind.Unspecified"/> y Npgsql
/// rechaza la escritura en columnas <c>timestamp with time zone</c> con un
/// <see cref="ArgumentException"/>, que el middleware terminaba reportando como conflicto.
/// Así se aceptan por igual "2027-10-18", "2027-10-18T08:30" y "2027-10-18T08:30Z".
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string DateOnlyFormat = "yyyy-MM-dd";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadValue(ref reader);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToUtc(value));

    internal static DateTime ReadValue(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        var raw = reader.GetString() ?? string.Empty;

        // "2027-10-18": System.Text.Json no lo acepta en GetDateTime, se interpreta
        // como medianoche UTC del día indicado.
        if (raw.Length == DateOnlyFormat.Length && DateTime.TryParseExact(raw, DateOnlyFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);

        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            throw new JsonException($"Fecha inválida: '{raw}'. Se espera ISO 8601 (por ejemplo 2027-10-18 o 2027-10-18T08:30Z).");

        return ToUtc(parsed);
    }

    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}

public sealed class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : UtcDateTimeConverter.ReadValue(ref reader);

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(UtcDateTimeConverter.ToUtc(value.Value));
    }
}

public static class UtcDatesJson
{
    public static void AddTo(JsonSerializerOptions options)
    {
        options.Converters.Add(new UtcDateTimeConverter());
        options.Converters.Add(new NullableUtcDateTimeConverter());
    }
}