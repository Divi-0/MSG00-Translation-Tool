using Mediator;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase.Write
{
    /// <summary>
    /// Writes changed messages with <see cref="SlotTextWriter"/> (in place or appended, existing data never moves);
    /// label texts (command 0x03) are never overwritten.
    /// </summary>
    public class WriteEvmBaseHandler : IRequestHandler<WriteEvmBase, byte[]>
    {
        private const int TextOffsetInSlot = 8;

        public ValueTask<byte[]> Handle(WriteEvmBase request, CancellationToken cancellationToken)
        {
            var evm = request.EvmBaseCsvb;
            var texts = EvmMessages.Find(evm)
                .Select(x => new SlotText(x.Pointer.Value, TextOffsetInSlot, x.TextOffset, EvmMessages.OriginalText(evm, x.TextOffset), x.Pointer.Offset!.Text))
                .ToList();

            var bytes = SlotTextWriter.Write(evm.Bytes, evm.CsvbHeader.OffsetToEndOfFullHeader, texts, EvmMessages.LabelTextOffsets(evm));
            return ValueTask.FromResult(bytes);
        }
    }
}