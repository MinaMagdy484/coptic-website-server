namespace CopticDictionarynew1.Services
{
    public class LocalFileService : IGoogleDriveService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalFileService> _logger;

        public LocalFileService(IWebHostEnvironment environment, ILogger<LocalFileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadAudioFileAsync(Stream audioStream, string fileName)
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "pronunciations");
                Directory.CreateDirectory(uploadsPath);

                var filePath = Path.Combine(uploadsPath, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await audioStream.CopyToAsync(fileStream);
                }

                var relativePath = $"/uploads/pronunciations/{fileName}";
                _logger.LogInformation("File saved locally: {FilePath}", relativePath);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file locally");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileId)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, fileId.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting local file");
                return false;
            }
        }

        public async Task<string> GetPublicLinkAsync(string fileId)
        {
            return fileId; // For local files, return the relative path
        }
    }
}