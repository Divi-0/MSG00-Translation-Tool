using System.Text;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MSG00.Translation.Application.Features.Csvb.EvmBase.Write;
using MSG00.Translation.Application.Features.Csvb.Read;
using MSG00.Translation.Application.Features.Csvb.EvmBase;

namespace MSG00.Translation.Application.Tests.Features.Csvb.EvmBase;

/// <summary>
/// Builds a small mission script like Ev/ETC/EVM_ETC001.csvb: EVM_BASE and EV_SEQ tables, column types, text table,
/// extra section and the data area with the command slots.
/// </summary>
internal static class EvmFileBuilder
{
    public const string Label = "ミサイル出現１";
    public const string Box = "これよりガンダムの操作説明を行います。";
    public const string Battle = "攻撃目標の破壊を確認";

    /// <summary>EV_SEQ commands (index in PointerAndValue, the EVM_BASE row is index 0)</summary>
    public const int LabelIndex = 1;
    public const int BoxIndex = 2;
    public const int BattleIndex = 3;
    public const int SharedBoxIndex = 4;
    public const int OtherIndex = 5;

    private static readonly Encoding ShiftJis = Encoding.GetEncoding("Shift-JIS");

    private static readonly IMediator Mediator = new ServiceCollection().AddApplicationServices()
        .BuildServiceProvider().GetRequiredService<IMediator>();

    public static byte[] Build()
    {
        var texts = new List<byte> { 0, 0, 0, 0 };
        int Text(string text)
        {
            var offset = texts.Count;
            var bytes = ShiftJis.GetBytes(text);
            texts.AddRange(bytes);
            texts.AddRange(new byte[4 - bytes.Length % 4]);
            return offset;
        }

        var id = Text("PL_002");
        var label = Text(Label);
        var box = Text(Box);
        var battle = Text(Battle);

        // slot words per command; the first row belongs to the EVM_BASE table
        var commands = new (int Type, int[] Words)[]
        {
            (0x0C, [1, 2, 3]),
            (0x03, [0x03, label]),
            (0x67, [0x67, 3, box]),
            (0x67, [0x67, 4, battle]),
            (0x67, [0x67, 3, box]),
            (0x0D, [id, 6, 7]),
        };

        var file = new List<byte>();
        file.AddRange("CSVBv4.3"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(2));
        file.AddRange(new byte[20]);

        var rowStart = 0x70;
        Descriptor(file, "EVM_BASE", 1, 0, rowStart);
        Descriptor(file, "EV_SEQ", commands.Length - 1, 8, rowStart + 8);

        var slot = 0;
        foreach (var (type, words) in commands)
        {
            file.AddRange(BitConverter.GetBytes(slot));
            file.AddRange(BitConverter.GetBytes(type));
            slot += words.Length * 4;
        }

        var endOfPointerTable = file.Count;
        foreach (var _ in Enumerable.Range(0, 4))
        {
            file.AddRange(BitConverter.GetBytes(2));
        }

        var endOfFullHeader = file.Count;
        file.AddRange(texts);
        var endOfTextTable = file.Count;
        file.AddRange(new byte[] { 0, 0, 0, 0, 2, 2, 2, 0 });
        var endOfExtraTable = file.Count;
        foreach (var word in commands.SelectMany(x => x.Words))
        {
            file.AddRange(BitConverter.GetBytes(word));
        }

        var result = file.ToArray();
        BitConverter.GetBytes(endOfPointerTable).CopyTo(result, 0x0C);
        BitConverter.GetBytes(endOfFullHeader).CopyTo(result, 0x10);
        BitConverter.GetBytes(endOfTextTable).CopyTo(result, 0x14);
        BitConverter.GetBytes(endOfExtraTable).CopyTo(result, 0x18);
        return result;
    }

    public static async Task<EvmBaseCsvb> ReadAsync(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return (EvmBaseCsvb)await Mediator.Send(new ReadCsvbFile(stream), TestContext.Current.CancellationToken);
    }

    public static async Task<byte[]> WriteAsync(EvmBaseCsvb evmBaseCsvb) =>
        await Mediator.Send(new WriteEvmBase(evmBaseCsvb), TestContext.Current.CancellationToken);

    /// <summary>40-byte table descriptor: name, flag, rows, row size, columns, column type index, start</summary>
    private static void Descriptor(List<byte> file, string name, int rows, int columnTypeIndex, int start)
    {
        var nameBytes = new byte[16];
        Encoding.ASCII.GetBytes(name).CopyTo(nameBytes, 0);
        file.AddRange(nameBytes);
        foreach (var value in new[] { 1, rows, 8, 2, columnTypeIndex, start })
        {
            file.AddRange(BitConverter.GetBytes(value));
        }
    }
}