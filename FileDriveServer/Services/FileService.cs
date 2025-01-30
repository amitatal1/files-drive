using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Server.Models;
using System.Collections.Generic;
using System.IO;

namespace Server.Services
{
    public class FileService
    {
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "FilesDriveStorage");
        private readonly IMongoCollection<FileRecord> _files;

        public FileService(IMongoDatabase database)
        {
            // Ensure storage directory exists
            Directory.CreateDirectory(_storagePath);
            _files = database.GetCollection<FileRecord>("Files");
        }

        public bool SaveFile(IFormFile file, string owner)
        {
            var filePath = Path.Combine(_storagePath, file.FileName);

            // Save the file to the local directory
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            // Create a new FileRecord and store metadata in MongoDB
            var fileRecord = new FileRecord
            {
                FileName = file.FileName,
                Owner = owner,
                SavePath = filePath,
                ViewPermissions = new List<string> { owner }, // Default view permission for the owner
                EditPermissions = new List<string> { owner }  // Default edit permission for the owner
            };

            _files.InsertOne(fileRecord); // Save metadata in MongoDB
            return true;
        }

        public byte[] GetFileContent(string filename)
        {
            // Retrieve the file metadata from MongoDB
            var fileRecord = _files.Find(f => f.FileName == filename).FirstOrDefault();
            if (fileRecord != null)
            {
                // Read the file data from local storage
                return File.ReadAllBytes(fileRecord.SavePath);
            }
            return null;
        }

        public bool ShareFile(string filename, string shareType, string sharedUser)
        {
            // Find the file in the database
            var fileRecord = _files.Find(f => f.FileName == filename).FirstOrDefault();
            if (fileRecord != null)
            {
                // Add the shared user to the appropriate permissions list
                if (shareType == "view" && !fileRecord.ViewPermissions.Contains(sharedUser))
                {
                    fileRecord.ViewPermissions.Add(sharedUser);
                }
                else if (shareType == "edit" && !fileRecord.EditPermissions.Contains(sharedUser))
                {
                    fileRecord.EditPermissions.Add(sharedUser);
                }
                else
                {
                    return false; // User already has permission or invalid shareType
                }

                // Update the file record in MongoDB
                _files.ReplaceOne(f => f.FileName == filename, fileRecord);
                return true;
            }
            return false;
        }
    }
}
