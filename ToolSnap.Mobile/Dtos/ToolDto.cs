
namespace ToolSnap.Mobile.Dtos
{
    public record ToolDto(
        Guid Id,
        Guid ToolTypeId,
        Guid? BrandId,
        Guid? ModelId,
        string? SerialNumber,
        Guid ToolStatusId,
        DateTimeOffset CreatedAt);
    public record ToolPhotoFileDto(
        string FileName,
        byte[] Content);
    
}
