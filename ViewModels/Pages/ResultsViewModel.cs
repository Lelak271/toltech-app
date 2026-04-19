using Toltech.App.Resources;

namespace Toltech.App.ViewModels
{
    public class ResultsViewModel : BaseViewModel
    {
        #region Fields
        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;
        #endregion

        public ResultsViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
        }
    }
}
