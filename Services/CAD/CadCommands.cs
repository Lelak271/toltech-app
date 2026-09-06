using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Toltech.App.Services.CAD
{
    public static class CadCommands
    {
        public static readonly RoutedUICommand PickPoint = new RoutedUICommand(
            "Choisir un point CAO",
            nameof(PickPoint),
            typeof(CadCommands));


        public static readonly RoutedUICommand PickEdge = new RoutedUICommand(
            "Choisir une arrete CAO",
            nameof(PickEdge),
            typeof(CadCommands));
    }
}
