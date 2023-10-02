using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Domain.Shared;
using MSG00.Translation.Infrastructure.Domain.StaffRoll;
using MSG00.Translation.Infrastructure.Domain.StaffRoll.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MSG00.Translation.UI.ViewModels
{
    public class StaffRollCsvbViewModel : ViewModelBase
    {
        private readonly IStaffRollService _staffRollService;
        private readonly IStorageProvider _storageProvider;

        public StaffRollCsvbViewModel(IStaffRollService staffRollService, IStorageProvider storageProvider)
        {
            _staffRollService = staffRollService;
            _storageProvider = storageProvider;
        }

        public async Task OpenFile()
        {
            try
            {
                IReadOnlyList<IStorageFile> selectFileList = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Select a conversation file",
                    FileTypeFilter = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("csvb")
                        {
                            Patterns = new List<string>()
                            {
                                "*.csvb"
                            }
                        }
                    }
                });

                if (!selectFileList.Any())
                {
                    return;
                }

                IsReadingFile = true;
                IsFileLoaded = true;

                using (Stream stream = await selectFileList[0].OpenReadAsync().ConfigureAwait(false))
                {
                    EpilogueFile = await _staffRollService.GetStaffRollAsync(await selectFileList[0].OpenReadAsync()).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                //{
                //    Application.Current.MainPage.DisplayAlert("Open File Failed", "The file you tried to open occured an error.", "Ok");
                //});

                IsFileLoaded = false;
            }
            finally
            {
                IsReadingFile = false;
            }
        }

        public async Task Save()
        {
            try
            {
                IsSaving = true;

                //if (!ValidateEntries())
                //{
                //    return;
                //}

                IStorageFile? storageFile = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    DefaultExtension = "csvb",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("csvb")
                        {
                            Patterns = new List<string>
                            {
                                "*.csvb"
                            }
                        }
                    }
                }).ConfigureAwait(false);

                if (storageFile == null)
                {
                    return;
                }

                using (Stream stream = await storageFile.OpenWriteAsync().ConfigureAwait(false))
                {
                    await _staffRollService.SaveStaffRollAsync(stream, EpilogueFile!).ConfigureAwait(false);
                }

                Dispatcher.UIThread.Post(async () =>
                {
                    var dialog = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentTitle = "Save",
                        ContentMessage = "File saved.",
                        ButtonDefinitions = ButtonEnum.Ok,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });

                    await dialog.ShowAsync().ConfigureAwait(false);
                });
            }
            catch (Exception e)
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                //{
                //    Application.Current.MainPage.DisplayAlert("Error", e.ToString(), "Ok");
                //});
            }
            finally
            {
                IsSaving = false;
            }
        }

        private bool ValidateEntries()
        {
            StringBuilder stringBuilder = new StringBuilder();

            //foreach (ConversationTextConversation textConversation in ConversationFile.Conversations)
            //{
            //    foreach (ConversationTextBox textBox in textConversation.TextBoxes)
            //    {
            //        foreach (ConversationTextLine line in textBox.Lines)
            //        {
            //            ValidationResult result = _validator.Validate(line);

            //            if (!result.IsValid)
            //            {
            //                foreach (ValidationFailure failure in result.Errors)
            //                {
            //                    stringBuilder.AppendLine($"{failure.AttemptedValue}: {failure.ErrorMessage}");
            //                }
            //            }
            //        }
            //    }
            //}

            if (stringBuilder.Length == 0)
            {
                return true;
            }

            //MainThread.BeginInvokeOnMainThread(() =>
            //{
            //    Application.Current.MainPage.DisplayAlert("Validation Failed", stringBuilder.ToString(), "Ok");
            //});

            return false;
        }

        private async Task CloseFile()
        {
            var dialog = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = "Close Conversation",
                ContentMessage = "Are you sure, you want to close the conversation file?",
                ButtonDefinitions = ButtonEnum.YesNo,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

            ButtonResult buttonResult = await dialog.ShowAsync().ConfigureAwait(false);

            if (buttonResult == ButtonResult.No)
            {
                return;
            }

            throw new NotImplementedException();
            //EpilogueFile = new StaffRollCsvb
            //{
            //    AfterTextSectionBytes = Array.Empty<byte>(),
            //    CountOfPointersInFile = 0,
            //    FileSizeToTextEnd = 0,
            //    FileSizeWithUnimportantInfo = 0,
            //    FullHeaderSize = 0,
            //    HeaderBytes = Array.Empty<byte>(),
            //    FileOffsetToAreaBetweenPointerAndTextTable = 0,
            //    MapiHeaderBytes = Array.Empty<byte>(),
            //};

            IsFileLoaded = false;
        }

        private void AddLine(StaffRollPointerText? staffRollPointerText)
        {
            ArgumentNullException.ThrowIfNull(staffRollPointerText, nameof(staffRollPointerText));

            staffRollPointerText.TextLines.Add(new CsvbTextLine
            {
                Text = string.Empty,
            });
        }


        private void DeleteLine(StaffRollPointerText? staffRollPointerText)
        {
            ArgumentNullException.ThrowIfNull(staffRollPointerText, nameof(staffRollPointerText));

            if (staffRollPointerText.TextLines.Count == 1)
            {
                return;
            }

            staffRollPointerText.TextLines.RemoveAt(staffRollPointerText.TextLines.Count - 1);
        }

        public ICommand OpenFileCommand => new AsyncRelayCommand(OpenFile);
        public ICommand SaveCommand => new AsyncRelayCommand(Save);
        public ICommand AddLineCommand => new RelayCommand<StaffRollPointerText>(AddLine);
        public ICommand DeleteLineCommand => new RelayCommand<StaffRollPointerText>(DeleteLine);
        public ICommand CloseFileCommand => new AsyncRelayCommand(CloseFile);

        private StaffRollCsvb? _prologueFile;
        public StaffRollCsvb? EpilogueFile
        {
            get => _prologueFile;
            set
            {
                SetProperty(ref _prologueFile, value);
                TextPointers = _prologueFile!.Pointers.Where(x => x.Type == StaffRollPointerType.Text).Cast<StaffRollPointerText>().ToList();
            }
        }

        private List<StaffRollPointerText> _textPointer;
        public List<StaffRollPointerText> TextPointers
        {
            get => _textPointer;
            set => SetProperty(ref _textPointer, value);
        }

        private bool _isReadingFile;
        public bool IsReadingFile
        {
            get => _isReadingFile;
            set => SetProperty(ref _isReadingFile, value);
        }

        private bool _isFileLoaded;
        public bool IsFileLoaded
        {
            get => _isFileLoaded;
            set => SetProperty(ref _isFileLoaded, value);
        }

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }
    }
}
