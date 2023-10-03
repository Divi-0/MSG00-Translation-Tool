using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Domain.Conversation;
using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;
using MSG00.Translation.Infrastructure.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MSG00.Translation.UI.ViewModels
{
    public class EvmCsvbViewModel : ViewModelBase
    {
        private readonly IEvmService _evmService;
        private readonly IStorageProvider _storageProvider;

        public EvmCsvbViewModel(IEvmService evmService, IStorageProvider storageProvider)
        {
            _evmService = evmService;
            _storageProvider = storageProvider;
        }

        public async Task OpenFile()
        {
            try
            {
                IReadOnlyList<IStorageFile> selectFileList = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Select a evm file",
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

                EvmFile = null;

                IsReadingFile = true;
                IsFileLoaded = true;

                using (Stream stream = await selectFileList[0].OpenReadAsync().ConfigureAwait(false))
                {
                    EvmFile = await _evmService.GetEvmAsync(await selectFileList[0].OpenReadAsync()).ConfigureAwait(false);
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
                   // await _conversationService.SaveFile(stream, EvmFile!).ConfigureAwait(false);
                }

                Dispatcher.UIThread.Post(async () =>
                {
                    var dialog = MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
                    {
                        ContentTitle = "Save",
                        ContentMessage = "File saved.",
                        ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.Ok,
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

        private async Task AddNewLineToTextBox(ConversationPointerText? conversationPointerText)
        {
            ArgumentNullException.ThrowIfNull(conversationPointerText, nameof(conversationPointerText));

            if (conversationPointerText.Lines.Count == 3)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    var dialog = MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
                    {
                        ContentTitle = "Maximum lines",
                        ContentMessage = "A Textbox can not have more than 3 lines",
                        ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.Ok,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });

                    await dialog.ShowAsync();
                });

                return;
            }

            conversationPointerText.Lines.Add(new CsvbTextLine
            {
                Text = string.Empty
            });
        }

        private void AddNewTextBox(ConversationPointerCharacter? conversationPointer)
        {
            ArgumentNullException.ThrowIfNull(conversationPointer, nameof(conversationPointer));

            conversationPointer.TextBoxes.Add(new ConversationPointerText
            {
                Type = ConversationPointerType.Text,
                OffsetValue = 0,
                Title = conversationPointer.TextBoxes[0].Title,
                ItemLifeTime = ConversationItemLifeTime.New,
                Lines = new ObservableCollection<CsvbTextLine>
                {
                    new CsvbTextLine
                    {
                        Text = string.Empty,
                    }
                }
            });
        }

        private void AddNewTextBoxInfront(ConversationPointerTextWithYesNo? conversationPointerTextWithYesNo)
        {
            ArgumentNullException.ThrowIfNull(conversationPointerTextWithYesNo, nameof(conversationPointerTextWithYesNo));

            //int index = (EvmFile!.PointerTable[0]).TextBoxes.ToList().FindIndex(x => x.Equals(conversationPointerTextWithYesNo));

            //(EvmFile.PointerTable[0]).TextBoxes.Insert(index, new ConversationPointerText
            //{
            //    Type = ConversationPointerType.Text,
            //    OffsetValue = 0,
            //    Title = (EvmFile.PointerTable[0]).TextBoxes[index - 1].Title,
            //    ItemLifeTime = ConversationItemLifeTime.New,
            //    Lines = new ObservableCollection<CsvbTextLine>
            //    {
            //        new CsvbTextLine
            //        {
            //            Text = string.Empty,
            //        }
            //    }
            //});
        }

        private async Task CloseFile()
        {
            var dialog = MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
            {
                ContentTitle = "Close Conversation",
                ContentMessage = "Are you sure, you want to close the conversation file?",
                ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

            ButtonResult buttonResult = await dialog.ShowAsync().ConfigureAwait(false);

            if (buttonResult == ButtonResult.No)
            {
                return;
            }

            EvmFile = null;

            IsFileLoaded = false;
        }

        public ICommand OpenFileCommand => new AsyncRelayCommand(OpenFile);
        public ICommand SaveCommand => new AsyncRelayCommand(Save);
        public ICommand AddLineCommand => new AsyncRelayCommand<ConversationPointerText>(AddNewLineToTextBox);
        public ICommand AddTextBoxCommand => new RelayCommand<ConversationPointerCharacter>(AddNewTextBox);
        public ICommand AddTextBoxInfrontCommand => new RelayCommand<ConversationPointerTextWithYesNo>(AddNewTextBoxInfront);
        public ICommand CloseFileCommand => new AsyncRelayCommand(CloseFile);

        private EvmBaseCsvb? _evmFile;
        public EvmBaseCsvb? EvmFile
        {
            get => _evmFile;
            set => SetProperty(ref _evmFile, value);
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
