using System;
using System.Collections.Generic;
using System.Text;

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
}
