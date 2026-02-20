using System;
using System.Collections.Generic;
using System.Text;

namespace ToolSnap.Mobile.Dtos
{
    public record DetectedToolDto(
        Guid Id,
        Guid PhotoSessionId,
        Guid ToolTypeId,
        Guid? BrandId,
        Guid? ModelId,
        string? SerialNumber,
        float Confidence,
        bool RedFlagged);
}
