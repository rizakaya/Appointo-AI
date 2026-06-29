namespace Appointo.Tools;

public sealed record UserContext(UserRole Role, string? CustomerName = null, string? PhoneNumber = null)
{
    public static UserContext Guest { get; } = new(UserRole.Guest);
}
