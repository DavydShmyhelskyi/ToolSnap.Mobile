namespace ToolSnap.Mobile.Models;

public class ToolItemViewModel
{
    public Guid Id { get; init; }
    public string? SerialNumber { get; init; }
    public ImageSource? Photo { get; init; }
    public string ToolTypeTitle { get; init; } = "";
    public decimal Price { get; init; }
    public string PriceText => Price > 0 ? $"{Price:F2} UAH" : "—";

    public DateTime? DueAt { get; init; }
    public bool IsOverdue { get; init; }

    public bool IsCloseToDue =>
        !IsOverdue
        && DueAt.HasValue
        && DueAt.Value > DateTime.UtcNow
        && (DueAt.Value - DateTime.UtcNow).TotalHours < 1;

    public bool HasDeadline => DueAt.HasValue;

    public string DueAtText => DueAt.HasValue
        ? DueAt.Value.ToLocalTime().ToString("dd MMM HH:mm")
        : string.Empty;

    public bool HasStatus => IsOverdue || IsCloseToDue;
    public string StatusLabel => IsOverdue ? "OVERDUE" : "DUE SOON";

    private static bool IsDark =>
        Application.Current?.RequestedTheme == AppTheme.Dark;

    public Color CardColor => IsOverdue
        ? Color.FromArgb(IsDark ? "#3B1A1A" : "#FFEBEE")
        : IsCloseToDue
            ? Color.FromArgb(IsDark ? "#2D2218" : "#FFF3E0")
            : Color.FromArgb(IsDark ? "#2C2C2E" : "#FFFFFF");

    public Brush CardStroke => IsOverdue
        ? new SolidColorBrush(Color.FromArgb(IsDark ? "#C62828" : "#EF9A9A"))
        : IsCloseToDue
            ? new SolidColorBrush(Color.FromArgb(IsDark ? "#BF6005" : "#FFCC80"))
            : new SolidColorBrush(Color.FromArgb(IsDark ? "#3A3A3C" : "#E0E0E0"));

    public Color StatusLabelColor => IsOverdue
        ? Color.FromArgb("#E53935")
        : Color.FromArgb("#FB8C00");

    public Command? TransferCommand { get; set; }
}
