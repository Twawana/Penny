namespace Penny.Agent;

public static class PortableConfigPaths
{
    public static string ResolveLogDirectory(string configuredValue)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return configuredValue.Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetAgentStateDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Penny");
    }

    public static string GetCertificatePath() => Path.Combine(GetAgentStateDirectory(), "agent-cert.pfx");

    public static string GetDeviceIdentityPath() => Path.Combine(GetAgentStateDirectory(), "device.json");

    /// <summary>
    /// Finds the repo/config root whether launched via <c>dotnet run</c>, IDE, or published output.
    /// </summary>
    public static string FindProjectRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Penny.sln")) ||
                Directory.Exists(Path.Combine(current.FullName, "config")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
