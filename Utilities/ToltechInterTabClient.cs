using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Dragablz;

namespace Toltech.App.Utilities
{
    public class ToltechInterTabClient : IInterTabClient
    {
        public static ToltechInterTabClient Instance { get; } = new ToltechInterTabClient();

        public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        {
            // Nouveau TabControl pour la fenêtre flottante
            var tabControl = new TabablzControl
            {
                ShowDefaultCloseButton = true,
                ItemContainerStyle = (Style)Application.Current.FindResource("TrapezoidDragableTabItemStyle"),
                InterTabController = new InterTabController
                {
                    InterTabClient = interTabClient,
                    Partition = partition as string
                },
               
            };

            var window = new Window
            {
                Title = "",
                Width = 900,
                Height = 600,
                WindowStyle = WindowStyle.ThreeDBorderWindow, // barre de titre Windows classique
                ResizeMode = ResizeMode.CanResize,            // redimensionnable
                Background = System.Windows.Media.Brushes.Transparent,
                Content = tabControl
            };

            // Quand la fenêtre flottante se ferme naturellement (par bouton de la fenêtre)
            window.Closed += (s, e) =>
            {
                if (source == null) return;

                var floatingTabControl = window.Content as TabablzControl;
                if (floatingTabControl == null) return;

                // On copie les onglets pour éviter modification de la collection en cours d'itération
                var tabs = floatingTabControl.Items.Cast<TabItem>().ToList();

                foreach (var tab in tabs)
                {
                    // Déplace l'item dans le thread UI
                    source.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            floatingTabControl.Items.Remove(tab); // Retire du contrôle flottant

                            int targetIndex = tab.TabIndex;

                            // Si l'index d'origine est valide, on insère à cette position
                            if (targetIndex >= 0 && targetIndex <= source.Items.Count)
                            {
                                source.Items.Insert(targetIndex, tab);
                            }
                            else
                            {
                                // Sinon on l'ajoute à la fin
                                source.Items.Add(tab);
                            }
                            source.SelectedItem = tab;
                        }
                        catch
                        {
                            // On peut loguer si besoin
                        }
                    });
                }
            };

            return new NewTabHost<Window>(window, tabControl);
        }

        public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        {
            // Ferme automatiquement la fenêtre si elle n’a plus d’onglets
            return TabEmptiedResponse.CloseWindowOrLayoutBranch;
        }
    }
}
