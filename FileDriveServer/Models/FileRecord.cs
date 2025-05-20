using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

public class FileRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    [BsonElement("filename")]
    public string FileName { get; set; }

    [BsonElement("savePath")]
    public string SavePath { get; set; }

    [BsonElement("owner")]
    public string Owner { get; set; }

    [BsonElement("viewPermissions")]
    public List<string> ViewPermissions { get; set; }

    [BsonElement("editPermissions")]
    public List<string> EditPermissions { get; set; }

    [BsonElement("lastUpdated")]
    public DateTime? LastUpdateTime { get; set; }


    [BsonElement("encryptedFileKey")]
    public string EncryptedFileKey { get; set; } // Base64 encoded encrypted file key

    [BsonElement("iv")]
    public string IV { get; set; } // Base64 encoded Initialization Vector (IV)

    [BsonElement("authTag")]
    public string AuthTag { get; set; }
}
