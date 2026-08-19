namespace Api.IntegrationTests;

/// <summary>
/// Minimal manual cookie jar, deliberately not using HttpClientHandler's built-in CookieContainer -
/// the reuse-detection test needs to hold onto a stale cookie value after the client has already
/// moved on to a newer one, which a real cookie container won't let you do.
/// </summary>
internal sealed class CookieJar
{
    private readonly Dictionary<string, string> _values = new();

    public void CaptureFrom(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            return;

        foreach (var header in setCookieHeaders)
        {
            var namePart = header.Split(';', 2)[0];
            var separatorIndex = namePart.IndexOf('=');
            if (separatorIndex < 0)
                continue;

            _values[namePart[..separatorIndex]] = namePart[(separatorIndex + 1)..];
        }
    }

    public string? Get(string name) => _values.GetValueOrDefault(name);

    public void Set(string name, string value) => _values[name] = value;

    public CookieJar Clone()
    {
        var clone = new CookieJar();
        foreach (var (key, value) in _values)
            clone._values[key] = value;

        return clone;
    }

    public string ToHeader() => string.Join("; ", _values.Select(kv => $"{kv.Key}={kv.Value}"));
}
