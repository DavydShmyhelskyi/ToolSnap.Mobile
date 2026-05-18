namespace ToolSnap.Mobile.Dtos;

public enum MapMarkerKind
{
    User,
    Location,
    Tool
}

public record MapMarkerDto(
    string Id,
    MapMarkerKind Kind,
    double Latitude,
    double Longitude,
    string Title,
    string Subtitle,
    string? Icon = null
);

public enum ToolAvailabilityFilter
{
    All,
    Available,
    NotAvailable
}