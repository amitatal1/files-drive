using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Server.Models; // Assuming your FileRecord is in Server.Models
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Cryptography;
using FileDriveServer.Services;
using System.Runtime.InteropServices; // Added for CryptographicException

namespace Server.Services
{
    // Helper class to return file data, name, and content type together
    public class FileContentResult
    {
        public byte[] FileData { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public class FileService
    {
        private readonly string _storagePath;
        private readonly IMongoCollection<FileRecord> _files;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;
        private readonly FileEncryptionService _encryptionService; // Injected encryption service

        // Constructor now takes FileEncryptionService
        public FileService(IMongoDatabase database, FileEncryptionService encryptionService)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _storagePath = Path.Combine(AppContext.BaseDirectory, "storage");
            }
            else // if unix
            {
                _storagePath = "/storage";
            }
            Directory.CreateDirectory(_storagePath);

            _files = database.GetCollection<FileRecord>("Files");
            _contentTypeProvider = new FileExtensionContentTypeProvider();
            _encryptionService = encryptionService; // Assign injected service
        }

        // Method to save a *new* file and its metadata with encryption
        public async Task<bool> SaveFileAsync(IFormFile file, string owner)
        {
            // Create owner-specific directory if it doesn't exist
            var ownerStoragePath = Path.Combine(_storagePath, owner);
            Directory.CreateDirectory(ownerStoragePath);

            // Generate a unique filename for the ENCRYPTED file on disk
            // This prevents issues with duplicate original filenames by the same owner
            // and hides the original name on the file system.
            string uniqueFileNameOnDisk = Guid.NewGuid().ToString();
            var filePath = Path.Combine(ownerStoragePath, uniqueFileNameOnDisk);

            try
            {
                // 1. Read file content into a byte array
                byte[] fileContent;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileContent = memoryStream.ToArray();
                }

                // 2. Generate a unique, random encryption key for this specific file
                byte[] perFileKey = _encryptionService.GenerateNewFileKey();

                // 3. Encrypt the file content with the per-file key (AES-256 GCM)
                // This will also generate a unique IV and an Authentication Tag.
                byte[] iv;       // Initialization Vector for data encryption
                byte[] authTag;  // Authentication Tag for data integrity
                byte[] encryptedContent = _encryptionService.EncryptData(fileContent, perFileKey, out iv, out authTag);

                // 4. Encrypt the per-file key itself using the master key (AES-256 GCM)
                // This combined byte array will contain the encrypted per-file key, its IV, and its AuthTag.
                byte[] encryptedPerFileKeyCombined = _encryptionService.EncryptFileKey(perFileKey);

                // 5. Save the ENCRYPTED file content to disk
                await System.IO.File.WriteAllBytesAsync(filePath, encryptedContent);

                // 6. Create a new FileRecord and store metadata in MongoDB
                var fileRecord = new FileRecord
                {
                    FileName = file.FileName, // Store original filename (unencrypted)
                    Owner = owner,
                    SavePath = filePath, // Path to the ENCRYPTED file on disk
                    ViewPermissions = new List<string> { owner }, // Default view permission for the owner
                    EditPermissions = new List<string> { owner }, // Default edit permission for the owner
                    LastUpdateTime = DateTime.UtcNow, // Use UtcNow for consistency
                    EncryptedFileKey = Convert.ToBase64String(encryptedPerFileKeyCombined), // Store Base64 of encrypted per-file key
                    IV = Convert.ToBase64String(iv), // Store Base64 of IV for content encryption
                    AuthTag = Convert.ToBase64String(authTag) // Store Base64 of AuthTag for content encryption
                };

                await _files.InsertOneAsync(fileRecord); // Save metadata in MongoDB async
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception details (e.g., using a logging framework)
                Console.WriteLine($"Error in SaveFileAsync: {ex.Message}"); // For debugging
                // Clean up partially created file if necessary
                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); } catch { /* Log cleanup error */ }
                }
                return false;
            }
        }

        // Method to edit an existing file's content with encryption
        public async Task<bool> EditFileAsync(IFormFile newFile, FileRecord fileRecord)
        {
            try
            {
                // For editing, we re-encrypt the entire new file content.
                // We generate a NEW per-file key and NEW IV for each edit to enhance security.

                // 1. Read new file content into a byte array
                byte[] newFileContent;
                using (var memoryStream = new MemoryStream())
                {
                    await newFile.CopyToAsync(memoryStream);
                    newFileContent = memoryStream.ToArray();
                }

                // 2. Generate a NEW unique key for this updated file version
                byte[] newPerFileKey = _encryptionService.GenerateNewFileKey();

                // 3. Encrypt the new file content with the NEW per-file key
                byte[] newIv;
                byte[] newAuthTag;
                byte[] newEncryptedContent = _encryptionService.EncryptData(newFileContent, newPerFileKey, out newIv, out newAuthTag);

                // 4. Encrypt the NEW per-file key with the master key
                byte[] newEncryptedPerFileKeyCombined = _encryptionService.EncryptFileKey(newPerFileKey);

                // Ensure the directory for the file still exists (should be the owner's)
                Directory.CreateDirectory(Path.GetDirectoryName(fileRecord.SavePath)!);

                // 5. Overwrite the existing encrypted file content on disk
                await System.IO.File.WriteAllBytesAsync(fileRecord.SavePath, newEncryptedContent);

                // 6. Update the FileRecord metadata in MongoDB
                var update = Builders<FileRecord>.Update
                    .Set(f => f.LastUpdateTime, DateTime.UtcNow) // Update last modified time
                    .Set(f => f.EncryptedFileKey, Convert.ToBase64String(newEncryptedPerFileKeyCombined)) // Update encrypted key
                    .Set(f => f.IV, Convert.ToBase64String(newIv)) // Update IV
                    .Set(f => f.AuthTag, Convert.ToBase64String(newAuthTag)); // Update AuthTag

                var result = await _files.UpdateOneAsync(f => f.Id == fileRecord.Id, update);

                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Error in EditFileAsync: {ex.Message}"); // For debugging
                return false;
            }
        }

        // --- GetFileRecord methods (no changes needed here as they don't involve content) ---

        public FileRecord? GetFileRecord(string filename, string owner)
        {
            // Note: This method would now need to search by original FileName, which is fine as FileName is not encrypted
            return _files.Find(x => x.FileName == filename && x.Owner == owner).FirstOrDefault();
        }

        public FileRecord? GetFileRecord(ObjectId fileID)
        {
            return _files.Find(x => x.Id == fileID).FirstOrDefault();
        }

        // --- File Content Methods (updated for decryption) ---

        public async Task<FileContentResult?> GetFileContentAsync(FileRecord fileRecord)
        {
            if (fileRecord == null || string.IsNullOrEmpty(fileRecord.SavePath))
            {
                return null;
            }

            // Ensure all encryption metadata is present
            if (string.IsNullOrEmpty(fileRecord.EncryptedFileKey) ||
                string.IsNullOrEmpty(fileRecord.IV) ||
                string.IsNullOrEmpty(fileRecord.AuthTag))
            {
                Console.WriteLine($"Missing encryption metadata for file {fileRecord.Id}. Cannot decrypt.");
                return null;
            }

            try
            {
                // 1. Read the ENCRYPTED file data from local storage
                byte[] encryptedFileData = await System.IO.File.ReadAllBytesAsync(fileRecord.SavePath);

                // 2. Decode stored encryption parameters from Base64
                byte[] encryptedPerFileKeyCombined = Convert.FromBase64String(fileRecord.EncryptedFileKey);
                byte[] iv = Convert.FromBase64String(fileRecord.IV);
                byte[] authTag = Convert.FromBase64String(fileRecord.AuthTag);

                // 3. Decrypt the per-file key using the master key
                byte[] perFileKey = _encryptionService.DecryptFileKey(encryptedPerFileKeyCombined);

                // 4. Decrypt the file data using the decrypted per-file key, IV, and Auth Tag
                // This call will throw CryptographicException if the data has been tampered with.
                byte[] decryptedFileData = _encryptionService.DecryptData(encryptedFileData, perFileKey, iv, authTag);

                // 5. Determine MIME type based on original file extension
                string contentType;
                if (!_contentTypeProvider.TryGetContentType(fileRecord.FileName, out contentType))
                {
                    contentType = "application/octet-stream"; // Default if unknown
                }

                return new FileContentResult
                {
                    FileData = decryptedFileData,
                    FileName = fileRecord.FileName,
                    ContentType = contentType
                };
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"CRITICAL: Cryptographic error during decryption for file {fileRecord.Id} (possible tampering or corruption): {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving/decrypting file {fileRecord.Id}: {ex.Message}");
                return null;
            }
        }

        // Method to update file permissions 
        public async Task<bool> ShareFileAsync(FileRecord record, List<string> editPerms, List<string> viewPerms)
        {
            if (record == null || editPerms == null || viewPerms == null)
            {
                return false;
            }

            var update = Builders<FileRecord>.Update
                .Set(f => f.ViewPermissions, viewPerms)
                .Set(f => f.EditPermissions, editPerms)
                .Set(f => f.LastUpdateTime, DateTime.UtcNow);

            try
            {
                var result = await _files.UpdateOneAsync(f => f.Id == record.Id, update);
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShareFileAsync: {ex.Message}"); // For debugging
                return false;
            }
        }

        // Method to get all files a user has access to
        public async Task<List<FileRecord>> GetUserFilesAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new List<FileRecord>();
            }

            var filter = Builders<FileRecord>.Filter.Or(
                Builders<FileRecord>.Filter.Eq(f => f.Owner, username),
                Builders<FileRecord>.Filter.AnyEq(f => f.EditPermissions, username),
                Builders<FileRecord>.Filter.AnyEq(f => f.ViewPermissions, username)
            );

            return await _files.Find(filter).ToListAsync();
        }
    }
}