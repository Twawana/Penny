using Penny.Controller;

static string Prompt(string label, string defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine();
    return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
}

string host;
int port;

if (args.Length >= 2)
{
    host = args[0];
    if (!int.TryParse(args[1], out port))
    {
        Console.WriteLine("Invalid port.");
        return;
    }
}
else
{
    Console.WriteLine("Penny Controller — connect directly to the agent PC.");
    Console.WriteLine();

    host = Prompt("Agent IP or hostname", "127.0.0.1");
    var portInput = Prompt("Port", "5000");
    if (!int.TryParse(portInput, out port))
    {
        Console.WriteLine("Invalid port.");
        return;
    }
}

var client = new PennyControllerClient();
try
{
    Console.WriteLine($"Connecting to {host}:{port}...");
    await using var session = await client.ConnectAsync(host, port, CancellationToken.None).ConfigureAwait(false);

    Console.WriteLine($"Connected. You have control (session {session.SessionId}).");
    Console.WriteLine("Sending ping...");
    await session.SendPingAsync(CancellationToken.None).ConfigureAwait(false);
    Console.WriteLine("Link is live.");

    await session.EndSessionAsync("UserDisconnected", CancellationToken.None).ConfigureAwait(false);
    Console.WriteLine("Disconnected.");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}
