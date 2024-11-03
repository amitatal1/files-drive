using MongoDB.Bson.IO;
using MongoDB.Bson;
using Server.Models;
using System;
using System.Net.Sockets;
using System.Text.Json;


namespace Server.Handler
{
    public class LoginHandler : ClientHandler
    {
        public LoginHandler(Socket clientSocket) : base(clientSocket)
        {
        }

        public override bool ValidateRequest(char code)
        {
            return (code == (char)MessageCode.Login || code == (char)MessageCode.SignUp);
        }

        public override RequestResult HandleClient(Message msg)
        {

            try
            {
                User user = JsonSerializer.Deserialize<User>((msg.Data.ToString()));
                if (msg.Code == (char)MessageCode.Login)
                {
                    return HandleLogin(user);
                }
                else if (msg.Code == (char)MessageCode.SignUp)
                {
                    return HandleSignup(user);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new RequestResult
                {
                    Response = GetErrorMessage("Try again please!"),
                    NewHandler = null
                };
            }


        }

        private RequestResult HandleLogin(User user)
        {

        }

        private RequestResult HandleSignup(User user)
        {
            
        }


        

    }


    struct 
}
