namespace ToolSnap.Mobile.Dtos
{
    public record PhotoSessionDto(
        Guid Id,
        double Latitude,
        double Longitude,
        Guid ActionTypeId,
        DateTimeOffset CreatedAt);

    public record CreatePhotoSessionDto(
    double Latitude,
    double Longitude,
    Guid ActionTypeId);
}
