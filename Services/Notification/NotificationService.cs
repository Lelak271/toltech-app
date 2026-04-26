using System.Threading.Tasks;
using System.Windows.Controls;
using Toltech.App.Resources;
using Toltech.App.ViewModels;

namespace Toltech.App.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly ContentPresenter _host;
        private MainViewModel _mainVM;
        public NotificationService()
        {
        }

        public async Task ShowNotifAsync(string message, bool isError = false)
        {
            if(_mainVM==null)
                  _mainVM = App.MainVM;

            var notification = new NotificationControl(message);

            _mainVM.CurrentNotification = notification;

            await notification.ShowAsync(isError);

            _mainVM.CurrentNotification = null;
        }
    }
}
