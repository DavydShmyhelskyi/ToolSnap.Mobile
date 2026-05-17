namespace ToolSnap.Mobile.Dtos;

public record ToolTransferDto(
    Guid Id,
    Guid ToolId,
    Guid ToolAssignmentId,
    Guid FromUserId,
    Guid ToUserId,
    string Status,
    DateTime InitiatedAt,
    DateTime? RespondedAt);

public record InitiateToolTransferDto(Guid FromUserId, Guid ToUserId, Guid ToolId);

public record RespondToToolTransferDto(Guid ResponderUserId);

public record CancelToolTransferDto(Guid InitiatorUserId);
