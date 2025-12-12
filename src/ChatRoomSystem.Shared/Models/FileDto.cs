// ChatRoomSystem.Shared/Models/FileDto.cs
using System;

namespace ChatRoomSystem.Shared.Models;

public enum FileType
{
    Image,
    Video,
    Audio,
    Document,
    Other
}

public class UploadedFileDto
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
}