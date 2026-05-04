using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.Images.UploadImage;

public class UploadImageHandler(
    AppDbContext context,
    IPullRequestValidationService validationService,
    IConfiguration configuration,
    ILogger<UploadImageHandler> logger) : IRequestHandler<UploadImageCommand, UploadImageResult>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"
    };

    private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
        ["image/svg+xml"] = ".svg",
    };

    private const long MaxFileSize = 25 * 1024 * 1024;

    private string GetStoragePath()
    {
        var configured = configuration["ImageUpload:StoragePath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;
        return Path.Combine(AppContext.BaseDirectory, "uploads", "images");
    }

    public async Task<UploadImageResult> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        await validationService.GetRequiredUserWithTokenAsync(request.User);

        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new InvalidOperationException($"Unsupported image type: {request.ContentType}. Allowed: png, jpg, gif, webp, svg");

        if (request.ImageData.Length > MaxFileSize)
            throw new InvalidOperationException("Image size exceeds the 25 MB limit.");

        var pr = await context.PullRequests.FirstOrDefaultAsync(p => p.Id == request.PrId, cancellationToken)
            ?? throw new KeyNotFoundException("Pull request not found.");

        var ext = ContentTypeToExtension.GetValueOrDefault(request.ContentType, ".png");
        var uniqueName = $"{Guid.NewGuid()}{ext}";

        var uploadDir = GetStoragePath();
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, uniqueName);
        await File.WriteAllBytesAsync(filePath, request.ImageData, cancellationToken);

        var url = $"/api/images/uploads/{uniqueName}";

        logger.LogInformation("Image saved for PR {PrId}: {File} ({Size} bytes)", request.PrId, uniqueName, request.ImageData.Length);
        return new UploadImageResult(url);
    }
}
