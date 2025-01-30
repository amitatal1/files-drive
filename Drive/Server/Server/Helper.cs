using Server.Handler;
using Server.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server.Server
{
    public class Helper
    {
        public static void SendMessage(Socket socket, Message message)
        {
            byte[] data = message.Serialize();
            int totalBytesSent = 0;

            while (totalBytesSent < data.Length)
            {
                int bytesSent = socket.Send(data, totalBytesSent, data.Length - totalBytesSent, SocketFlags.None);
                if (bytesSent == 0) break;
                totalBytesSent += bytesSent;
            }
        }

        public static Message ReceiveMessage(Socket socket)
        {
            byte[] header = new byte[Constants.MessageHeaderLen];
            int totalBytesReceived = 0;

            while (totalBytesReceived < Constants.MessageHeaderLen)
            {
                int bytesReceived = socket.Receive(header, totalBytesReceived, Constants.MessageHeaderLen - totalBytesReceived, SocketFlags.None);
                if (bytesReceived == 0) throw new SocketException();
                totalBytesReceived += bytesReceived;
            }

            if (!BitConverter.IsLittleEndian) Array.Reverse(header);
            int len = BitConverter.ToInt32(header, 0);

            byte[] data = new byte[len];
            totalBytesReceived = 0;

            while (totalBytesReceived < len)
            {
                int bytesReceived = socket.Receive(data, totalBytesReceived, len - totalBytesReceived, SocketFlags.None);
                if (bytesReceived == 0) throw new SocketException();
                totalBytesReceived += bytesReceived;
            }

            return new Message { Data = data };
        }

        public static RequestResult CreateRequestResult<T>(ResponseCondition code, T dataStruct, ClientHandler newHandler)
        {
            return new RequestResult
            {
                Response = SerializePacket(code, dataStruct),
                NewHandler = newHandler
            };
        }

        private static byte[] SerializePacket<T>(ResponseCondition code, T dataStruct)
        {
            string jsonString = JsonSerializer.Serialize(dataStruct);
            byte codeByte = (byte)code;
            byte[] stringBytes = Encoding.UTF8.GetBytes(jsonString);
            byte[] lenBytes = BitConverter.GetBytes(stringBytes.Length);

            if (!BitConverter.IsLittleEndian) Array.Reverse(lenBytes);

            byte[] serializedPacket = new byte[1 + lenBytes.Length + stringBytes.Length];
            serializedPacket[0] = codeByte;

            Array.Copy(lenBytes, 0, serializedPacket, 1, lenBytes.Length);
            Array.Copy(stringBytes, 0, serializedPacket, 1 + lenBytes.Length, stringBytes.Length);

            return serializedPacket;
        }


    }
}
