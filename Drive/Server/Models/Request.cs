using MongoDB.Bson.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Models
{
    class Request
    {
        public int RequestCode { get; set; }
        public JsonDocument RequestData { get; set; } // Using System.Text.Json.JsonDocument
    }
}
