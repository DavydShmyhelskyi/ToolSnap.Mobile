namespace ToolSnap.Mobile.Models;

public class TransferListItem
{
    public Guid TransferId { get; init; }
    public Guid ToolId { get; init; }
    public string ToolLabel { get; init; } = "";
    public string OtherUserName { get; init; } = "";
    public string Status { get; init; } = "";
    public string InitiatedAtText { get; init; } = "";
    public bool CanAccept { get; init; }
    public bool CanReject { get; init; }
    public bool CanCancel { get; init; }
    public Command? AcceptCommand { get; set; }
    public Command? RejectCommand { get; set; }
    public Command? CancelCommand { get; set; }
}
