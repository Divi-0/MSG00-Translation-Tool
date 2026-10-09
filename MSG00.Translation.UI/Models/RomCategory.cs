using System.Collections.Generic;
using MSG00.Translation.Application.Features.Rom;

namespace MSG00.Translation.UI.Models;

public sealed record RomCategory(string Name, IReadOnlyList<RomFile> Files)
{
    public string Header => $"{Name} ({Files.Count})";
}