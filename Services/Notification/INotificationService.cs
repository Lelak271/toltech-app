using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toltech.App.Services.Notification
{
    public interface INotificationService
    {
        Task ShowNotifAsync(string message, bool isError = false);
    }
}
