using System;
using System.Net.Sockets;
using System.Text;

public class ClientHandler
{
    private Socket _clientSocket;
    private UserService _userService;

    public ClientHandler(Socket clientSocket, UserService userService)
    {
        _clientSocket = clientSocket;
        _userService = userService;
    }

    public void HandleClient()
    {
        bool authenticated = false;

        while (!authenticated)
        {
            try
            {
                SendMessage("Enter username: ");
                string username = ReceiveMessage();

                // Step 2: Request password
                SendMessage("Enter password: ");
                string password = ReceiveMessage();

                // Step 3: Authenticate user
                if (_userService.Authenticate(username, password))
                {
                    SendMessage("Sign in successful!\n");
                    authenticated = true;
                    HandleAuthenticatedClient();
                }
                else
                {
                    SendMessage("Invalid credentials. Try again.\n");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket exception: " + ex.Message);
                break; // Exit loop and close connection if an error occurs
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                break;
            }
        }

        // Close the socket after authentication loop ends
        CloseSocket();
    }

    private void HandleAuthenticatedClient()
    {
        SendMessage("Welcome to the server!");

        while (true)
        {
            try
            {
                string message = ReceiveMessage();

                // If the client wants to exit, break the loop
                if (message == null || message.ToLower() == "exit")
                {
                    break;
                }

                SendMessage("Echo: " + message);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket exception during message handling: " + ex.Message);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during message handling: " + ex.Message);
                break;
            }
        }

        // Close the socket after the communication ends
        CloseSocket();
        Console.WriteLine("Client disconnected.");
    }

    private void SendMessage(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            _clientSocket.Send(data); // Using Send() method for sending data via socket
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Socket exception while sending message: " + ex.Message);
            throw; // Rethrow to exit the communication loop
        }
    }

    private string ReceiveMessage()
    {
        try
        {
            byte[] buffer = new byte[1024];
            int bytesRead = _clientSocket.Receive(buffer); // Using Receive() method for receiving data via socket

            // If the client has disconnected, return null
            if (bytesRead == 0)
            {
                Console.WriteLine("Client disconnected.");
                return null; // Returning null will trigger the disconnection handling
            }

            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Socket exception while receiving message: " + ex.Message);
            throw; // Rethrow to exit the communication loop
        }
    }

    private void CloseSocket()
    {
        try
        {
            // Properly shut down the socket and close it
            if (_clientSocket != null && _clientSocket.Connected)
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Close();
                Console.WriteLine("Socket closed.");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Exception while closing the socket: " + ex.Message);
        }
    }
}
