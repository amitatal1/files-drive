using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Program
{
    static TcpListener listener;
    static UserService userService;

    static void Main(string[] args)
    {
        userService = new UserService();
        listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Server started...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected...");

            ClientHandler clientHandler = new ClientHandler(client, userService);
            Thread clientThread = new Thread(clientHandler.HandleClient);
            clientThread.Start();
        }
    }
}
