using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOLTECH_APPLICATION.FrontEnd.Interfaces
{
    public interface INotificationService
    {
        Task ShowNotifAsync(string message, bool isError = false);
    }
}
