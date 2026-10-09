using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace MSG00.Translation.UI.Services;

/// <summary>
/// Export and import of translation texts as JSON (readable Japanese, indented), shared by the editors.
/// </summary>
public static class JsonTextFile
{
    private static readonly FilePickerFileType JsonFileType = new("json") { Patterns = new List<string> { "*.json" } };

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    /// <returns>false when the user cancelled</returns>
    public static async Task<bool> ExportAsync<T>(IStorageProvider storageProvider, string suggestedFileName, IReadOnlyList<T> texts)
    {
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export texts",
            DefaultExtension = "json",
            ShowOverwritePrompt = true,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = new List<FilePickerFileType> { JsonFileType },
        });

        if (file == null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await JsonSerializer.SerializeAsync(stream, texts, Options);
        return true;
    }

    /// <returns>null when the user cancelled</returns>
    public static async Task<T[]?> ImportAsync<T>(IStorageProvider storageProvider)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Import texts",
            FileTypeFilter = new List<FilePickerFileType> { JsonFileType },
        });

        if (!files.Any())
        {
            return null;
        }

        await using var stream = await files[0].OpenReadAsync();
        return await JsonSerializer.DeserializeAsync<T[]>(stream, Options) ?? [];
    }
}