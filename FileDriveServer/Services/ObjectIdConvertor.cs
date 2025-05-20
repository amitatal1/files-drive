using MongoDB.Bson;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace FileDriveServer.Services
{
    public class ObjectIdConverter : JsonConverter<ObjectId>
    {
        // This method is called when deserializing JSON to an ObjectId
        public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Expect the JSON value to be a string
            if (reader.TokenType != JsonTokenType.String)
            {
                // If it's not a string, throw an error (or handle other formats if necessary)
                throw new JsonException($"Expected string but got {reader.TokenType} when deserializing ObjectId.");
            }

            // Get the string value from the JSON reader
            string? objectIdString = reader.GetString();

            // Validate and convert the string to an ObjectId
            if (string.IsNullOrEmpty(objectIdString) || !ObjectId.TryParse(objectIdString, out ObjectId objectId))
            {
                throw new JsonException($"Unable to parse '{objectIdString}' as ObjectId.");
            }

            return objectId; // Return the parsed ObjectId
        }

        // This method is called when serializing an ObjectId to JSON
        public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
        {
            // Write the string representation of the ObjectId
            writer.WriteStringValue(value.ToString());
        }
    }
}
