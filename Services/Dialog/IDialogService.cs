using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toltech.App.Services.Dialog
{
    public interface IDialogService
    {
        void Info(string message, string title = "Information");
        void Warning(string message, string title = "Attention");
        void Error(string message, string title = "Erreur");
        bool Confirm(string message, string title = "Confirmation");
        bool Ask(string message, string title = "Question");

        string? OpenFile(string filter, string title, string initialDirectory="");
        string? OpenFolder(string title, string initialDirectory="");
        string? SaveFile(string filter, string title, string defaultFileName = "");

    }

}
