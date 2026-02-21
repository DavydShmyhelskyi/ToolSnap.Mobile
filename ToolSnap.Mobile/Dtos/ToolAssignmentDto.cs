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

    public record CreateToolAssignmentsBatchItemDto(
        Guid TakenDetectedToolId,
        Guid ToolId,
        Guid UserId,
        Guid LocationId);

    public record CreateToolAssignmentsBatchDto(
        List<CreateToolAssignmentsBatchItemDto> Items);
}
