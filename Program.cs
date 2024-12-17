using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class MySqlProxyGateway
{
    private readonly int _proxyPort;    // Proxy- (Docker)
    private readonly string _mysqlHost; // MySQL  (127.0.0.1)
    private readonly int _mysqlPort;    // MySQL  (3306)

    public MySqlProxyGateway(string mysqlHost, int mysqlPort, int proxyPort)
    {
        _mysqlHost = mysqlHost;
        _mysqlPort = mysqlPort;
        _proxyPort = proxyPort;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _proxyPort); // Accept connections from any network interface
        listener.Start();
        Console.WriteLine($"MySQL Proxy Gateway is running on port {_proxyPort}...");

        while (true)
        {
            var clientSocket = await listener.AcceptTcpClientAsync();
            Console.WriteLine("New connection received.");
            _ = HandleClientAsync(clientSocket); // Handle client connection asynchronously
        }
    }

    private async Task HandleClientAsync(TcpClient clientSocket)
    {
        using (clientSocket)
        {
            try
            {
                // Connect to MySQL server locally
                var mysqlSocket = new TcpClient();
                await mysqlSocket.ConnectAsync(_mysqlHost, _mysqlPort);
                Console.WriteLine($"Connected to MySQL server at {_mysqlHost}:{_mysqlPort}");

                // Forward data between Docker client and MySQL server
                var clientToServer = ForwardDataAsync(clientSocket.GetStream(), mysqlSocket.GetStream());
                var serverToClient = ForwardDataAsync(mysqlSocket.GetStream(), clientSocket.GetStream());

                await Task.WhenAll(clientToServer, serverToClient); // Wait for both directions to complete
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }
    }

   private async Task ForwardDataAsync(NetworkStream input, NetworkStream output)
{
    var buffer = new byte[8192];
    int bytesRead;

    while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        Console.WriteLine($"Forwarding {bytesRead} bytes...");
        await output.WriteAsync(buffer, 0, bytesRead);
        await output.FlushAsync();
    }

    Console.WriteLine("Stream closed.");
}

}

class Program
{
    static async Task Main(string[] args)
    {
      
        string mysqlHost = "127.0.0.1"; // Localhost (MySQL server is bound here)
        int mysqlPort = 3306;          // MySQL port
        int proxyPort = 4000;          // Proxy Gateway port (Docker will connect here)

        var proxy = new MySqlProxyGateway(mysqlHost, mysqlPort, proxyPort);
        await proxy.StartAsync();
    }
}
