using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Services;


namespace Server.Handler
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

        public abstract bool ValidateRequest(char code);
        public abstract RequestResult HandleClient(Message msg);

 
    }
}
