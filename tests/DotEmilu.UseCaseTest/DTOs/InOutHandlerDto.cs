namespace DotEmilu.UseCaseTest.DTOs;

public record InOutHandlerDto : IRequest<InOutHandlerDtoResponse>;
public record InOutHandlerDtoResponse(int Month);