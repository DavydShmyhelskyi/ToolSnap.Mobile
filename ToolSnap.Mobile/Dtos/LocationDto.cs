namespace ToolSnap.Mobile.Dtos
{
    public record LocationDto(
        Guid Id,
        string Name,
        Guid LocationTypeId,
        string? Address,
        double Latitude,
        double Longitude,
        bool IsActive,
        DateTimeOffset CreatedAt);

}
