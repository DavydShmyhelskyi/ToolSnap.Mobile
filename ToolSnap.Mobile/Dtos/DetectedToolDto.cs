using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ToolSnap.Mobile.Dtos
{
    public record DetectedToolDto(
        Guid Id,
        Guid PhotoSessionId,
        Guid ToolTypeId,
        Guid? BrandId,
        Guid? ModelId,
        string? SerialNumber,
        float Confidence,
        bool RedFlagged);
    public class DetectToolsApiResponse
    {
        [JsonPropertyName("detection")]
        public string Detection { get; set; } = string.Empty;
    }
    public class GeminiDetectionEnvelope
    {
        [JsonPropertyName("detections")]
        public List<GeminiDetection> Detections { get; set; } = new();
    }
    public class GeminiDetection
    {
        [JsonPropertyName("toolType")]
        public string ToolType { get; set; } = string.Empty;

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("redFlagged")]
        public bool RedFlagged { get; set; }
    }

    public record CreateDetectedToolItemDto(
        Guid PhotoSessionId,
        Guid ToolTypeId,
        Guid? BrandId,
        Guid? ModelId,
        string? SerialNumber,
        float Confidence,
        bool RedFlagged);

    public record CreateDetectedToolsBatchDto(
        List<CreateDetectedToolItemDto> Items);

    
}
