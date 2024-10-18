using System;
using System.Net.Sockets;
using System.Text;

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
            // Clear stream before taking new input
            stream.Flush();

            // Step 1: Request username
            SendMessage(stream, "Enter username: ");
            string username = ReceiveMessage(stream).Trim();  // Trim to remove any newline or space characters

            // Step 2: Request password
            SendMessage(stream, "Enter password: ");
            string password = ReceiveMessage(stream).Trim();

            // Step 3: Authenticate user
            if (_userService.Authenticate(username, password))
            {
                SendMessage(stream, "Sign in successful!\n");
                authenticated = true;
            }
            else
            {
                SendMessage(stream, "Invalid credentials. Try again.\n");
            }
        }

        // Continue handling post-authenticated actions
        HandleAuthenticatedClient(stream);
    }

    private void HandleAuthenticatedClient(NetworkStream stream)
    {
        SendMessage(stream, "Welcome to the server!");

        while (true)
        {
            string message = ReceiveMessage(stream).Trim();
            if (message.ToLower() == "exit") break;
            SendMessage(stream, "Echo: " + message);
        }

        _client.Close();
        Console.WriteLine("Client disconnected.");
    }

    private void SendMessage(NetworkStream stream, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    private string ReceiveMessage(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim(); // Trim to remove unwanted newlines
    }
}
