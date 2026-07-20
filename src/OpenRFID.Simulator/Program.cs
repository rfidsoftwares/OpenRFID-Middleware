using OpenRFID.Simulator.Profiles;
using OpenRFID.Simulator.Servers;

Console.WriteLine("==================================================");
Console.WriteLine("📡 OpenRFID Hardware Simulator CLI v1.0.0");
Console.WriteLine("==================================================");

int port = 5084;
Console.WriteLine($"Starting TCP Tag Stream Server on port {port} (Static Inventory Mode)...");

await using var server = new TcpSocketSimulatorServer(port, new StaticInventoryProfile(uniqueTagCount: 20, intervalMs: 200));
server.Start();

Console.WriteLine("Server active. Press Ctrl+C or Enter to exit.");
Console.ReadLine();

await server.StopAsync();
Console.WriteLine("Simulator stopped.");
