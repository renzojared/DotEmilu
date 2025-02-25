namespace DotEmilu.UseCaseTest.DTOs;

public record InDto(int Day) : IRequest;

public class InDtoValidator : AbstractValidator<InDto>
{
    public InDtoValidator()
    {
        RuleFor(x => x.Day)
            .GreaterThan(0);
    }
}