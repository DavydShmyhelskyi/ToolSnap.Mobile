using System;
using System.Collections.Generic;
using System.Text;

namespace ToolSnap.Mobile.Dtos
{
    public record ToolTypeDto(
        Guid Id,
        string Title);

    public record BrandDto(
        Guid Id,
        string Title);

    public record ModelDto(
        Guid Id,
        string Title);
}
