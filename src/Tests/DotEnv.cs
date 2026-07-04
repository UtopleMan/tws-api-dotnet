using Microsoft.Extensions.Configuration;

namespace TwsApi.Tests;

/// <summary>
/// Minimal <c>.env</c> reader so the repo-root <c>.env</c> (the file you fill in from
/// <c>.env.example</c>) feeds the integration-test configuration - the same file
/// <c>docker compose</c> reads. This lets a plain <c>dotnet test</c> pick up the
/// TWS_USERID / TWS_PASSWORD you put in <c>.env</c> without also exporting them as
/// environment variables or setting user-secrets.
///
/// It is registered as the lowest-priority source, so real environment variables and
/// user-secrets still win when present.
/// </summary>
internal static class DotEnv
{
    /// <summary>
    /// Adds the nearest <c>.env</c> file (searching upward from the test output directory
    /// to the repo root) as an in-memory configuration source. A no-op when none is found.
    /// </summary>
    public static IConfigurationBuilder AddRepoDotEnv(this IConfigurationBuilder builder)
    {
        var path = FindDotEnv();
        if (path is null)
        {
            return builder;
        }

        var values = Parse(File.ReadAllLines(path));
        return builder.AddInMemoryCollection(values);
    }

    private static string? FindDotEnv()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<KeyValuePair<string, string?>> Parse(IEnumerable<string> lines)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue; // blank line or comment
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue; // not a KEY=VALUE assignment
            }

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            // Strip a single pair of surrounding quotes, if present.
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
            {
                value = value[1..^1];
            }

            yield return new KeyValuePair<string, string?>(key, value);
        }
    }
}
