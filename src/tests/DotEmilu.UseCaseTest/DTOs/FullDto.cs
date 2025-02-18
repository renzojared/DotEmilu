namespace DotEmilu.UseCaseTest.DTOs;

public record FullDto(int Day, int Month, int Year) : IRequest<FullOutDto>;
public record FullOutDto();