using MongoDB.Bson.Serialization.Conventions;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Server
{
    public class Helper
    {

        public static void SendMessage(Socket socket, Message message)
        {
            socket.Send(message.Serialize()); // Using Send() method for sending data via socket
        }

        public static Message ReceiveMessage(Socket socket)
        {
            Message receivedMessage = new Message();

            byte[] header = new byte[Constants.MessageHeaderLen];
            socket.Receive(header);

     

            // Convert the four bytes to an integer (ensure correct endianness)
            int len = 
                .ToInt32(header, 1);

            byte[] data = new byte[len];
            socket.Receive(data);

            receivedMessage.Data = data;

            return receivedMessage;

        }


    }
}
