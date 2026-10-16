namespace Nexora.Infrastructure.Services.Common;

public static class DotEnvConfiguration
{
    public static IReadOnlyDictionary<string, string?> ReadFromAncestors(string startDirectory)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(startDirectory)); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, ".env");
            if (!File.Exists(path))
            {
                continue;
            }

            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    throw new FormatException($"Invalid .env entry in {path}.");
                }

                var key = line[..separator].Trim();
                if (!IsValidKey(key))
                {
                    throw new FormatException($"Invalid .env key in {path}.");
                }

                var value = line[(separator + 1)..].Trim();
                if (value.Length >= 2 && value[0] is '\'' or '"')
                {
                    if (value[^1] != value[0])
                    {
                        throw new FormatException($"Unclosed .env value for {key} in {path}.");
                    }

                    value = value[1..^1];
                }
                else
                {
                    var comment = value.IndexOf(" #", StringComparison.Ordinal);
                    if (comment >= 0)
                    {
                        value = value[..comment].TrimEnd();
                    }
                }

                values[key.Replace("__", ":", StringComparison.Ordinal)] = value;
            }

            return values;
        }

        return new Dictionary<string, string?>();
    }

    private static bool IsValidKey(string key) =>
        key.Length > 0 && (char.IsLetter(key[0]) || key[0] == '_') &&
        key.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');
}
