namespace ToolSnap.Mobile.Dtos
{
    public record UserDto(
        Guid Id,
        string FullName,
        string Email,
        bool ConfirmedEmail,
        Guid RoleId,
        bool IsActive,
        DateTime CreatedAt,
        double? Longitude,
        double? Latitude);

    public record LoginDto(
        string Email,
        string Password,
        double Longitude,
        double Latitude);

    public record AuthenticationResponseDto(
        Guid Id,
        string FullName,
        string Email,
        bool EmailConfirmed,
        Guid RoleId,
        bool IsActive,
        string AccessToken,
        string RefreshToken);

    public record RefreshTokenDto(
        string RefreshToken);
}
