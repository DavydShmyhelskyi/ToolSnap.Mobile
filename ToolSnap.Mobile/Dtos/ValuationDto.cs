namespace ToolSnap.Mobile.Dtos;

public record WorkerOnHandsStatsDto(Guid UserId, decimal TotalValue, int ToolCount);
public record InventoryStatsDto(decimal TotalValue, int ToolCount);
