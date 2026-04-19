using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toltech.App;

namespace Toltech.App.ViewModels
{

    public class HomePageViewModel : BaseViewModel
    {
        public string AppVersion => AppInfo.VersionApp;
        public string NameApp => AppInfo.ProductName;
        public string Framework => AppInfo.Framework;


    }
}
