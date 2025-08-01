
namespace CopticDictionarynew1.Services
{
    public interface IGoogleDriveService
    {
        Task<string> UploadAudioFileAsync(Stream audioStream, string fileName);
        Task<bool> DeleteFileAsync(string fileId);
        Task<string> GetPublicLinkAsync(string fileId);
    }
}