using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Server.Services;
using System.Security.Claims;
using System.IO;
using MongoDB.Bson;


namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly FileService _fileService;

        public FileController(FileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file) 
        {
            try
            {
                string? username = getUsername();
                if (string.IsNullOrEmpty(username))
                { 
                    return Unauthorized(new { Message = "User not authenticated." });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Message = "No file uploaded or file is empty." });
                }

                // Save the new file. FileService will handle creating the record.
                // Check if a file with the same name already exists for this user
                var existingRecord = _fileService.GetFileRecord(file.FileName, username);
                if (existingRecord != null)
                {
                    // If a file with the same name exists for the user, treat this as a conflict
                    // and instruct the user to use the edit endpoint if they intended to edit.
                    return Conflict(new { Message = "A file with this name already exists for this user. Use the edit endpoint to modify it." });
                }


                bool success = await _fileService.SaveFileAsync(file, username); 
                if (success)
                {
                    return Ok(new { Message = "File uploaded successfully." });
                }
                // If SaveFileAsync returns false, it indicates an issue on the service side
                return StatusCode(500, new { Message = "File upload failed due to an internal issue." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred during file upload.", Error = ex.Message });
            }
        }

        //  endpoint for editing an existing file by its ID.
        [HttpPut("edit/{fileId}")] // Using PUT as it's an update operation
        public async Task<IActionResult> EditFile(string fileId, IFormFile file)
        {
            try
            {
                string? username = getUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { Message = "User not authenticated." });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Message = "No file provided for editing or file is empty." });
                }

                // Validate fileId is a valid ObjectId
                if (!ObjectId.TryParse(fileId, out ObjectId objectId))
                {
                    return BadRequest(new { Message = "Invalid file ID format." });
                }

                // Get the file record by ID
                FileRecord? record = _fileService.GetFileRecord(objectId);
                if (record == null)
                {
                    return NotFound(new { Message = "File not found." });
                }

                // Check if the user has edit permissions (owner or explicitly listed)
                if (!(record.Owner == username || record.EditPermissions.Contains(username)))
                {
                    return StatusCode(403,new { Message = "User is not authorized to edit this file." }); // Use Forbid for authorization failures
                }

                // Edit the file content and update the record (like LastUpdateTime)
                bool success = await _fileService.EditFileAsync(file, record); // Changed to async call
                if (success)
                {
                    return Ok(new { Message = "File edited successfully." });
                }
                // If EditFileAsync returns false, it indicates an issue on the service side
                return StatusCode(500, new { Message = "File edit failed due to an internal issue." });
            }
            catch (Exception ex)
            {
                // Log the exception details in a real application
                return StatusCode(500, new { Message = "An error occurred during file edit.", Error = ex.Message });
            }
        }


        // Download file by ID
        [HttpGet("{fileId}")] // Route changed to use fileId
        public async Task<IActionResult> DownloadFile(string fileId) // Parameter changed to fileId
        {
            string? username = getUsername();
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { Message = "User not authenticated." });
            }

            // Validate fileId is a valid ObjectId
            if (!ObjectId.TryParse(fileId, out ObjectId objectId))
            {
                return BadRequest(new { Message = "Invalid file ID format." });
            }

            // Get the file record by ID
            FileRecord? fileRecord = _fileService.GetFileRecord(objectId); // Get record by ObjectId
            if (fileRecord == null)
            {
                return NotFound(new { Message = "File not found." });
            }

            // Check if the user has permission (Owner, Edit, or View)
            if (!(fileRecord.Owner == username ||
                  fileRecord.EditPermissions.Contains(username) ||
                  fileRecord.ViewPermissions.Contains(username)))
            {
                return StatusCode(500 ,new{ Message = "User is not authorized to access the file." }); // Use Forbid
            }

            try
            {
                // Use the service to get file content and details
                var fileContentResult = await _fileService.GetFileContentAsync(fileRecord); 

                if (fileContentResult == null)
                {
                    return StatusCode(500, new { Message = "Error reading file content from storage." });
                }

                byte[] fileData = fileContentResult.FileData;
                string contentType = fileContentResult.ContentType;
                string downloadFileName = fileContentResult.FileName; // Use the filename from the record

                return File(fileData, contentType, downloadFileName);
            }
            catch (Exception ex)
            {
                // Log the exception details in a real application
                return StatusCode(500, new { Message = "Error retrieving or reading file.", Error = ex.Message });
            }
        }

        // Share file by ID (keeping the existing ObjectId overload)
        // Removed the duplicate ShareFile endpoint that used filename and owner.
        [HttpPost("share")]
        public async Task<IActionResult> ShareFile(
        [FromQuery] string fileId, // Use string for parameter binding, then parse
        [FromQuery] List<string> editPermissions,
        [FromQuery] List<string> viewPermissions)
        {
            string? username = getUsername();
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { Message = "User not authenticated." });
            }

            // Validate fileId is a valid ObjectId
            if (!ObjectId.TryParse(fileId, out ObjectId objectId))
            {
                return BadRequest(new { Message = "Invalid file ID format." });
            }


            FileRecord? file = _fileService.GetFileRecord(objectId); // Get record by ObjectId
            if (file == null)
            {
                return NotFound(new { Message = "File not found." }); // Fixed typo
            }

            // Only the file owner can share the file
            if (username != file.Owner)
            {
                return Forbid("Unauthorized to edit file permmisions. Only file owner can share."); 
            }

            // Ensure owner is always included in permissions if desired (optional logic)
            if (!editPermissions.Contains(username)) editPermissions.Add(username);
            if (!viewPermissions.Contains(username)) viewPermissions.Add(username);


            bool success = await _fileService.ShareFileAsync(file, editPermissions, viewPermissions);
            if (success)
            {
                return Ok(new { Message = "File shared successfully." });
            }
            return BadRequest(new { Message = "File sharing failed." });
        }


        // This endpoint remains the same as it lists files *for a user*,
        // not a specific file by ID.
        [HttpGet("listFiles")]
        public async Task<IActionResult> GetUserFiles()
        {
            try
            {
                string? username = getUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { Message = "User not authenticated." });
                }

                var files = await _fileService.GetUserFilesAsync(username);
                return Ok(files);
            }
            catch (Exception ex)
            {
                // Log the exception details in a real application
                return StatusCode(500, new { Message = "An error occurred retrieving user files.", Error = ex.Message }); // More specific error
            }
        }

        // Helper method to get the authenticated user's name from claims
        private string? getUsername()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}