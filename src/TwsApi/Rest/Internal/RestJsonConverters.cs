using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TwsApi.Rest.Internal;

/// <summary>
/// Shared helpers for the tolerant converters below. IBKR's Web API frequently encodes numbers,
/// ids, flags, and dates as strings, and sprinkles empty / "N/A" sentinels through otherwise
/// numeric fields. These converters let the typed models use real value types while treating
/// those sentinels (and JSON null) as <c>null</c> instead of throwing.
/// </summary>
internal static class RestJson
{
    private static readonly string[] NullLikeTokens = ["", "n/a", "na", "--", "-", "null"];

    /// <summary>True for the empty / placeholder strings IBKR uses in place of a missing value.</summary>
    public static bool IsNullLike(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        foreach (var token in NullLikeTokens)
        {
            if (trimmed.Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The invariant, digit-only spans IBKR uses for epoch timestamps (seconds or millis).</summary>
    public static DateTimeOffset FromUnix(long value)
    {
        // 13+ digits is milliseconds (e.g. 1782915547000); shorter is seconds.
        return value >= 100_000_000_000L
            ? DateTimeOffset.FromUnixTimeMilliseconds(value)
            : DateTimeOffset.FromUnixTimeSeconds(value);
    }

    /// <summary>
    /// Parse an IBKR epoch value that may arrive as an integer string (<c>"1782915547000"</c>) or,
    /// on some endpoints (e.g. <c>/fyi/notifications</c>), a float string (<c>"1783199205.0"</c>).
    /// Returns <c>false</c> for non-numeric text.
    /// </summary>
    public static bool TryParseUnix(string? text, out long value)
    {
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && seconds is >= long.MinValue and <= long.MaxValue)
        {
            value = (long)seconds;
            return true;
        }

        value = 0;
        return false;
    }
}

/// <summary>Reads a <see cref="long"/> from a JSON number or numeric string; sentinels/null become <c>null</c>.</summary>
internal sealed class NullableInt64Converter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt64();
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }

