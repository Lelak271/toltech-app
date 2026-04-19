using System.Windows;
using Microsoft.Win32;

namespace Toltech.App.Services.Dialog
{
    public class DialogService : IDialogService
    {
        public void Info(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Warning(string message, string title = "Attention")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void Error(string message, string title = "Erreur")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool Confirm(string message, string title = "Confirmation")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public bool Ask(string message, string title = "Question")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public string? OpenFile(string filter, string title, string initialDirectory = "")
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(initialDirectory))
                dialog.InitialDirectory = initialDirectory;

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? SaveFile(string filter, string title, string defaultFileName = "")
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = defaultFileName,
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? OpenFolder(string title, string initialDirectory = "")
        {
            var dialog = new OpenFolderDialog
            {
                Title = title,
                InitialDirectory = initialDirectory,
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(initialDirectory))
                dialog.InitialDirectory = initialDirectory;

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

    }

}
