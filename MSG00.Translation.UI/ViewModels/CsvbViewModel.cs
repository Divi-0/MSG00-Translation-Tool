using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Mediator;
using MSG00.Translation.Application.Features.Csvb.Read;
using MSG00.Translation.Infrastructure.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICommand = System.Windows.Input.ICommand;

namespace MSG00.Translation.UI.ViewModels
{
    public class CsvbViewModel : ViewModelBase
    {
        private readonly IMediator _mediator;
        private readonly IStorageProvider _storageProvider;

        public CsvbViewModel(IMediator mediator, IStorageProvider storageProvider)
        {
            _mediator = mediator;
            _storageProvider = storageProvider;
        }

        private async Task OpenFile()
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

            await using Stream stream = await selectFileList[0].OpenReadAsync().ConfigureAwait(false);
            await _mediator.Send(new ReadCsvbFile(stream)).ConfigureAwait(false);
        }

        public ICommand OpenFileCommand => new AsyncRelayCommand(OpenFile);
    }
}
