using Mediator;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase.Write
{
    /// <summary>
    /// Returns the file with the changed message texts (<see cref="Offset.Text"/> of the 0x67 commands).
    /// </summary>
    public sealed record WriteEvmBase(EvmBaseCsvb EvmBaseCsvb) : IRequest<byte[]>
    {
    }
}