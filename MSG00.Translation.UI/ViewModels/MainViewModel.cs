using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            _currentView = App.Services.GetRequiredService<CsvbSelectViewModel>();
        }

        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get { return _currentView; }
            set { SetProperty(ref _currentView, value); }
        }
    }
}