namespace DotEmilu.Abstractions;

/// <summary>
/// Represents the current user context.
/// </summary>
/// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
public interface IContextUser<out TUserKey>
    where TUserKey : struct
{
    /// <summary>Gets the identifier of the current user.</summary>
    TUserKey Id { get; }
}