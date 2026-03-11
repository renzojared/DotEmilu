namespace DotEmilu.Abstractions;

public interface IContextUser<out TUserKey>
    where TUserKey : struct
{
    TUserKey Id { get; }
}