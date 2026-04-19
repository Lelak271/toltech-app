using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TOLTECH_APPLICATION.Resources;
using TOLTECH_APPLICATION.Resources.ColorTheme;
using TOLTECH_APPLICATION.Resources.Lang;
using TOLTECH_APPLICATION.Utilities;

namespace TOLTECH_APPLICATION.Views
{
    public partial class SettingsPage : Window
    {
        private AppSettingsData _userSettings;

        public SettingsPage()
        {
            InitializeComponent();

            _userSettings = new AppSettingsData();
            _userSettings.LoadFromUserConfig();

            InitializeControlsFromSettings();

            DirtyFunction();
        }

        private void InitializeControlsFromSettings()
        {
            // Langue
            SelectComboBoxItemByEnum(LanguageComboBox, _userSettings.Language);

            // Unité de longueur
            SelectRadioButtonByTag(UnitStack, "Units", _userSettings.LengthUnit.ToString());
       
            // Unité d'angle
            SelectRadioButtonByTag(AngleStack, "angle", _userSettings.AngleUnit.ToString());

            // Unité de masse volumique
            SelectComboBoxItemByEnum(DensityUnitComboBox, _userSettings.DensityUnit);

            // Thème
            SelectListBoxItemByEnum(ThemeList, _userSettings.Theme);
        }

        #region Fonctions Get / Set pour appliquer les settings à la View
        private static void SelectRadioButtonByTag(DependencyObject container, string groupName, string tagValue)
        {
            foreach (var rb in UIHelper.FindVisualChildren<RadioButton>(container))
            {
                if (rb.GroupName == groupName)
                    rb.IsChecked = rb.Tag?.ToString() == tagValue;
            }
        }

        private void SelectComboBoxItemByEnum<TEnum>(ComboBox combo, TEnum value)
            where TEnum : struct, Enum
        {
            foreach (var obj in combo.Items)
            {
                // On ignore tout ce qui n'est pas un ComboBoxItem
                if (obj is not ComboBoxItem item)
                    continue;

                // Vérifie si le Tag correspond à la valeur enum
                if (item.Tag != null && item.Tag.ToString() == value.ToString())
                {
                    combo.SelectedItem = item;
                    break;
                }
            }
        }

        private void SelectListBoxItemByEnum<TEnum>(ListBox list, TEnum value) where TEnum : struct, Enum
        {
            foreach (ListBoxItem item in list.Items)
            {
                if (Enum.TryParse(item.Tag?.ToString(), out TEnum itemEnum) &&
                    EqualityComparer<TEnum>.Default.Equals(itemEnum, value))
                {
                    list.SelectedItem = item;
                    break;
                }
            }
        }

        private static TEnum? GetSelectedRadioButtonTagByGroupName<TEnum>(DependencyObject container, string groupName)
            where TEnum : struct, Enum
        {
            foreach (var rb in UIHelper.FindVisualChildren<RadioButton>(container))
            {
                if (rb.GroupName == groupName && rb.IsChecked == true)
                {
                    if (rb.Tag != null && Enum.TryParse<TEnum>(rb.Tag.ToString(), out var result))
                        return result;
                }
            }
            return null;
        }

        private TEnum GetSelectedEnumFromComboBox<TEnum>(ComboBox combo) where TEnum : struct, Enum
        {
            if (combo.SelectedItem is ComboBoxItem item &&
            item.Tag != null &&
            Enum.TryParse(item.Tag.ToString(), true, out TEnum result))
            {
                return result;
            }
            return default;
        }

        private TEnum GetSelectedEnumFromListBox<TEnum>(ListBox list) where TEnum : struct, Enum
        {
            if (list.SelectedItem is ListBoxItem item &&
                Enum.TryParse(item.Tag?.ToString(), out TEnum result))
            {
                return result;
            }
            return default;
        }

        #endregion
     
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _userSettings.Language = GetSelectedEnumFromComboBox<SupportedLanguage>(LanguageComboBox);
            _userSettings.LengthUnit = GetSelectedRadioButtonTagByGroupName<SupportedLengthUnit>(UnitStack, "Units") ?? _userSettings.LengthUnit;
            _userSettings.AngleUnit = GetSelectedRadioButtonTagByGroupName<SupportedAnglehUnit>(AngleStack, "angle") ?? _userSettings.AngleUnit;
            _userSettings.DensityUnit = GetSelectedEnumFromComboBox<SupportedDensityUnit>(DensityUnitComboBox);
            _userSettings.Theme = GetSelectedEnumFromListBox<AppTheme>(ThemeList);

            _userSettings.SaveToUserConfig();

            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && Flag != null && selectedItem.Tag != null)
            {
                Flag.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri($"pack://application:,,,/Asset/Flags/{selectedItem.Tag}.png", UriKind.Absolute));

                string cultureCode = selectedItem.Tag.ToString();
                var culture = new CultureInfo(cultureCode);
                string tooltipText = AppResources.ResourceManager.GetString("ToolTipFlag", culture) ?? "Language information";

                Flag.ToolTip = new ToolTip
                {
                    Content = new TextBlock
                    {
                        Text = tooltipText,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 300
                    }
                };
            }
        }

        #region Dirty Tracking

        private bool _isDirty = false;

        private void MarkDirty() => SaveButton.IsEnabled = _isDirty = true;
        private void ResetDirty() => SaveButton.IsEnabled = _isDirty = false;

        private void DirtyFunction()
        {
            // Comboboxes
            LanguageComboBox.SelectionChanged += (s, e) => MarkDirty();
            DensityUnitComboBox.SelectionChanged += (s, e) => MarkDirty();
            ThemeList.SelectionChanged += (s, e) => MarkDirty();

            // Tous les RadioButton dans UnitStack
            foreach (var rb in UIHelper.FindVisualChildren<RadioButton>(UnitStack))
                rb.Checked += (s, e) => MarkDirty();

            // Tous les RadioButton dans AngleStack
            foreach (var rb in UIHelper.FindVisualChildren<RadioButton>(AngleStack))
                rb.Checked += (s, e) => MarkDirty();

            SaveButton.IsEnabled = false;
        }

        #endregion


    }
}