using System.Text.Json;

namespace Penny.Agent;

public sealed class AgentSettings
{
    public NetworkSettings Network { get; init; } = new();
    public ScreenSettings Screen { get; init; } = new();
    public LoggingSettings Logging { get; init; } = new();

    public static AgentSettings Load()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "config", "agent.settings.json"),
            Path.Combine(AppContext.BaseDirectory, "config", "agent.settings.example.json"),
            Path.Combine(PortableConfigPaths.FindProjectRoot(), "config", "agent.settings.json"),
            Path.Combine(PortableConfigPaths.FindProjectRoot(), "config", "agent.settings.example.json")
        };

        var pathToRead = candidates.FirstOrDefault(File.Exists);
        if (pathToRead is null)
        {
            throw new FileNotFoundException("Missing agent settings file. Expected config/agent.settings.json or config/agent.settings.example.json.");
        }

        var json = File.ReadAllText(pathToRead);
        var parsed = JsonSerializer.Deserialize<AgentSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? throw new InvalidOperationException("Unable to deserialize agent settings.");
    }
}

public sealed class NetworkSettings
{
    public int Port { get; init; } = 5000;
}

public sealed class ScreenSettings
{
    public int Fps { get; init; } = 15;
    public int Quality { get; init; } = 70;
    public string Codec { get; init; } = "jpeg";
}

public sealed class LoggingSettings
{
    public string MinimumLevel { get; init; } = "Information";
    public string Directory { get; init; } = "%LOCALAPPDATA%/Penny/logs";
}
