using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

public class FileRecord
{
    [BsonId]
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

    [BsonElement("lastUpdateTime")]
    public DateTime LastUpdateTime { get; set; }
}
