using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ClientHandler
{
    private TcpClient _client;
    private UserService _userService;

    public ClientHandler(TcpClient client, UserService userService)
    {
        _client = client;
        _userService = userService;
    }

    public void HandleClient()
    {
        NetworkStream stream = _client.GetStream();
        bool authenticated = false;

        while (!authenticated)
        {
            // Request username
            SendMessage(stream, "Enter username: ");
            string username = ReceiveMessage(stream);

            // Request password
            SendMessage(stream, "Enter password: ");
            string password = ReceiveMessage(stream);

            // Authentication
            if (_userService.Authenticate(username, password))
            {
                SendMessage(stream, "Sign in successful!");
                authenticated = true;
            }
            else
            {
                SendMessage(stream, "Invalid credentials. Try again.");
            }
        }

        // Post-authentication logic
        SendMessage(stream, "Welcome to the server!");
        while (true)
        {
            string message = ReceiveMessage(stream);
            if (message.ToLower() == "exit") break;
            SendMessage(stream, "Echo: " + message);
        }

        _client.Close();
        Console.WriteLine("Client disconnected.");
    }

    private void SendMessage(NetworkStream stream, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
        stream.Write(data, 0, data.Length);
    }

    private string ReceiveMessage(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
    }
}
