using Microsoft.Extensions.Logging;
using SceneGate.Ekona.Containers.Rom;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace MSG00.Translation.Application.Features.Rom;

/// <param name="Path">path inside the ROM file system, e.g. "2D/Prologue.csvb", or <see cref="RomPaths.Arm9"/></param>
public sealed record RomFile(string Path, string Name, string Category, long Size);

public static class RomPaths
{
    /// <summary>the main program; holds texts like the in-game menu (see Features/Arm9)</summary>
    public const string Arm9 = "system/arm9";
}

public interface IRomService
{
    bool IsOpen { get; }

    string? FileName { get; }

    bool HasChanges { get; }

    IReadOnlyList<RomFile> CsvbFiles { get; }

    event EventHandler? Changed;

    Task OpenAsync(Stream romStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>returns a seekable copy of a data file or <see cref="RomPaths.Arm9"/>, including changes applied with <see cref="ReplaceFile"/></summary>
    Stream OpenFile(string path);

    /// <summary>keeps the new content in memory; the opened ROM is never modified</summary>
    void ReplaceFile(string path, byte[] data);

    /// <summary>writes a new ROM with all replaced files</summary>
    Task SaveAsAsync(Stream output, CancellationToken cancellationToken = default);
}

internal sealed class RomService : IRomService, IDisposable
{
    private const string RootName = "rom";

    /// <summary>csvb texts, cut-ins (.fcmd, a csvb) and graphics archives (.narc)</summary>
    private static readonly string[] EditableExtensions = [".csvb", ".fcmd", ".narc"];

    private readonly ILogger<RomService> _logger;
    private byte[] _romBytes = Array.Empty<byte>();
    private Node? _rom;
    private readonly Dictionary<string, byte[]> _replacements = new();

    public bool IsOpen => _rom != null;

    public string? FileName { get; private set; }

    public bool HasChanges => _replacements.Count > 0;

    public IReadOnlyList<RomFile> CsvbFiles { get; private set; } = Array.Empty<RomFile>();

    public event EventHandler? Changed;

    public RomService(ILogger<RomService> logger)
    {
        _logger = logger;
    }

    public async Task OpenAsync(Stream romStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await romStream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        var romBytes = memory.ToArray();

        // parse before replacing the current ROM, so a failed open keeps the previous one
        var rom = Parse(romBytes);

        _rom?.Dispose();
        _rom = rom;
        _romBytes = romBytes;
        _replacements.Clear();
        FileName = fileName;
        CsvbFiles = Navigator.IterateNodes(DataNode(rom), NavigationMode.DepthFirst)
            .Where(node => !node.IsContainer && EditableExtensions.Any(x => node.Name.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
            .Select(node =>
            {
                var path = RelativePath(node);
                return new RomFile(path, node.Name, CsvbCategories.Of(path), node.Stream!.Length);
            })
            .Where(file => CsvbCategories.HasText(file.Path))
            .ToList();

        _logger.LogInformation("Opened ROM {FileName} ({Size} bytes, {CsvbCount} csvb files)", fileName, romBytes.Length, CsvbFiles.Count);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Stream OpenFile(string path)
    {
        if (_replacements.TryGetValue(path, out var replaced))
        {
            return new MemoryStream(replaced, writable: false);
        }

        var node = FindFile(_rom ?? throw new InvalidOperationException("No ROM is open"), path);
        var copy = new MemoryStream();
        node.Stream!.WriteTo(copy);
        copy.Position = 0;
        return copy;
    }

    public void ReplaceFile(string path, byte[] data)
    {
        FindFile(_rom ?? throw new InvalidOperationException("No ROM is open"), path);
        _replacements[path] = data;
        _logger.LogInformation("Replaced {Path} in memory ({Size} bytes, {ChangedFiles} changed files)", path, data.Length, _replacements.Count);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAsAsync(Stream output, CancellationToken cancellationToken = default)
    {
        if (_rom == null)
        {
            throw new InvalidOperationException("No ROM is open");
        }

        // build from the original bytes so the opened ROM tree stays untouched
        using var rom = Parse(_romBytes);
        foreach (var (path, data) in _replacements)
        {
            FindFile(rom, path).ChangeFormat(new BinaryFormat(DataStreamFactory.FromArray(data)));
        }

        // the arm9 of this game is not compressed; without this Ekona marks it as compressed and the ROM does not boot
        rom.TransformWith(new NitroRom2Binary(new NitroRom2BinaryParams { DecompressedProgram = true }));

        using var memory = new MemoryStream();
        rom.Stream!.WriteTo(memory);
        memory.Position = 0;
        await memory.CopyToAsync(output, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Saved new ROM ({Size} bytes) with {ChangedFiles} changed files: {Paths}", memory.Length, _replacements.Count, _replacements.Keys);
    }

    public void Dispose() => _rom?.Dispose();

    private static Node Parse(byte[] romBytes)
    {
        var rom = NodeFactory.FromArray(RootName, romBytes);
        rom.TransformWith(new Binary2NitroRom());
        return rom;
    }

    private static Node DataNode(Node rom) => rom.Children["data"] ?? throw new InvalidDataException("ROM has no data folder");

    private static Node FindFile(Node rom, string path)
    {
        var nodePath = path == RomPaths.Arm9 ? $"/{RootName}/{path}" : $"/{RootName}/data/{path}";
        return Navigator.SearchNode(rom, nodePath) ?? throw new FileNotFoundException($"{path} not found in ROM");
    }

    private static string RelativePath(Node node) => node.Path[$"/{RootName}/data/".Length..];
}