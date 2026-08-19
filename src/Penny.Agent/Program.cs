using Penny.Agent;
using Penny.Network;
using Penny.Security;

var settings = AgentSettings.Load();

Directory.CreateDirectory(PortableConfigPaths.GetAgentStateDirectory());
Directory.CreateDirectory(PortableConfigPaths.ResolveLogDirectory(settings.Logging.Directory));

var certificate = AgentCertificateProvider.GetOrCreate(PortableConfigPaths.GetCertificatePath());
var listener = new PennyAgentListener(settings.Network.Port, certificate);

Console.WriteLine("Penny Agent is ready.");
Console.WriteLine($"Listening on port {settings.Network.Port}");
Console.WriteLine("Any controller that reaches this machine gets immediate control.");
Console.WriteLine();
Console.WriteLine("Connect from the first PC:");
Console.WriteLine($"  dotnet run --project src/Penny.Controller -- <agent-ip> {settings.Network.Port}");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop the agent.");

listener.OnConnectionAsync = async (transport, cancellationToken) =>
{
    await using var scopedTransport = transport;
    Console.WriteLine($"Controller connected from {scopedTransport.RemoteEndpoint}.");

    try
    {
        await PennySessionLoop.RunAgentAsync(scopedTransport, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Controller disconnected ({scopedTransport.RemoteEndpoint}).");
    }
    catch (OperationCanceledException)
    {
        // Listener shutting down.
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Session error: {ex.Message}");
    }
};

listener.OnConnectionSetupFailed = (remote, ex) =>
{
    Console.WriteLine($"Connection setup failed from {remote}: {ex.Message}");
};

listener.Start();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Graceful shutdown.
}

await listener.DisposeAsync().ConfigureAwait(false);
