namespace DotEmilu.Abstractions;

/// <summary>
/// Represents a request that does not return a response.
/// </summary>
public interface IRequest : IBaseRequest;

/// <summary>
/// Represents a request that returns a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse> : IBaseRequest;

/// <summary>
/// Marker interface to represent a base request.
/// </summary>
public interface IBaseRequest;