using MongoDB.Bson.IO;
using MongoDB.Bson;
using Server.Models;
using System;
using System.Net.Sockets;
using System.Text.Json;
using Services;
using Server.Server;

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
                    NewHandler = null,
                };
            }


        }

        private RequestResult HandleLogin(User user)
        {
            UserService db = UserService.Instance;            
            if (db.Authenticate(user.Username, user.Password))
            {
                GenericResponse resp = new GenericResponse("Login Succeded");

                return Helper.CreateRequestResult<GenericResponse>(ResponseCondition.Success ,resp,null ); // TO ADD next handler
            }
            else
            {
                GenericResponse resp = new GenericResponse("Login Failed");

                return Helper.CreateRequestResult<GenericResponse>(ResponseCondition.Success, resp, null);

            }
        }

        private RequestResult HandleSignup(User user)
        {
            UserService db = UserService.Instance;
            if (db.Authenticate(user.Username, user.Password))
            {
                GenericResponse resp = new GenericResponse("Login Succeded");

                return new RequestResult<GenericResponse>(ResponseCondition.Success, resp, null); // TO ADD next handler
            }
            else
            {
                GenericResponse resp = new GenericResponse("Login Failed");

                return new RequestResult<GenericResponse>(ResponseCondition.Success, resp, null);

            }
        }

        

        

    }
}
