using ToolSnap.Mobile.Models;

namespace ToolSnap.Mobile.Services;

public class TransferFlowStateService
{
    public ToolItemViewModel? SelectedTool { get; set; }

    public void Clear() => SelectedTool = null;
}
