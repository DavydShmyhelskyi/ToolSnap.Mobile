namespace ToolSnap.Mobile.Dtos
{
    public record PhotoForDetectionDto(
        Guid Id,
        Guid PhotoSessionId,
        string OriginalName,
        DateTimeOffset UploadDate);
}
