using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Graphics;
using MSG00.Translation.Application.Features.Rom;
using MSG00.Translation.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ICommand = System.Windows.Input.ICommand;

namespace MSG00.Translation.UI.ViewModels
{
    /// <summary>
    /// Graphics of a NARC archive like 2D/Briefing.narc: backgrounds (NCLR + NCGR + NSCR) and sprite cells (NCER).
    /// Export as PNG, edit in an image editor, import (nearest colors of the original palette). See docs/graphics/README.md.
    /// </summary>
    public class GraphicsViewModel : ViewModelBase
    {
        private readonly IWriter _writer;
        private readonly IRomService _romService;
        private readonly IStorageProvider _storageProvider;
        private readonly string _romPath;
        private byte[] _narc;

        public GraphicsViewModel(byte[] narc, IReader reader, IWriter writer, IRomService romService, IStorageProvider storageProvider, string romPath)
        {
            _narc = narc;
            _writer = writer;
            _romService = romService;
            _storageProvider = storageProvider;
            _romPath = romPath;

            var items = reader.ReadBackgrounds(narc).Select(x => new GraphicsItem(x)).ToList();
            foreach (var sprites in reader.ReadSprites(narc))
            {
                // animation frames often repeat a cell exactly; show each picture once, leave out empty cells
                var seen = new HashSet<string>();
                foreach (var cell in sprites.Cells)
                {
                    var pixels = SpriteImage.Render(sprites, cell).Pixels;
                    if (pixels.Where((_, i) => i % 4 == 3).Any(a => a != 0) && seen.Add(Convert.ToHexString(MD5.HashData(pixels))))
                    {
                        items.Add(new GraphicsItem(sprites, cell));
                    }
                }
            }

            Items = new ObservableCollection<GraphicsItem>(items);
        }

        /// <summary>selects the first item; creates the preview bitmap, so it is called on the UI thread after loading</summary>
        public void SelectFirst() => SelectedItem = Items.FirstOrDefault();

        public ObservableCollection<GraphicsItem> Items { get; }

        public bool HasNoItems => Items.Count == 0;

        private GraphicsItem? _selectedItem;
        public GraphicsItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    UpdatePreview();
                }
            }
        }

        private Bitmap? _preview;
        public Bitmap? Preview
        {
            get => _preview;
            private set => SetProperty(ref _preview, value);
        }

        public string Info => SelectedItem?.Info ?? string.Empty;

        public ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);

        public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

        public ICommand SaveCommand => new AsyncRelayCommand(ApplyAsync);

        private void UpdatePreview()
        {
            Preview = SelectedItem is { } item ? PngImageFile.ToBitmap(item.Render()) : null;
            RaisePropertyChanged(nameof(Info));
        }

        private async Task ExportAsync()
        {
            if (SelectedItem is not { } item)
            {
                return;
            }

            await PngImageFile.ExportAsync(_storageProvider, $"{Path.GetFileNameWithoutExtension(_romPath)}__{item.FileLabel}.png", item.Render());
        }

        /// <summary>
        /// Replaces the selected background or sprite cell with the image; colors are matched to the palette.
        /// </summary>
        private async Task ImportAsync()
        {
            if (SelectedItem is not { } item || await PngImageFile.ImportAsync(_storageProvider) is not { } image)
            {
                return;
            }

            try
            {
                _narc = item.Import(image, _narc, _writer);
                UpdatePreview();
            }
            catch (InvalidOperationException e)
            {
                await ShowMessageAsync("Import", e.Message);
            }
        }

        /// <summary>
        /// Writes the archive into the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        private async Task ApplyAsync()
        {
            _romService.ReplaceFile(_romPath, _narc);
            await ShowMessageAsync("Applied", $"{_romPath} was updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.");
        }

        private static async Task ShowMessageAsync(string title, string message)
        {
            await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = title,
                ContentMessage = message,
                ButtonDefinitions = ButtonEnum.Ok,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }).ShowAsync();
        }
    }

    /// <summary>a background or one sprite cell of the archive</summary>
    public sealed class GraphicsItem
    {
        private readonly Background? _background;
        private readonly SpriteSet? _sprites;
        private readonly SpriteCell? _cell;

        public GraphicsItem(Background background)
        {
            _background = background;
            Label = $"{background.Name.Split('/').Last()} (background)";
            FileLabel = background.Name.Split('/').Last();
        }

        public GraphicsItem(SpriteSet sprites, SpriteCell cell)
        {
            _sprites = sprites;
            _cell = cell;
            Label = $"{sprites.Name.Split('/').Last()} #{cell.Index}";
            FileLabel = $"{sprites.Name.Split('/').Last()}#{cell.Index}";
        }

        public string Label { get; }

        /// <summary>used in export file names: archive__name.png or archive__name#cell.png</summary>
        public string FileLabel { get; }

        public string Info => _background is { } background
            ? $"Background {background.Width} × {background.Height} px (the DS shows the top 192 rows), {background.BitsPerPixel}-bit colors, room for {background.TileCount} tiles"
            : $"Sprite cell {_cell!.Width} × {_cell.Height} px, {_cell.Oams.Count} parts. Other cells using the same tiles (animation frames, selected states) change too.";

        public RgbaImage Render() => _background != null ? BackgroundImage.Render(_background) : SpriteImage.Render(_sprites!, _cell!);

        /// <returns>the archive with the imported image</returns>
        public byte[] Import(RgbaImage image, byte[] narc, IWriter writer)
        {
            if (_background != null)
            {
                var (tiles, map) = BackgroundImage.Import(_background, image);
                _background.Tiles = tiles;
                _background.Map = map;
                return writer.Write(narc, _background);
            }

            _sprites!.Tiles = SpriteImage.Import(_sprites, _cell!, image);
            return writer.Write(narc, _sprites);
        }

        public override string ToString() => Label;
    }
}