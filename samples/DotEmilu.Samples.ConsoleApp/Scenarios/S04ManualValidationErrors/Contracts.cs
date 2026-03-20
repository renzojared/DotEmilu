namespace DotEmilu.Samples.ConsoleApp.Scenarios.S04ManualValidationErrors;

/// <summary>
/// Request to authenticate a user.
/// Defined locally so <c>LoginHandler</c> is the sole <c>IHandler&lt;LoginRequest, LoginResult&gt;</c>
/// in the assembly, enabling unambiguous resolution via <c>AddHandlers</c>.
/// </summary>
public sealed record LoginRequest(string Username, string Password) : IRequest<LoginResult>;

/// <summary>
/// Result of a successful authentication attempt.
/// </summary>
public sealed record LoginResult(string Token, DateTimeOffset ExpiresAt);
