using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Windows.Input;

namespace MSG00.Translation.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly List<ViewModelBase> _viewModels = new List<ViewModelBase>();

        public MainViewModel()
        {
            _viewModels.Add(App.Services.GetRequiredService<WelcomeViewModel>()!);
            _viewModels.Add(App.Services.GetRequiredService<ConversationCsvbViewModel>()!);
            _viewModels.Add(App.Services.GetRequiredService<PrologueCsvbViewModel>()!);
            _viewModels.Add(App.Services.GetRequiredService<EpilogueCsvbViewModel>()!);
            _viewModels.Add(App.Services.GetRequiredService<EtcCsvbViewModel>()!);
            _viewModels.Add(App.Services.GetRequiredService<RequirementCsvbViewModel>()!);
            _viewModels.Add(App.Services.GetRequiredService<StaffRollCsvbViewModel>()!);
            _viewModels.Add(App.Services.GetRequiredService<EvmCsvbViewModel>()!);

            CurrentView = _viewModels[0];
        }
        private void NavigateToHome()
        {
            CurrentView = _viewModels[0];
        }

        private void NavigateToConversation()
        {
            CurrentView = _viewModels[1];
        }

        private void NavigateToPrologue()
        {
            CurrentView = _viewModels[2];
        }

        private void NavigateToEpilogue()
        {
            CurrentView = _viewModels[3];
        }

        private void NavigateToEtc()
        {
            CurrentView = _viewModels[4];
        }

        private void NavigateToRequirement()
        {
            CurrentView = _viewModels[5];
        }

        private void NavigateToStaffRoll()
        {
            CurrentView = _viewModels[6];
        }

        private void NavigateToEvm()
        {
            CurrentView = _viewModels[7];
        }

        public ICommand HomeCommand => new RelayCommand(NavigateToHome);
        public ICommand ConversationCommand => new RelayCommand(NavigateToConversation);
        public ICommand PrologueCommand => new RelayCommand(NavigateToPrologue);
        public ICommand EpilogueCommand => new RelayCommand(NavigateToEpilogue);
        public ICommand EtcCommand => new RelayCommand(NavigateToEtc);
        public ICommand RequirementCommand => new RelayCommand(NavigateToRequirement);
        public ICommand StaffRollCommand => new RelayCommand(NavigateToStaffRoll);
        public ICommand EvmCommand => new RelayCommand(NavigateToEvm);

        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get { return _currentView; }
            set { SetProperty(ref _currentView, value); }
        }
    }
}