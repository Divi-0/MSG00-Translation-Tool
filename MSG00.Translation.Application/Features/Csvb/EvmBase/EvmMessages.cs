using System.Text;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase;

public enum EvmMessageKind
{
    /// <summary>explanation box (tutorials), word 1 = 2 or 3: 3 lines of 38 bytes</summary>
    Box,

    /// <summary>short message during battle, word 1 = 4 (e.g. 攻撃目標の破壊を確認)</summary>
    Battle,

    Other,
}

/// <param name="Index">row in EV_SEQ, stable key for import</param>
/// <param name="TextOffset">offset in the text table (slot word 2)</param>
public sealed record EvmMessage(int Index, Pointer Pointer, int TextOffset, EvmMessageKind Kind)
{
    public int? MaxBytesPerLine => Kind == EvmMessageKind.Box ? 38 : null;

    public int? MaxLines => Kind == EvmMessageKind.Box ? 3 : null;
}

/// <summary>
/// EV_SEQ commands of type 0x67 show a message: slot words {0x67, display kind, text offset}.
/// Commands of type 0x03 hold labels (internal names like ミサイル出現１) that must not change. See docs/evm/README.md.
/// </summary>
public static class EvmMessages
{
    public const int LabelCommand = 0x03;

    private static readonly Encoding ShiftJis = Encoding.GetEncoding("Shift-JIS");

    public static IReadOnlyList<EvmMessage> Find(EvmBaseCsvb evmBaseCsvb) =>
        evmBaseCsvb.PointerAndValue
            .Select((pointer, index) => (pointer, index))
            .Where(x => x.pointer.Type == (int)EvmBasePointerType.Text && x.pointer.Offset is { Value.Length: >= 12 })
            .Select(x => new EvmMessage(x.index, x.pointer, BitConverter.ToInt32(x.pointer.Offset!.Value, 8), Kind(BitConverter.ToInt32(x.pointer.Offset.Value, 4))))
            .ToList();

    /// <summary>text offsets used by labels (command 0x03, slot word 1)</summary>
    public static IReadOnlySet<int> LabelTextOffsets(EvmBaseCsvb evmBaseCsvb) =>
        evmBaseCsvb.PointerAndValue
            .Where(x => x.Type == LabelCommand && x.Offset is { Value.Length: >= 8 })
            .Select(x => BitConverter.ToInt32(x.Offset!.Value, 4))
            .ToHashSet();

    /// <summary>the text as it is stored in the original file</summary>
    public static string OriginalText(EvmBaseCsvb evmBaseCsvb, int textOffset)
    {
        var start = evmBaseCsvb.CsvbHeader.OffsetToEndOfFullHeader + textOffset;
        var end = Array.IndexOf(evmBaseCsvb.Bytes, (byte)0, start);
        return ShiftJis.GetString(evmBaseCsvb.Bytes, start, end - start);
    }

    private static EvmMessageKind Kind(int displayKind) => displayKind switch
    {
        2 or 3 => EvmMessageKind.Box,
        4 => EvmMessageKind.Battle,
        _ => EvmMessageKind.Other,
    };
}