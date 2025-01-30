using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;



public class User
{

    [BsonElement("username")]
    public string Username { get; set; }

    [BsonElement("password")]
    public string Password { get; set; }
}

public class GenericResponse
{
    public GenericResponse(string message)
    {
        Message = message;
    }

    public string Message;
}

