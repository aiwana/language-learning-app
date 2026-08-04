namespace WebShadowing.Configuration;

public static class EnvironmentFileConfiguration
{
    public static void AddDevelopmentEnvironmentFile(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment()) return;
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(builder.Environment.ContentRootPath, ".env"),
            Path.Combine(Directory.GetParent(builder.Environment.ContentRootPath)?.FullName ?? string.Empty, ".env")
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null) return;

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var separator = line.IndexOf('=');
            if (separator <= 0) continue;
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"', '\'');
            if (key.Length > 0 && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key))) values[key] = value;
        }
        builder.Configuration.AddInMemoryCollection(values);
    }
}
