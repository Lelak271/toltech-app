using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toltech.App.ViewModels
{
    public class V3DViewModel : BaseViewModel
    {
        #region Fields
        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;

        #endregion


        public V3DViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
        }

    }
}
