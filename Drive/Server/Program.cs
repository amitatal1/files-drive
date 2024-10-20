using System;
using System.Net;
using System.Net.Sockets;

public class Program
{
    private const string IpAddress = "127.0.0.1";
    private const int Port = 5000;
    
    public static void Main(string[] args)
    {
        // Start the server
        Communicator.StartServer(IpAddress, Port);
    }
}
