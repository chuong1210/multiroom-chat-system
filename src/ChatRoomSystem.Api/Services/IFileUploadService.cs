using ChatRoomSystem.Shared.Models;

namespace ChatRoomSystem.Api.Services
{

    public interface IFileUploadService
    {
        Task<UploadedFileDto?> UploadFileAsync(IFormFile file, string folder = "uploads");
        Task<bool> DeleteFileAsync(string fileUrl);
        Task<UploadedFileDto?> UploadImageAsync(IFormFile file, int maxWidth = 1920, int maxHeight = 1080);
        Task<UploadedFileDto?> UploadVideoAsync(IFormFile file);
    }
}
