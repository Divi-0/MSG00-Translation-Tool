using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Font;
using MSG00.Translation.Application.Features.Graphics;
using MSG00.Translation.UI.Models;
using MSG00.Translation.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ICommand = System.Windows.Input.ICommand;

namespace MSG00.Translation.UI.ViewModels
{
    /// <summary>
    /// Extra characters of the game font (ä, é, ñ, …): add a preset or own characters, draw their pixels, preview a
    /// text and "Apply to ROM". Nothing is written before that. See docs/font/README.md.
    /// </summary>
    public class FontViewModel : ViewModelBase
    {
        private const string DefaultPreviewText = "Grüße! ¡Señor! ¿Qué? Ça va? ÄÖÜ äöü ß";

        private readonly IFontService _fontService;
        private readonly IStorageProvider _storageProvider;

        /// <summary>the characters of the ROM, to tell which ones are not applied yet</summary>
        private Dictionary<byte, char> _applied;

        public FontViewModel(IFontService fontService, IStorageProvider storageProvider)
        {
            _fontService = fontService;
            _storageProvider = storageProvider;

            var characters = fontService.ReadCharacters();
            _applied = characters.ToDictionary(x => x.Slot, x => x.Character);
            Characters = new ObservableCollection<FontCharacterRow>(characters.Select(CreateRow));
            Pixels = new ObservableCollection<GlyphPixel>();
            SelectedPreset = Presets[0];
        }

        public IReadOnlyList<CharacterPreset> Presets => CharacterPresets.All;

        private CharacterPreset? _selectedPreset;
        public CharacterPreset? SelectedPreset
        {
            get => _selectedPreset;
            set => SetProperty(ref _selectedPreset, value);
        }

        public ObservableCollection<FontCharacterRow> Characters { get; }

