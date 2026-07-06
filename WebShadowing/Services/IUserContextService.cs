namespace WebShadowing.Services;

public interface IUserContextService
{
    bool IsAuthenticated { get; }
    long? GetCurrentUserId();
}
