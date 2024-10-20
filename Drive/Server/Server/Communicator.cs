using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;



public class Communicator
{
    private static UserService _userService = UserService.Instance;

    public static void StartServer(string ipAddress, int port)
    {
        // Create a new socket for the server
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the IP address and port
        serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port));

        // Start listening for incoming connections
        serverSocket.Listen(10);
        Console.WriteLine($"Server started on {ipAddress}:{port}");

        while (true)
        {
            // Accept incoming connections
            Socket clientSocket = serverSocket.Accept();

            // Create a new thread to handle each client connection
            ClientHandler clientHandler = new ClientHandler(clientSocket, _userService);
            System.Threading.Thread clientThread = new System.Threading.Thread(clientHandler.HandleClient);
            clientThread.Start();
        }
    }

}

