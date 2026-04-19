using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace TOLTECH_APPLICATION.Resources.Lang
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static readonly LocalizationManager _instance = new();
        public static LocalizationManager Instance => _instance;

        private readonly ResourceManager _resourceManager =
            AppResources.ResourceManager;

        public CultureInfo CurrentCulture { get; private set; }


        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key]
            => _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        public void ChangeCulture(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            CurrentCulture= culture;

            // Force refresh bindings
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
