using System;
using System.IO;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Drive.v3.Data;

namespace CopticDictionarynew1.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly IConfiguration _configuration;

        public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        private DriveService CreateDriveService()
        {
            try
            {
                _logger.LogInformation("🧪 Step 1: Getting configuration...");
                
                var keyPath = _configuration["GoogleDrive:ServiceAccountKeyPath"];
                var folderId = _configuration["GoogleDrive:FolderId"];

                if (string.IsNullOrEmpty(keyPath))
                    throw new ArgumentException("❌ GoogleDrive:ServiceAccountKeyPath not configured in appsettings.json");

                if (!System.IO.File.Exists(keyPath))
                    throw new FileNotFoundException($"❌ Key file not found: {keyPath}");

                _logger.LogInformation("✅ Configuration paths are valid.");

                _logger.LogInformation("🧪 Step 2: Authenticating with Google...");
                GoogleCredential credential = GoogleCredential
                    .FromFile(keyPath)
                    .CreateScoped(DriveService.Scope.Drive);

                _logger.LogInformation("✅ Authenticated successfully.");

                _logger.LogInformation("🧪 Step 3: Creating DriveService...");
                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Coptic Dictionary"
                });

                _logger.LogInformation("✅ DriveService created.");

                // Verify folder ID if provided
                if (!string.IsNullOrEmpty(folderId))
                {
                    _logger.LogInformation("🧪 Step 4: Verifying folder ID...");
                    var folderRequest = service.Files.Get(folderId);
                    folderRequest.Fields = "id, name";
                    var folder = folderRequest.Execute();
                    _logger.LogInformation($"✅ Folder found: {folder.Name} (ID: {folder.Id})");
                }

                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating DriveService");
                throw;
            }
        }

        public async Task<string> UploadAudioFileAsync(Stream audioStream, string fileName)
        {
            DriveService service = null;
            try
            {
                _logger.LogInformation("🔍 Starting Google Drive upload for file: {FileName}", fileName);

                service = CreateDriveService();

                // Reset stream position
                if (audioStream.CanSeek)
                {
                    audioStream.Position = 0;
                }

                var folderId = _configuration["GoogleDrive:FolderId"];

                _logger.LogInformation("🧪 Step 5: Preparing file metadata...");
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName,
                    Parents = !string.IsNullOrEmpty(folderId) ? new List<string> { folderId } : null
                };

                _logger.LogInformation("🧪 Step 6: Starting upload...");
                var uploadRequest = service.Files.Create(fileMetadata, audioStream, GetMimeType(fileName));
                uploadRequest.Fields = "id, name, webViewLink";

                var progress = await uploadRequest.UploadAsync();

                _logger.LogInformation($"🧪 Step 7: Upload result: {progress.Status}");

                if (progress.Status == UploadStatus.Completed)
                {
                    var uploadedFile = uploadRequest.ResponseBody;
                    _logger.LogInformation($"✅ File uploaded: {uploadedFile.Name} (ID: {uploadedFile.Id})");

                    // Make file publicly accessible
                    await MakeFilePublic(service, uploadedFile.Id);

                    // Return the direct download link instead of file ID
                    var directLink = $"https://drive.google.com/uc?export=download&id={uploadedFile.Id}";
                    _logger.LogInformation($"🔗 Direct download link: {directLink}");

                    return directLink;
                }
                else if (progress.Status == UploadStatus.Failed)
                {
                    var errorMessage = progress.Exception?.Message ?? "Unknown upload error";
                    _logger.LogError($"❌ Upload failed: {errorMessage}");
                    
                    if (progress.Exception != null)
                        _logger.LogError("📜 Exception Stack Trace:\n{StackTrace}", progress.Exception.StackTrace);

                    throw new Exception($"Upload failed: {errorMessage}");
                }

                throw new Exception("Upload completed with unknown status");
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError($"❌ Google API Error: {ex.Message}");
                _logger.LogError($"📜 Details: {ex.Error?.Message}");
                _logger.LogError($"🌐 Status Code: {ex.HttpStatusCode}");
                throw new Exception($"Google API Error: {ex.Message} (Status: {ex.HttpStatusCode})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ General Error uploading to Google Drive");
                throw;
            }
            finally
            {
                service?.Dispose();
            }
        }

        private async Task MakeFilePublic(DriveService service, string fileId)
        {
            try
            {
                _logger.LogInformation("🧪 Step 8: Making file publicly accessible...");
                
                var permission = new Permission()
                {
                    Role = "reader",
                    Type = "anyone"
                };

                await service.Permissions.Create(permission, fileId).ExecuteAsync();
                _logger.LogInformation("✅ File made publicly accessible: {FileId}", fileId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Could not make file public: {FileId}", fileId);
                // Don't throw here, as the file was uploaded successfully
            }
        }

        public async Task<bool> DeleteFileAsync(string fileId)
        {
            DriveService service = null;
            try
            {
                _logger.LogInformation("🗑️ Deleting file from Google Drive");
                
                // Extract file ID from URL if it's a full URL
                var actualFileId = ExtractFileIdFromUrl(fileId);
                
                service = CreateDriveService();
                await service.Files.Delete(actualFileId).ExecuteAsync();
                
                _logger.LogInformation("✅ File deleted successfully: {FileId}", actualFileId);
                return true;
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError($"❌ Google API Error deleting file: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting file from Google Drive");
                return false;
            }
            finally
            {
                service?.Dispose();
            }
        }

        public async Task<string> GetPublicLinkAsync(string fileId)
        {
            try
            {
                // If it's already a full URL, return as is
                if (fileId.Contains("drive.google.com"))
                    return fileId;
                    
                // Otherwise, create the direct download link
                return $"https://drive.google.com/uc?export=download&id={fileId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting public link for file: {FileId}", fileId);
                throw;
            }
        }

        private string GetMimeType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            var mimeTypes = new Dictionary<string, string>
            {
                { ".wav", "audio/wav" },
                { ".mp3", "audio/mpeg" },
                { ".ogg", "audio/ogg" },
                { ".m4a", "audio/mp4" },
                { ".txt", "text/plain" },
                { ".pdf", "application/pdf" },
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".doc", "application/msword" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
            };

            return mimeTypes.ContainsKey(ext) ? mimeTypes[ext] : "application/octet-stream";
        }

        private string ExtractFileIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            // If it's already a file ID (no URL), return as is
            if (!url.Contains("drive.google.com"))
                return url;

            // Extract file ID from various Google Drive URL formats
            if (url.Contains("/file/d/"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"/file/d/([a-zA-Z0-9-_]+)");
                return match.Success ? match.Groups[1].Value : url;
            }
            else if (url.Contains("id="))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"id=([a-zA-Z0-9-_]+)");
                return match.Success ? match.Groups[1].Value : url;
            }

            return url;
        }
    }
}