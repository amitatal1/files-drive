using Server.Handler;
using System.Text.Json;
using System.Text;

namespace Server.Models
{
    public class RequestResult
    {
        public byte[] Response { get; set; }
        public ClientHandler NewHandler { get; set; }

    }
}
