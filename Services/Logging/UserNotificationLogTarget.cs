using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Toltech.App.Services.Logging
{
    public class UserNotificationLogTarget : ILogTarget
    {
        public void Write(LogEntry entry)
        {
            if (entry.Level == LogLevel.Error || entry.Level == LogLevel.Warning)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(entry.Message, "Toltech", MessageBoxButton.OK,
                        entry.Level == LogLevel.Error ? MessageBoxImage.Error : MessageBoxImage.Warning);
                });
            }
        }
    }
}
