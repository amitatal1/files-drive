using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers
{
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
        public IActionResult DownloadFile(string filename)
        {
            var fileData = _fileService.GetFileContent(filename);
            if (fileData != null)
            {
                return File(fileData, "application/octet-stream", filename);
            }
            return NotFound(new { Message = "File not found." });
        }

        [HttpPost("share")]
        public IActionResult ShareFile(string filename, string shareType, string sharedUser)
        {
            bool success = _fileService.ShareFile(filename, shareType, sharedUser);
            if (success)
            {
                return Ok(new { Message = "File shared successfully." });
            }
            return BadRequest(new { Message = "File sharing failed." });
        }
    }
}
