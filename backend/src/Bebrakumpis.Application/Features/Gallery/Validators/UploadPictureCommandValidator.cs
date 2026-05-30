using Bebrakumpis.Application.Features.Gallery.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Gallery.Validators;

public class UploadPictureCommandValidator : AbstractValidator<UploadPictureCommand>
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];
    private const long MaxFileSize = 10 * 1024 * 1024;

    public UploadPictureCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Only JPEG and PNG images are allowed.");
        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(MaxFileSize).WithMessage("File must not exceed 10 MB.");
        RuleFor(x => x.Content)
            .Must(HasValidMagicBytes)
            .WithMessage("File content does not match a valid JPEG or PNG.");
    }

    private static bool HasValidMagicBytes(Stream content)
    {
        if (!content.CanSeek) return true;
        var header = new byte[4];
        var read = content.Read(header, 0, 4);
        content.Seek(0, SeekOrigin.Begin);
        if (read < 3) return false;
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
        if (read >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
        return false;
    }
}
