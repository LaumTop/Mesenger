using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Client
    {
        public static async Task Main(string[] args)
        {
            IPAddress ip = null;
            int port = 0;
            bool dataCorrect = false;
            string? nickname = null;
            string? ipAdres = null;

            Console.Write("Write your nickname: ");
            nickname = Console.ReadLine();
            if (string.IsNullOrEmpty(nickname))
            {
                nickname = "Anonim";
            }

            Console.Write("Connect to IP address: ");
            ipAdres = Console.ReadLine();
            if (string.IsNullOrEmpty(ipAdres))
            {
                Console.WriteLine("IP address can't be empty or null");
                return;
            }

            Console.Write("Write server port: ");
            string? ports = Console.ReadLine();
            if (string.IsNullOrEmpty(ports))
            {
                Console.WriteLine("Port can't be empty or null");
                return;
            }

            try
            {
                port = int.Parse(ports);
                ip = IPAddress.Parse(ipAdres);
                dataCorrect = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Entered data is wrong\nError message: " + ex.Message);
            }

            if (dataCorrect)
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(ip, port);
                    using Socket client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    await client.ConnectAsync(ipEndPoint);
                    string message = nickname;
                    byte[] messageSend = Encoding.UTF8.GetBytes(message);
                    await client.SendAsync(messageSend, SocketFlags.None);

                    var buffer = new byte[1024];

                    // Start a task to continuously read messages from the server
                    _ = Task.Run(async () =>
                    {
                        while (true)
                        {
                            var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                            if (received == 0) break;

                            var response = Encoding.UTF8.GetString(buffer, 0, received);
                            Console.WriteLine(response);
                        }
                    });

                    // Main loop to send messages to the server
                    while (true)
                    {
                        string clientMessage = Console.ReadLine();
                        if (string.IsNullOrEmpty(clientMessage)) continue;

                        byte[] clientMessageBytes = Encoding.UTF8.GetBytes(clientMessage);
                        await client.SendAsync(clientMessageBytes, SocketFlags.None);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
    }
}
