using System.Security.Claims;
using MediatR;

namespace Graphite.Api.Features.Images.UploadImage;

public record UploadImageCommand(
    int PrId,
    byte[] ImageData,
    string FileName,
    string ContentType,
    ClaimsPrincipal User) : IRequest<UploadImageResult>;

public record UploadImageResult(string Url);
