using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server.Models
{
    public class Message
    {
        public char Code { get; set; }
        public byte[] Data { get; set; }

        public Message() { }
        public Message(char code, string data)
        {
            this.Code = code;
            this.Data = Encoding.UTF8.GetBytes(data);
        }

        // Serialize message into the required format (1 byte for code, 4 bytes for data length, and rest for the data)
        public byte[] Serialize()
        {
            // 1 byte for message code
            byte[] codeBytes = new byte[] { (byte)Code };

            // 4 bytes for data length (in big-endian format)
            byte[] dataLengthBytes = BitConverter.GetBytes(Data.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(dataLengthBytes); // Make sure it's in big-endian
            }

            // Combine all into one byte array
            byte[] messageBytes = new byte[Constants.CodeLen + Constants.DataLengthFieldLen + Data.Length];
            Buffer.BlockCopy(codeBytes, 0, messageBytes, 0, Constants.CodeLen);
            Buffer.BlockCopy(dataLengthBytes, 0, messageBytes, Constants.CodeLen, Constants.DataLengthFieldLen);
            Buffer.BlockCopy(Data, 0, messageBytes, Constants.MessageHeaderLen, Data.Length);

            return messageBytes;
        }


    }
}
