using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Server.Services;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;


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

        private string GetUserName()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }


        [HttpPost("upload")]
        public IActionResult UploadFile(IFormFile file, string owner)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "Invalid file." });
            }

            bool success = _fileService.SaveFile(file, owner);
            if (success)
            {
                return Ok(new { Message = "File uploaded successfully." });
            }
            return StatusCode(500, new { Message = "File upload failed." });
        }


        [HttpGet("{filename}")]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            string username = getUsername();
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { Message = "User not authenticated." });
            }

            FileRecord? file = _fileService.GetFileRecord(filename);
            if (file == null)
            {
                return NotFound(new { Message = "File not found." });
            }

            if (!(file.Owner == username ||
                  file.EditPermissions.Contains(username) ||
                  file.ViewPermissions.Contains(username)))
            {
                return Unauthorized(new { Message = "User is not authorized to access the file." });
            }

            try
            {
                byte[] fileData = System.IO.File.ReadAllBytes(file.SavePath);

                // Determine MIME type based on file extension
                string contentType;
                new FileExtensionContentTypeProvider().TryGetContentType(filename, out contentType);

                // Default to "application/octet-stream" if MIME type is unknown
                contentType ??= "application/octet-stream";

                return File(fileData, contentType, filename);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error reading file.", Error = ex.Message });
            }
        }

        [HttpPost("share")]
        public async Task<IActionResult> ShareFile(string filename, string shareType, string sharedUser)
        {
            bool success = _fileService.ShareFile(filename, shareType, sharedUser);
            if (success)
            {
                return Ok(new { Message = "File shared successfully." });
            }
            return BadRequest(new { Message = "File sharing failed." });
        }

        [HttpGet("listFiles")]
        public async Task<IActionResult> GetUserFiles()
        {
            string? username = getUsername();
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { Message = "User not authenticated." });
            }

            var files = await _fileService.GetUserFilesAsync(username);
            return Ok(files);
        }

        private string? getUsername()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
