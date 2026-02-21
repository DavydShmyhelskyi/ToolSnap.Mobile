namespace ToolSnap.Mobile.Dtos
{
    public record UserDto(
        Guid Id,
        string FullName,
        string Email,
        string Role,
        bool IsActive,
        bool EmailConfirmed);

    public record LoginDto(
        string Email, 
        string Password);

    public record AuthenticationResponseDto(
        Guid Id,
        string FullName,
        string Email,
        string Role,
        bool IsActive,
        bool EmailConfirmed,
        string AccessToken,
        string RefreshToken);

    public record RefreshTokenDto(
        string RefreshToken);
}
