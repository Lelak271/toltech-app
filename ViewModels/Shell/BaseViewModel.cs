using System.ComponentModel;
using System.Runtime.CompilerServices;
using Toltech.App.Resources.Lang;
using Toltech.App.Services.Dialog;
using Toltech.App.Utilities.Result;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Toltech.App.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        protected readonly IDialogService _dialog = App.DialogService;
        protected string Loc(string key)=> LocalizationManager.Instance[key];
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Dispose virtuel — chaque VM override ce dont elle a besoin
        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Affiche un message d'erreur localisé à l'utilisateur en fonction du <see cref="ErrorCode"/> du Result.
        /// Constitue le point d'entrée par défaut pour la gestion des erreurs dans les ViewModels.
        /// Pour un comportement UI spécifique, utilisez <see cref="_dialog"/> directement avec <see cref="Loc"/>.
        /// </summary>
        /// <remarks>
        /// result.Error  → message technique destiné aux logs, jamais affiché à l'utilisateur.
        /// result.Code   → clé de décision pour le message localisé affiché via les fichiers .resx.
        /// </remarks>
        protected void HandleError(Result result)
        {
            var message = result.Code switch
            {
                ErrorCode.DatabaseError => Loc("Error_Database"),
                ErrorCode.NotFound => Loc("Error_NotFound"),
                ErrorCode.FileLocked => Loc("Error_FileLocked"),
                ErrorCode.Unauthorized => Loc("Error_Unauthorized"),
                ErrorCode.NoActiveModel => Loc("Error_NoActiveModel"),
                _ => Loc("Error_Unknown")
            };

            _dialog.Error(message, Loc("Title_Error"));
        }

    }
}
