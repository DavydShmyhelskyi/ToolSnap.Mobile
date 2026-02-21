using System.Collections.Generic;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public class TakeFlowStateService
{
    public PhotoSessionDto? CurrentSession { get; set; }
    public List<GeminiDetection>? CurrentDetections { get; set; }

    public void Clear()
    {
        CurrentSession = null;
        CurrentDetections = null;
    }
}
