using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Gallery.DTOs;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Gallery.Commands;

public record UpdatePictureOrderCommand(Guid Id, int Order) : IRequest<Result<PictureResponse>>;

public class UpdatePictureOrderCommandHandler(
    IPictureRepository pictureRepository,
    IValidator<UpdatePictureOrderCommand> validator)
    : IRequestHandler<UpdatePictureOrderCommand, Result<PictureResponse>>
{
    public async Task<Result<PictureResponse>> HandleAsync(UpdatePictureOrderCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<PictureResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var picture = await pictureRepository.GetByIdAsync(command.Id, ct);
        if (picture is null)
            return Result<PictureResponse>.NotFound($"Picture '{command.Id}' not found.");

        await pictureRepository.UpdateOrderAsync(command.Id, command.Order, ct);
        picture.Order = command.Order;

        return Result<PictureResponse>.Success(new PictureResponse
        {
            Id = picture.Id,
            BlobUrl = picture.BlobUrl,
            Order = picture.Order,
            UploadedAt = picture.UploadedAt
        });
    }
}
