// ChatRoomSystem.Api/Services/LocalFileUploadService.cs
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ChatRoomSystem.Api.Services;

public class LocalFileUploadService : IFileUploadService
{
    private readonly string _uploadPath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileUploadService> _logger;

    public LocalFileUploadService(
        IWebHostEnvironment env,
        IConfiguration config,
        ILogger<LocalFileUploadService> logger)
    {
        _uploadPath = Path.Combine(env.WebRootPath, "uploads");
        _baseUrl = config["AppSettings:BaseUrl"] ?? "http://localhost:5000";
        _logger = logger;

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<UploadedFileDto?> UploadFileAsync(IFormFile file, string folder = "uploads")
    {
        try
        {
            if (file == null || file.Length == 0)
                return null;

            // Generate unique filename
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var folderPath = Path.Combine(_uploadPath, folder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"{_baseUrl}/uploads/{folder}/{fileName}";

            return new UploadedFileDto
            {
                Url = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                MimeType = file.ContentType,
                FileType = GetFileType(file.ContentType)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return null;
        }
    }

    public async Task<UploadedFileDto?> UploadImageAsync(IFormFile file, int maxWidth = 1920, int maxHeight = 1080)
    {
        try
        {
            if (file == null || file.Length == 0)
                return null;

            var fileName = $"{Guid.NewGuid()}.jpg";
            var folderPath = Path.Combine(_uploadPath, "images");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);

            using (var image = await Image.LoadAsync(file.OpenReadStream()))
            {
                var originalWidth = image.Width;
                var originalHeight = image.Height;

                // Resize if needed
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(maxWidth, maxHeight),
                        Mode = ResizeMode.Max
                    }));
                }

                await image.SaveAsJpegAsync(filePath);

                var fileUrl = $"{_baseUrl}/uploads/images/{fileName}";

                // Generate thumbnail
                var thumbnailFileName = $"{Guid.NewGuid()}_thumb.jpg";
                var thumbnailPath = Path.Combine(folderPath, thumbnailFileName);

                using (var thumbnail = await Image.LoadAsync(filePath))
                {
                    thumbnail.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(200, 200),
                        Mode = ResizeMode.Max
                    }));

                    await thumbnail.SaveAsJpegAsync(thumbnailPath);
                }

                var thumbnailUrl = $"{_baseUrl}/uploads/images/{thumbnailFileName}";

                return new UploadedFileDto
                {
                    Url = fileUrl,
                    FileName = file.FileName,
                    FileSize = new FileInfo(filePath).Length,
                    MimeType = "image/jpeg",
                    FileType = FileType.Image,
                    ThumbnailUrl = thumbnailUrl,
                    Width = image.Width,
                    Height = image.Height
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return null;
        }
    }

    public async Task<UploadedFileDto?> UploadVideoAsync(IFormFile file)
    {
        // For now, just save the video file
        // In production, you'd want to:
        // 1. Generate thumbnail using FFmpeg
        // 2. Extract metadata (duration, resolution)
        // 3. Possibly transcode to web-friendly format

        var result = await UploadFileAsync(file, "videos");
        if (result != null)
        {
            result.FileType = FileType.Video;
        }
        return result;
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            // Extract file path from URL
            var uri = new Uri(fileUrl);
            var relativePath = uri.AbsolutePath.TrimStart('/');
            var filePath = Path.Combine(_uploadPath, "..", relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return Task.FromResult(false);
        }
    }

    private FileType GetFileType(string mimeType)
    {
        if (mimeType.StartsWith("image/"))
            return FileType.Image;
        if (mimeType.StartsWith("video/"))
            return FileType.Video;
        if (mimeType.StartsWith("audio/"))
            return FileType.Audio;
        if (mimeType.Contains("pdf") || mimeType.Contains("document") || mimeType.Contains("word") || mimeType.Contains("excel"))
            return FileType.Document;

        return FileType.Other;
    }
}