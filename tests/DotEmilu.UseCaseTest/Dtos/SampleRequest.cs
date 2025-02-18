namespace DotEmilu.UseCaseTest.Dtos;

public record SampleRequest(DateOnly Date, decimal Amount, int Category, int Account, string? Note, string? Description);