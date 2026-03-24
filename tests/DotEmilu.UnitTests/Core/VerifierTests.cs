using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.UnitTests.Core;

public class VerifierTests
{
    private sealed record TestRequest(int Value) : IRequest;

    [Fact]
    public async Task ValidateAsync_WhenNoValidatorsRegistered_ThenIsValid()
    {
        var verifier = BuildVerifier([]);

        await verifier.ValidateAsync(new TestRequest(1), CancellationToken.None);

        Assert.True(verifier.IsValid);
        Assert.Empty(verifier.ValidationErrors);
    }

    [Fact]
    public async Task ValidateAsync_WhenValidatorFails_ThenCollectsErrors()
    {
        var verifier = BuildVerifier([new FailingValidator()]);

        await verifier.ValidateAsync(new TestRequest(0), CancellationToken.None);

        Assert.False(verifier.IsValid);
        var error = Assert.Single(verifier.ValidationErrors);
        Assert.Equal("Value", error.PropertyName);
    }

    [Fact]
    public async Task ValidateAsync_WhenMultipleValidatorsFail_ThenAggregatesErrors()
    {
        var verifier = BuildVerifier([new FailingValidator(), new AnotherFailingValidator()]);

        await verifier.ValidateAsync(new TestRequest(0), CancellationToken.None);

        Assert.False(verifier.IsValid);
        Assert.Equal(2, verifier.ValidationErrors.Count);
    }

    [Fact]
    public async Task ValidateAsync_WhenCalledMultipleTimes_ThenAccumulatesErrors()
    {
        var verifier = BuildVerifier([new FailingValidator()]);

        await verifier.ValidateAsync(new TestRequest(0), CancellationToken.None);
        await verifier.ValidateAsync(new TestRequest(0), CancellationToken.None);

        Assert.False(verifier.IsValid);
        Assert.Equal(2, verifier.ValidationErrors.Count);
    }

    [Fact]
    public async Task ValidateAsync_WhenCancellationRequested_ThenPropagatesOperationCanceledException()
    {
        var verifier = BuildVerifier([new CancellationAwareValidator()]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            verifier.ValidateAsync(new TestRequest(1), cts.Token));
    }

    [Fact]
    public void AddValidationError_WhenUsingPropertyAndMessage_ThenAddsError()
    {
        var verifier = BuildVerifier([]);

        verifier.AddValidationError("Field", "Error message");

        Assert.False(verifier.IsValid);
        var error = Assert.Single(verifier.ValidationErrors);
        Assert.Equal("Error message", error.ErrorMessage);
    }

    [Fact]
    public void AddValidationError_WhenUsingValidationFailure_ThenAddsError()
    {
        var verifier = BuildVerifier([]);
        var failure = new ValidationFailure("Prop", "Fail");

        verifier.AddValidationError(failure);

        Assert.False(verifier.IsValid);
        var error = Assert.Single(verifier.ValidationErrors);
        Assert.Equal("Prop", error.PropertyName);
    }

    [Fact]
    public void AddValidationErrors_WhenUsingList_ThenMergesAllFailures()
    {
        var verifier = BuildVerifier([]);
        var failures = new List<ValidationFailure>
        {
            new("A", "Error A"),
            new("B", "Error B"),
            new("C", "Error C")
        };

        verifier.AddValidationErrors(failures);

        Assert.False(verifier.IsValid);
        Assert.Equal(3, verifier.ValidationErrors.Count);
    }

    private static IVerifier<TestRequest> BuildVerifier(IEnumerable<IValidator<TestRequest>> validators)
    {
        var services = new ServiceCollection();
        services.AddVerifier();
        foreach (var v in validators)
            services.AddScoped<IValidator<TestRequest>>(_ => v);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IVerifier<TestRequest>>();
    }

    private sealed class FailingValidator : AbstractValidator<TestRequest>
    {
        public FailingValidator()
        {
            RuleFor(x => x.Value).GreaterThan(0);
        }
    }

    private sealed class AnotherFailingValidator : AbstractValidator<TestRequest>
    {
        public AnotherFailingValidator()
        {
            RuleFor(x => x.Value).NotEqual(0).WithMessage("Must not be zero");
        }
    }

    private sealed class CancellationAwareValidator : AbstractValidator<TestRequest>
    {
        public CancellationAwareValidator()
        {
            RuleFor(x => x.Value)
                .MustAsync((_, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.FromResult(true);
                });
        }
    }
}
