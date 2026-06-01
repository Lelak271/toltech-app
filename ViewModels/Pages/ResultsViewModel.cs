using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using Toltech.App.Models;
using Toltech.App.Resources;

namespace Toltech.App.ViewModels
{
    public class ResultsViewModel : BaseViewModel
    {
        #region Fields
        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;

        private readonly RequirementsViewModel _requirementsVM;
        #endregion

        #region Collections

        public ObservableCollection<Requirements> Requirements => _requirementsVM.Requirements;

        public ListCollectionView AllRequirements => _requirementsVM.AllRequirements;
        #endregion

        public ResultsViewModel(MainViewModel mainVM, RequirementsViewModel requirementsVM)
        {
            _mainVM = mainVM;
            _requirementsVM = requirementsVM;
        }
    }
}