        private FontCharacterRow? _selectedCharacter;
        public FontCharacterRow? SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                if (SetProperty(ref _selectedCharacter, value))
                {
                    UpdatePixels();
                    RaisePropertyChanged(nameof(HasSelection));
                    RaisePropertyChanged(nameof(SelectionInfo));
                }
            }
        }

        public bool HasSelection => SelectedCharacter != null;

        public string SelectionInfo => SelectedCharacter is { } row
            ? $"{row.Character}  U+{(int)row.Character:X4}, byte 0x{row.Slot:X2} in the texts. Rows 0–9 capitals, 3–9 lowercase, 10 below the line; column 0 is the gap to the previous character."
            : "Select a character to draw it.";

        /// <summary>6 × 11 pixels of the selected character, row by row</summary>
        public ObservableCollection<GlyphPixel> Pixels { get; }

        private string _newCharacters = string.Empty;
        public string NewCharacters
        {
            get => _newCharacters;
            set => SetProperty(ref _newCharacters, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public string SlotInfo
        {
            get
            {
                var current = Characters.ToDictionary(x => x.Slot, x => x.Character);
                var notApplied = !current.OrderBy(x => x.Key).SequenceEqual(_applied.OrderBy(x => x.Key)) || Characters.Any(x => x.IsChanged);
                return $"{Characters.Count} of {CharacterTable.Slots.Count} slots used"
                    + (notApplied ? " – changes not applied to the ROM yet" : _applied.Count > 0 ? " – applied" : string.Empty);
            }
        }

        private string _previewText = DefaultPreviewText;
        public string PreviewText
        {
            get => _previewText;
            set
            {
                if (SetProperty(ref _previewText, value))
                {
                    RaisePropertyChanged(nameof(Preview));
                }
            }
        }

        /// <summary>the preview text as the game draws it, with the characters of this editor (applied or not)</summary>
        public Bitmap? Preview
        {
            get
            {
                try
                {
                    return PngImageFile.ToBitmap(_fontService.Render(PreviewText, CurrentCharacters()));
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        public ICommand AddPresetCommand => new RelayCommand(() =>
        {
            if (SelectedPreset != null)
            {
                Add(SelectedPreset.Characters);
            }
        });

        public ICommand AddCharactersCommand => new RelayCommand(() =>
        {
            Add(NewCharacters);
            NewCharacters = string.Empty;
        });

        public ICommand RemoveAllCommand => new RelayCommand(() =>
        {
            Characters.Clear();
            SelectedCharacter = null;
            Message = _applied.Count > 0 ? AppliedSlotNote(_applied.Values) : string.Empty;
            OnCharactersChanged();
        });

        public ICommand RedrawCommand => new RelayCommand(() =>
        {
            if (SelectedCharacter is { } row)
            {
                row.Glyph = _fontService.CreateGlyph(row.Character) ?? new Glyph(GlyphFactory.Width, GlyphFactory.Height);
                UpdatePixels();
                OnCharactersChanged();
            }
        });

        public ICommand ClearCommand => new RelayCommand(() =>
        {
            if (SelectedCharacter is { } row)
            {
                row.Glyph = new Glyph(GlyphFactory.Width, GlyphFactory.Height);
                UpdatePixels();
                OnCharactersChanged();
            }
        });

        public ICommand ExportCommand => new AsyncRelayCommand(() => JsonTextFile.ExportAsync(_storageProvider, "font_characters.json",
            Characters.Select(x => new FontCharacterJson($"{x.Slot:X2}", x.Character.ToString(), x.Glyph.ToRows().ToArray())).ToList()));

        public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

        public ICommand ApplyCommand => new AsyncRelayCommand(ApplyAsync);

        /// <summary>adds the characters to free slots with a first draft of their pixels; skipped ones are listed in <see cref="Message"/></summary>
        private void Add(string text)
        {
            var added = new List<char>();
            var notes = new List<string>();
            var withoutDraft = new List<char>();
            foreach (var character in text.Where(c => !char.IsWhiteSpace(c)).Distinct())
            {
                if (Characters.Any(x => x.Character == character))
                {
                    continue;
                }

                if (CharacterTable.Check(character) is { } reason)
                {
                    notes.Add($"{character}: {reason}");
                    continue;
                }

                if (FreeSlot() is not { } slot)
                {
                    notes.Add($"{character}: all {CharacterTable.Slots.Count} slots are used");
                    continue;
                }

                var glyph = _fontService.CreateGlyph(character);
                if (glyph == null)
                {
                    withoutDraft.Add(character);
                }

                var row = CreateRow(new FontCharacter(slot, character, glyph ?? new Glyph(GlyphFactory.Width, GlyphFactory.Height)));
                Characters.Insert(Characters.TakeWhile(x => x.Slot < slot).Count(), row);
                added.Add(character);
            }

            if (withoutDraft.Count > 0)
            {
                notes.Add($"Draw these by hand, there is no draft for them: {string.Join(" ", withoutDraft)}");
            }

            Message = string.Join("\n", notes.Prepend(added.Count > 0 ? $"Added {added.Count}: {new string(added.ToArray())}" : "Nothing added"));
            SelectedCharacter ??= Characters.FirstOrDefault(x => added.Contains(x.Character));
            OnCharactersChanged();
        }

        private void Remove(FontCharacterRow row)
        {
            Characters.Remove(row);
            if (SelectedCharacter == row)
            {
                SelectedCharacter = null;
            }

            Message = _applied.ContainsKey(row.Slot) ? AppliedSlotNote([row.Character]) : $"Removed {row.Character}";
            OnCharactersChanged();
        }

        private static string AppliedSlotNote(IEnumerable<char> characters) =>
            $"Removed {string.Join(" ", characters)}. Texts already in the ROM that use them show whatever character takes their slot next.";

        private byte? FreeSlot() => CharacterTable.Slots.Where(x => Characters.All(c => c.Slot != x)).Select(x => (byte?)x).FirstOrDefault();

        private FontCharacterRow CreateRow(FontCharacter character) => new(character, Remove);

        private List<FontCharacter> CurrentCharacters() => Characters.Select(x => new FontCharacter(x.Slot, x.Character, x.Glyph)).ToList();

        private void UpdatePixels()
        {
            Pixels.Clear();
            if (SelectedCharacter is not { } row)
            {
                return;
            }

            for (var y = 0; y < row.Glyph.Height; y++)
            {
                for (var x = 0; x < row.Glyph.Width; x++)
                {
                    Pixels.Add(new GlyphPixel(x, y, row.Glyph[x, y], OnPixelChanged));
                }
            }
        }

        private void OnPixelChanged(GlyphPixel pixel)
        {
            if (SelectedCharacter is { } row)
            {
                var glyph = row.Glyph.Clone();
                glyph[pixel.X, pixel.Y] = pixel.IsOn;
                row.Glyph = glyph;
                OnCharactersChanged();
            }
        }

        private void OnCharactersChanged()
        {
            RaisePropertyChanged(nameof(SlotInfo));
            RaisePropertyChanged(nameof(Preview));
        }

        /// <summary>replaces the characters of the editor with the file (slots, characters and pixels)</summary>
        private async Task ImportAsync()
        {
            if (await JsonTextFile.ImportAsync<FontCharacterJson>(_storageProvider) is not { } imported)
            {
                return;
            }

            try
            {
                var characters = imported.Select(x =>
                {
                    var character = x.Character.Length == 1 ? x.Character[0] : throw new FormatException($"\"{x.Character}\" is not one character");
                    var slot = byte.Parse(x.Slot, NumberStyles.HexNumber);
                    return new FontCharacter(slot, character, Glyph.FromRows(x.Glyph, GlyphFactory.Width, GlyphFactory.Height));
                }).ToList();

                // checks slots, duplicates and characters
                _ = new CharacterTable(characters.ToDictionary(x => x.Slot, x => x.Character));

                Characters.Clear();
                foreach (var character in characters.OrderBy(x => x.Slot))
                {
                    Characters.Add(CreateRow(character));
                }

                SelectedCharacter = Characters.FirstOrDefault();
                Message = $"Imported {characters.Count} characters";
                OnCharactersChanged();
            }
            catch (Exception e) when (e is FormatException or ArgumentException or OverflowException)
            {
                await ShowMessageAsync("Import", $"The file was not imported:\n{e.Message}");
            }
        }

        /// <summary>
        /// Writes the glyphs and the character table into every copy of the font; from now on the editors write the
        /// characters as 1 byte. "Save ROM as" writes the new .nds.
        /// </summary>
        private async Task ApplyAsync()
        {
            var characters = CurrentCharacters();
            var empty = characters.Where(x => x.Glyph.IsEmpty).Select(x => x.Character).ToList();
            try
            {
                _fontService.Apply(characters);
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            {
                await ShowMessageAsync("Not applied", e.Message);
                return;
            }

            _applied = characters.ToDictionary(x => x.Slot, x => x.Character);
            foreach (var row in Characters)
            {
                row.MarkApplied();
            }

            OnCharactersChanged();
            await ShowMessageAsync("Applied",
                $"{characters.Count} extra characters were written into the {_fontService.FontFiles.Count} copies of the game font in the opened ROM."
                + (empty.Count > 0 ? $"\nThese have no pixels yet and show as blanks: {string.Join(" ", empty)}" : string.Empty)
                + "\nThe text editors can use them now. Use \"Save ROM as\" to write a new .nds file.");
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

    /// <summary>JSON export of a character: slot as hex byte, the pixels as '#'/'.' rows</summary>
    public sealed record FontCharacterJson(string Slot, string Character, string[] Glyph);

    /// <summary>one pixel of the drawing grid</summary>
    public sealed class GlyphPixel : NotifyBase
    {
        private readonly Action<GlyphPixel> _changed;
        private bool _isOn;

        public GlyphPixel(int x, int y, bool isOn, Action<GlyphPixel> changed)
        {
            X = x;
            Y = y;
            _isOn = isOn;
            _changed = changed;
        }

        public int X { get; }

        public int Y { get; }

        public bool IsOn
        {
            get => _isOn;
            set
            {
                if (SetProperty(ref _isOn, value))
                {
                    _changed(this);
                }
            }
        }

        /// <summary>column 0 (gap) and the rows outside the capital height are shaded in the grid</summary>
        public bool IsGuide => X == 0 || Y == GlyphFactory.Height - 1;
    }

    public sealed class FontCharacterRow : NotifyBase
    {
        private Glyph _glyph;
        private Glyph _appliedGlyph;
        private Bitmap? _preview;

        public FontCharacterRow(FontCharacter character, Action<FontCharacterRow> remove)
        {
            Slot = character.Slot;
            Character = character.Character;
            _glyph = character.Glyph;
            _appliedGlyph = character.Glyph;
            RemoveCommand = new RelayCommand(() => remove(this));
        }

        public byte Slot { get; }

        public char Character { get; }

        public string Label => $"{Character}   0x{Slot:X2}";

        public Glyph Glyph
        {
            get => _glyph;
            set
            {
                _glyph = value;
                _preview = null;
                RaisePropertyChanged(nameof(Glyph));
                RaisePropertyChanged(nameof(Preview));
                RaisePropertyChanged(nameof(IsEmpty));
            }
        }

        public bool IsEmpty => Glyph.IsEmpty;

        /// <summary>pixels differ from the ROM (or the character was added)</summary>
        public bool IsChanged => !Glyph.SamePixels(_appliedGlyph);

        /// <summary>created on first use from the binding, so on the UI thread</summary>
        public Bitmap Preview => _preview ??= PngImageFile.ToBitmap(Render(Glyph));

        public ICommand RemoveCommand { get; }

        public void MarkApplied() => _appliedGlyph = Glyph;

        private static RgbaImage Render(Glyph glyph)
        {
            var pixels = new byte[glyph.Width * glyph.Height * 4];
            for (var y = 0; y < glyph.Height; y++)
            {
                for (var x = 0; x < glyph.Width; x++)
                {
                    var p = (y * glyph.Width + x) * 4;
                    var value = glyph[x, y] ? (byte)0 : (byte)255;
                    pixels[p] = pixels[p + 1] = pixels[p + 2] = value;
                    pixels[p + 3] = 255;
                }
            }

            return new RgbaImage(glyph.Width, glyph.Height, pixels);
        }
    }
}