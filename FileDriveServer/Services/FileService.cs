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


        public FileRecord GetFileRecord(string filename)
        {
             return _files.Find(x => x.FileName == filename).FirstOrDefault();
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

        public async Task<bool> ShareFileAsync(string filename, string shareType, List<string> sharedUsers)
        {
            var fileRecord = await _files.Find(f => f.FileName == filename).FirstOrDefaultAsync();
            if (fileRecord == null)
            {
                return false; // File not found
            }

            bool updated = false;

            foreach (var user in sharedUsers)
            {
                if (shareType == "view" && !fileRecord.ViewPermissions.Contains(user))
                {
                    fileRecord.ViewPermissions.Add(user);
                    updated = true;
                }
                else if (shareType == "edit" && !fileRecord.EditPermissions.Contains(user))
                {
                    fileRecord.EditPermissions.Add(user);
                    updated = true;
                }
            }

            if (updated)
            {
                await _files.ReplaceOneAsync(f => f.FileName == filename, fileRecord);
                return true;
            }

            return false; // No changes were made
        }


        public async Task<List<FileRecord>> GetUserFilesAsync(string username)
        {
            var filter = Builders<FileRecord>.Filter.Or(
                Builders<FileRecord>.Filter.Eq(f => f.Owner, username),
                Builders<FileRecord>.Filter.AnyEq(f => f.EditPermissions, username),
                Builders<FileRecord>.Filter.AnyEq(f => f.ViewPermissions, username)
            );

            return await _files.Find(filter).ToListAsync();
        }
    }
}
