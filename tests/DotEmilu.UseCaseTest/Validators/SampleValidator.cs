namespace DotEmilu.UseCaseTest.Validators;

public class SampleValidator : AbstractValidator<SampleRequest>
{
    public SampleValidator()
    {
        RuleFor(s => s.Date)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now));

        RuleFor(s => s.Amount)
            .GreaterThan(0);
    }
}