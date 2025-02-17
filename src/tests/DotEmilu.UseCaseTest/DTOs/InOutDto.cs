namespace DotEmilu.UseCaseTest.DTOs;

public record InOutDto(int Day) : IRequest<InOutDtoResponse>;
public record InOutDtoResponse;

public class InOutDtoValidator : AbstractValidator<InOutDto>
{
    public InOutDtoValidator()
    {
        RuleFor(x => x.Day).GreaterThan(1);
    }
}