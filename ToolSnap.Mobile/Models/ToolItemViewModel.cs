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

    public Color CardColor => IsOverdue
        ? Color.FromArgb("#FFEBEE")
        : IsCloseToDue
            ? Color.FromArgb("#FFF3E0")
            : Colors.White;

    public Brush CardStroke => IsOverdue
        ? new SolidColorBrush(Color.FromArgb("#EF9A9A"))
        : IsCloseToDue
            ? new SolidColorBrush(Color.FromArgb("#FFCC80"))
            : new SolidColorBrush(Color.FromArgb("#E0E0E0"));

    public Color StatusLabelColor => IsOverdue
        ? Color.FromArgb("#E53935")
        : Color.FromArgb("#FB8C00");

    public Command? TransferCommand { get; set; }
}
