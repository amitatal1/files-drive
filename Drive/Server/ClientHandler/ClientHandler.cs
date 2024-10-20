using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Server.ClientHandler
{

    public abstract class ClientHandler
    {
        protected Socket _clientSocket;
        protected UserService _userService;
           
        public ClientHandler(Socket clientSocket)
        {
            _clientSocket = clientSocket;
            _userService = UserService.Instance;
        }

        public abstract bool ValidateRequest( int code);

    }
}
