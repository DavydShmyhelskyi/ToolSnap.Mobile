using System;
using System.Collections.Generic;
using System.Text;

namespace ToolSnap.Mobile.Dtos
{
    public record ToolAssignmentDto(
        Guid Id,
        Guid TakenDetectedToolId,
        Guid? ReturnedDetectedToolId,
        Guid ToolId,
        Guid UserId,
        Guid TakenLocationId,
        Guid? ReturnedLocationId,
        DateTime TakenAt,
        DateTime? ReturnedAt);
}
