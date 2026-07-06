namespace WebShadowing.Models;

public class AuthResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public User? User { get; init; }

    public static AuthResult Success(User user) => new() { Succeeded = true, User = user };

    public static AuthResult Failure(string message) => new() { Succeeded = false, Message = message };
}