                throw new JsonException($"Cannot parse '{text}' as a 64-bit integer.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a 64-bit integer.");
        }
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is long number)
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>Reads an <see cref="int"/> from a JSON number or numeric string; sentinels/null become <c>null</c>.</summary>
internal sealed class NullableInt32Converter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt32();
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }

                throw new JsonException($"Cannot parse '{text}' as a 32-bit integer.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a 32-bit integer.");
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is int number)
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>Reads a <see cref="double"/> from a JSON number or numeric string; sentinels/null become <c>null</c>.</summary>
internal sealed class NullableDoubleConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetDouble();
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }

                throw new JsonException($"Cannot parse '{text}' as a double.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a double.");
        }
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value is double number)
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>Reads a <see cref="decimal"/> from a JSON number or numeric string; sentinels/null become <c>null</c>.</summary>
internal sealed class NullableDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetDecimal();
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }

                throw new JsonException($"Cannot parse '{text}' as a decimal.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a decimal.");
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is decimal number)
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>Reads a <see cref="bool"/> from JSON true/false, 0/1, or textual flags ("yes"/"no"/"true"/"false").</summary>
internal sealed class NullableBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                return reader.GetDecimal() != 0m;
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                return text!.Trim().ToLowerInvariant() switch
                {
                    "1" or "true" or "yes" or "y" => true,
                    "0" or "false" or "no" or "n" => false,
                    _ => throw new JsonException($"Cannot parse '{text}' as a boolean."),
                };
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a boolean.");
        }
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is bool flag)
        {
            writer.WriteBooleanValue(flag);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Reads an IBKR display timestamp ("yyyyMMdd-HH:mm:ss" and friends) or an epoch value into a
/// <see cref="DateTimeOffset"/>. IBKR display strings carry no offset, so they are treated as UTC.
/// </summary>
internal sealed class IbkrTimestampConverter : JsonConverter<DateTimeOffset?>
{
    private static readonly string[] Formats =
    [
        "yyyyMMdd-HH:mm:ss",
        "yyyyMMdd-HH:mm",
        "yyyyMMdd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyyMMdd",
    ];

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var epochNumber)
                    ? RestJson.FromUnix(epochNumber)
                    : RestJson.FromUnix((long)reader.GetDouble());
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                var trimmed = text!.Trim();

                // Purely numeric strings of 10+ digits are epoch seconds/milliseconds (integer or
                // float-formatted); an 8-digit yyyyMMdd falls through to the exact-format parse below.
                if (trimmed.Length >= 10 && RestJson.TryParseUnix(trimmed, out var epoch))
                {
                    return RestJson.FromUnix(epoch);
                }

                if (DateTimeOffset.TryParseExact(
                        trimmed,
                        Formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var parsed))
                {
                    return parsed;
                }

                throw new JsonException($"Cannot parse '{text}' as an IBKR timestamp.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a timestamp.");
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is DateTimeOffset moment)
        {
            writer.WriteStringValue(moment.UtcDateTime.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>Reads an epoch value (unix seconds or milliseconds, as a number or numeric string) into a <see cref="DateTimeOffset"/>.</summary>
internal sealed class IbkrEpochConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var epochNumber)
                    ? RestJson.FromUnix(epochNumber)
                    : RestJson.FromUnix((long)reader.GetDouble());
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                if (RestJson.TryParseUnix(text, out var epoch))
                {
                    return RestJson.FromUnix(epoch);
                }

                throw new JsonException($"Cannot parse '{text}' as an epoch timestamp.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading an epoch timestamp.");
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is DateTimeOffset moment)
        {
            writer.WriteNumberValue(moment.ToUnixTimeMilliseconds());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>Reads an IBKR "yyyyMMdd" (or "yyyy-MM-dd") calendar date into a <see cref="DateOnly"/>.</summary>
internal sealed class IbkrDateConverter : JsonConverter<DateOnly?>
{
    private static readonly string[] Formats = ["yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd"];

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return FromNumber(reader.GetInt64());
            case JsonTokenType.String:
                var text = reader.GetString();
                if (RestJson.IsNullLike(text))
                {
                    return null;
                }

                var trimmed = text!.Trim();
                if (DateOnly.TryParseExact(trimmed, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed;
                }

                // Some fields carry the date as a numeric string (yyyyMMdd or an epoch).
                if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
                {
                    return FromNumber(numeric);
                }

                throw new JsonException($"Cannot parse '{text}' as a date.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a date.");
        }
    }

    private static DateOnly? FromNumber(long value)
    {
        // A yyyyMMdd integer (e.g. 20261218) parses directly; larger values are epoch seconds/ms.
        var digits = value.ToString(CultureInfo.InvariantCulture);
        if (digits.Length == 8
            && DateOnly.TryParseExact(digits, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return DateOnly.FromDateTime(RestJson.FromUnix(value).UtcDateTime);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value is DateOnly date)
        {
            writer.WriteStringValue(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Non-nullable "yyyyMMdd"/"yyyy-MM-dd" date converter, registered globally so it also applies to
/// <see cref="DateOnly"/> elements of collections (e.g. the performance <c>dates</c> array). Fields
/// that need different handling opt in per-property with their own <see cref="JsonConverterAttribute"/>.
/// </summary>
internal sealed class IbkrDateOnlyElementConverter : JsonConverter<DateOnly>
{
    // "yyyyMM" appears in monthly-frequency performance series and resolves to the first of the month.
    private static readonly string[] Formats = ["yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMM"];

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (DateOnly.TryParseExact(text?.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new JsonException($"Cannot parse '{text}' as a date.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}

/// <summary>Reads an ISO-style "yyyy-MM-dd" (or "yyyyMMdd") date into a <see cref="DateOnly"/>, writing it back as ISO.</summary>
internal sealed class IbkrIsoDateConverter : JsonConverter<DateOnly?>
{
    private static readonly string[] Formats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd"];

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var text = reader.GetString();
        if (RestJson.IsNullLike(text))
        {
            return null;
        }

        if (DateOnly.TryParseExact(text!.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new JsonException($"Cannot parse '{text}' as an ISO date.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value is DateOnly date)
        {
            writer.WriteStringValue(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
