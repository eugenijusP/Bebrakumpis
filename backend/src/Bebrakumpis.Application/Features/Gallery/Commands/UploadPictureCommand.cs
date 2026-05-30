using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Gallery.DTOs;
using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Gallery.Commands;

public record UploadPictureCommand(Stream Content, long FileSize, string ContentType, string FileName)
    : IRequest<Result<PictureResponse>>;

public class UploadPictureCommandHandler(
    IPictureRepository pictureRepository,
    IBlobStorageService blobStorageService,
    IValidator<UploadPictureCommand> validator)
    : IRequestHandler<UploadPictureCommand, Result<PictureResponse>>
{
    public async Task<Result<PictureResponse>> HandleAsync(UploadPictureCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<PictureResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var extension = command.ContentType == "image/png" ? ".png" : ".jpg";
        var blobName = $"{Guid.NewGuid():N}{extension}";

        var blobUrl = await blobStorageService.UploadAsync(command.Content, command.ContentType, blobName, ct);

        var maxOrder = await pictureRepository.GetMaxOrderAsync(ct);
        var picture = new Picture
        {
            Id = Guid.NewGuid(),
            BlobUrl = blobUrl,
            Order = maxOrder + 1,
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            await pictureRepository.CreateAsync(picture, ct);
        }
        catch
        {
            await blobStorageService.DeleteByUrlAsync(blobUrl, ct);
            throw;
        }

        return Result<PictureResponse>.Success(new PictureResponse
        {
            Id = picture.Id,
            BlobUrl = picture.BlobUrl,
            Order = picture.Order,
            UploadedAt = picture.UploadedAt
        });
    }
}
