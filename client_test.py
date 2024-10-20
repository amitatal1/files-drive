import socket

def login_client(server_ip, server_port):
    # Create a socket object
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    try:
        # Connect to the server
        client_socket.connect((server_ip, server_port))
        print(f"Connected to server at {server_ip}:{server_port}")
        response = receive_message(client_socket)

        # Enter the login loop
        while True:

                user_input = input()
                send_message(client_socket, user_input)

                response = receive_message(client_socket)
                print(response)


    except Exception as e:
        print(f"An error occurred: {e}")
    finally:
        client_socket.close()
        print("Connection closed.")

def send_message(sock, message):
    """Send a message to the server."""
    sock.sendall(message.encode('utf-8'))

def receive_message(sock):
    """Receive a message from the server."""
    message = sock.recv(1024).decode('utf-8')
    return message

if __name__ == "__main__":
    # Specify server IP and port
    SERVER_IP = '127.0.0.1'  # Localhost for testing
    SERVER_PORT = 5000       # Match with your server port

    login_client(SERVER_IP, SERVER_PORT)
