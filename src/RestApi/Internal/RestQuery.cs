using System.Text;

namespace RestApi.Internal;

/// <summary>
/// Tiny fluent builder for URL query strings. Skips <c>null</c> values so optional parameters
/// simply drop out, and URL-encodes keys and values. Used by the generated sub-clients.
/// </summary>
public sealed class RestQuery
{
    private readonly List<KeyValuePair<string, string>> _items = [];

    /// <summary>Start a new, empty query.</summary>
    public static RestQuery New() => new();

    /// <summary>Add a parameter unless <paramref name="value"/> is <c>null</c>.</summary>
    public RestQuery Add(string key, string? value)
    {
        if (value is not null) _items.Add(new(key, value));
        return this;
    }

    /// <summary>Add a parameter unless <paramref name="value"/> is <c>null</c> (invariant-formatted).</summary>
    public RestQuery Add(string key, int? value) =>
        Add(key, value?.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Add a parameter unless <paramref name="value"/> is <c>null</c> (invariant-formatted).</summary>
    public RestQuery Add(string key, long? value) =>
        Add(key, value?.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Add a parameter unless <paramref name="value"/> is <c>null</c> (invariant-formatted).</summary>
    public RestQuery Add(string key, double? value) =>
        Add(key, value?.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Add a boolean parameter (rendered <c>true</c>/<c>false</c>) unless <c>null</c>.</summary>
    public RestQuery Add(string key, bool? value) =>
        Add(key, value is null ? null : (value.Value ? "true" : "false"));

    /// <summary>Render the leading-<c>?</c> query string, or an empty string when no parameters were added.</summary>
    public override string ToString()
    {
        if (_items.Count == 0) return string.Empty;
        var sb = new StringBuilder("?");
        for (var i = 0; i < _items.Count; i++)
        {
            if (i > 0) sb.Append('&');
            sb.Append(Uri.EscapeDataString(_items[i].Key))
              .Append('=')
              .Append(Uri.EscapeDataString(_items[i].Value));
        }
        return sb.ToString();
    }
}
