using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;
using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;
using MSG00.Translation.Application.Features.Csvb.Read;
using ConversationReader = MSG00.Translation.Application.Features.Csvb.Mapi.Conversation.Reader;
using ConversationWriter = MSG00.Translation.Application.Features.Csvb.Mapi.Conversation.Writer;
using PrologueReader = MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue.Reader;
using PrologueWriter = MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue.Writer;
using StaffRollReader = MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll.Reader;
using StaffRollWriter = MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll.Writer;
using MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi;

/// <summary>
/// Reads and writes csvb bytes with the real reader/writer pipeline used by the app.
/// </summary>
internal static class CsvbRoundTrip
{
    public static async Task<MapiCsvb> ReadAsync(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var csvbFile = await new CsvbReader().ReadAsync(stream, TestContext.Current.CancellationToken);
        var mapiService = new MapiService(new PrologueReader(), new ConversationReader(), new StaffRollReader());
        return await mapiService.GetMapiCsvbAsync(csvbFile, stream);
    }

    public static async Task<StaffRollTextCsvb> ReadStaffRollAsync(byte[] bytes) => (await ReadAsync(bytes)).ShouldBeOfType<StaffRollTextCsvb>();

    public static async Task<byte[]> WriteAsync(StaffRollTextCsvb staffRoll)
    {
        using var stream = new MemoryStream();
        await new StaffRollWriter().WriteFileAsync(stream, staffRoll);
        return stream.ToArray();
    }

    public static async Task<ConversationCsvb> ReadConversationAsync(byte[] bytes) => (await ReadAsync(bytes)).ShouldBeOfType<ConversationCsvb>();

    public static async Task<PrologueCsvb> ReadPrologueAsync(byte[] bytes) => (await ReadAsync(bytes)).ShouldBeOfType<PrologueCsvb>();

    public static async Task<byte[]> WriteAsync(ConversationCsvb conversationCsvb)
    {
        using var stream = new MemoryStream();
        await new ConversationWriter().WriteFileAsync(stream, conversationCsvb);
        return stream.ToArray();
    }

    public static async Task<byte[]> WriteAsync(PrologueCsvb prologueCsvb)
    {
        using var stream = new MemoryStream();
        await new PrologueWriter().WriteFileAsync(stream, prologueCsvb);
        return stream.ToArray();
    }
}