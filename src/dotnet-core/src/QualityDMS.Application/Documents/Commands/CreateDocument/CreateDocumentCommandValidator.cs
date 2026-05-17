using FluentValidation;

namespace QualityDMS.Application.Documents.Commands.CreateDocument;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).LessThanOrEqualTo(52_428_800); // 50 MB
        RuleFor(x => x.ContentType).NotEmpty();
    }
}
