namespace FileUpload.Configuration;

public static class EnvironmentFileConfigurationExtensions
{
    public static void AddProjectEnvironmentFile(this ConfigurationManager configuration, string contentRootPath)
    {
        var environmentFile = Path.Combine(contentRootPath, ".env");
        if (!File.Exists(environmentFile))
            return;

        var values = File.ReadLines(environmentFile)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new KeyValuePair<string, string?>(parts[0].Trim(), parts[1].Trim()));

        configuration.AddInMemoryCollection(values);
    }
}
