using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Server.Handler;
using Server.Server;
using Server.Models;



public class Communicator
{


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
            Thread thread = new Thread(() => clientFlow(clientSocket));
            thread.Start();
        }
    }


    public static void clientFlow(Socket clientSocket)
    {
        ClientHandler clientHandler = new LoginHandler(clientSocket);
        while (true)
        {
            try
            {
                while (true)
                {
                    Message message = Helper.ReceiveMessage(clientSocket);
                    if (clientHandler.ValidateRequest(message.Code))
                    {
                        clientHandler.HandleClient(message);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                CloseSocket(clientSocket);
                break;
            }

        }
    }



    private static void CloseSocket(Socket socket)
    {
        try
        {
                // Properly shut down the socket and close it
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    Console.WriteLine("Socket closed.");
                }
        
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Exception while closing the socket: " + ex.Message);
        }
    }
}