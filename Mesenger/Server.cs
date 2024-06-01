using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Server
{
    private IPEndPoint ipEndPoint;
    static List<Users> allUsers = new List<Users>();

    public static async Task Main(string[] args)
    {
        Server serverInstance = new Server();
        Config config = ReadConfigFromFile("config.json");
        if (config != null)
        {
            Socket server = serverInstance.ServerConfig(IPAddress.Parse(config.Ip), config.Port);
            Console.Title = config.ServerName;
            server.Listen(config.Slots);
            Console.WriteLine(config.ServerName + " Started successfully");
            while (true)
            {
                Socket client = await server.AcceptAsync();
                _ = Task.Run(async () => await serverInstance.HandleClient(client, config));
            }
        }
        else
        {
            Console.WriteLine("Failed to read configuration from config.json. Exiting...");
        }
    }

    private static Config ReadConfigFromFile(string filePath)
    {
        try
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Config>(jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration from {filePath}: {ex.Message}");
            return null;
        }
    }

    private Socket ServerConfig(IPAddress ip, int port)
    {
        ipEndPoint = new IPEndPoint(ip, port);
        Socket server = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(ipEndPoint);
        return server;
    }

    private async Task HandleClient(Socket client, Config config)
    {
        var buffer = new byte[1024];
        var ClientNickname = await client.ReceiveAsync(buffer, SocketFlags.None);
        var nickname = Encoding.UTF8.GetString(buffer, 0, ClientNickname);

        if (!string.IsNullOrEmpty(config.PasswordForUser))
        {
            string response = "Password";
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await client.SendAsync(responseBytes, SocketFlags.None);
            var received = await client.ReceiveAsync(buffer, SocketFlags.None);
            string password = Encoding.UTF8.GetString(buffer, 0, received);

            if (password != config.PasswordForUser)
            {
                Console.WriteLine($"Incorrect password from user {nickname}. Connection closed.");
                client.Close();
                return;
            }
        }

        Console.WriteLine($"New user - {nickname}");
        Users user = new Users(nickname, ((IPEndPoint)client.RemoteEndPoint).Address, client);
        allUsers.Add(user);

        while (true)
        {
            try
            {
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                if (received == 0) break;

                var clientMessage = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"Message from {nickname}: {clientMessage}");

                string serverMessage = $"{nickname}: {clientMessage}";
                byte[] serverMessageBytes = Encoding.UTF8.GetBytes(serverMessage);

                foreach (var item in allUsers)
                {
                    if (item.ClientSocket != client)
                    {
                        await item.ClientSocket.SendAsync(serverMessageBytes, SocketFlags.None);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Connection lost with {nickname}: {ex.Message}");
                break;
            }
        }

        client.Close();
        allUsers.Remove(user);
        Console.WriteLine($"User {nickname} disconnected.");
    }
}

class Config
{
    public string Ip { get; set; }
    public int Port { get; set; }
    public string? PasswordForUser { get; set; }
    public string ServerName { get; set; }
    public int Slots { get; set; }
}

class Users
{
    private static int counter = 0;
    public int Id { get; private set; }
    public string Nickname { get; private set; }
    public IPAddress Ip { get; private set; }
    public Socket ClientSocket { get; private set; }

    public Users(string nickname, IPAddress ip, Socket clientSocket)
    {
        this.Id = ++counter;
        this.Nickname = nickname;
        this.Ip = ip;
        this.ClientSocket = clientSocket;
    }
}
