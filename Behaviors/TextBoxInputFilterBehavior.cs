using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TOLTECH_APPLICATION.Behaviors
{
    /// <summary>
    /// Comportement attaché pour filtrer la saisie des TextBox.
    /// Permet de limiter les caractères saisis selon le type défini :
    /// - <see cref="FilterType.Numeric"/> : autorise uniquement les nombres, 
    ///   le signe négatif et le séparateur décimal (normalisé en point).
    /// - <see cref="FilterType.Alphabetic"/> : autorise uniquement les lettres et les espaces.
    /// - <see cref="FilterType.None"/> : aucune restriction.
    /// 
    /// Prend en charge :
    ///— la saisie clavier et le collage,
    ///— la gestion des états intermédiaires(ex. "-", "." pour Numeric),
    ///— la compatibilité avec les bindings sur double.
    /// </summary>
    /// 
    public static class TextBoxInputFilterBehavior
    {
        public enum FilterType { None, Numeric, Alphabetic }

        public static readonly DependencyProperty InputFilterProperty =
            DependencyProperty.RegisterAttached(
                "InputFilter",
                typeof(FilterType),
                typeof(TextBoxInputFilterBehavior),
                new PropertyMetadata(FilterType.None, OnFilterChanged));

        public static FilterType GetInputFilter(DependencyObject obj) =>
            (FilterType)obj.GetValue(InputFilterProperty);

        public static void SetInputFilter(DependencyObject obj, FilterType value) =>
            obj.SetValue(InputFilterProperty, value);

        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox)
                return;

            textBox.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(textBox, OnPaste);

            if ((FilterType)e.NewValue != FilterType.None)
            {
                textBox.PreviewTextInput += OnPreviewTextInput;
                DataObject.AddPastingHandler(textBox, OnPaste);
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            // Normaliser la saisie : remplacer la virgule par un point
            if (e.Text == ",")
            {
                int start = textBox.SelectionStart;
                int length = textBox.SelectionLength;

                textBox.Text = textBox.Text.Remove(start, length)
                                         .Insert(start, ".");
                textBox.SelectionStart = start + 1;

                // Marquer comme traité pour empêcher l'insertion du caractère original
                e.Handled = true;
                return;
            }

            // Si le caractère n'est pas autorisé, bloquer
            if (!IsInputAllowed(textBox, e.Text))
                e.Handled = true;
        }


        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
                return;

            if (!IsInputAllowed(textBox, e.DataObject.GetData(DataFormats.Text)?.ToString()))
                e.CancelCommand();
        }

        private static bool IsInputAllowed(TextBox textBox, string input)
        {
            return GetInputFilter(textBox) switch
            {
                FilterType.Numeric => IsNumericInputValid(textBox, input),

                FilterType.Alphabetic => Regex.IsMatch(
                    input,
                    @"^[a-zA-Z\s]*$",
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(100)
                ),

                _ => true
            };
        }

        private static bool IsNumericInputValid(TextBox textBox, string input)
        {
            // On met uniquement le point comme séparateur décimal
            // car :
            // - WPF binding sur double n'accepte que le point avec CultureInfo.InvariantCulture
            // - la virgule disparaît sinon au LostFocus
            // - cela évite de créer des propriétés string supplémentaires
            char separator = '.';

            // Remplacer la virgule par le point si l'utilisateur tape une virgule
            if (input == ",")
                input = separator.ToString();

            // On prend en compte le curseur et la sélection pour simuler le futur texte
            string text = textBox.Text;
            int start = textBox.SelectionStart;
            int length = textBox.SelectionLength;

            string newText = text.Remove(start, length).Insert(start, input);

            // - vide : l'utilisateur n'a rien tapé
            // - "-" : signe négatif
            // - "." : point seul pour commencer un décimal
            if (string.IsNullOrEmpty(newText) ||
                newText == "-" ||
                newText == separator.ToString())
                return true;

            // On parse avec InvariantCulture pour que le binding double accepte toujours le point
            return double.TryParse(
                newText,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture,
                out _);
        }

    }

    /// <summary>
    /// Comportement attaché pour les TextBox qui gère la touche Enter.
    /// Lorsqu'Enter est pressé :
    /// - si <see cref="TextBox.AcceptsReturn"/> est false, déclenche un LostFocus
    ///   sans déplacer le focus vers un autre contrôle,
    /// - évite le bip sonore et l’insertion de saut de ligne.
    /// </summary>
    public static class TextBoxEnterBehavior
    {
        public static readonly DependencyProperty MoveFocusOnEnterProperty =
            DependencyProperty.RegisterAttached(
                "MoveFocusOnEnter",
                typeof(bool),
                typeof(TextBoxEnterBehavior),
                new PropertyMetadata(false, OnMoveFocusOnEnterChanged));

        public static readonly DependencyProperty AllowEnterKeyProperty =
    DependencyProperty.RegisterAttached(
        "AllowEnterKey",
        typeof(bool),
        typeof(TextBoxEnterBehavior),
        new PropertyMetadata(false));

        public static bool GetAllowEnterKey(DependencyObject obj) =>
            (bool)obj.GetValue(AllowEnterKeyProperty);

        public static void SetAllowEnterKey(DependencyObject obj, bool value) =>
            obj.SetValue(AllowEnterKeyProperty, value);


        public static bool GetMoveFocusOnEnter(DependencyObject obj) => (bool)obj.GetValue(MoveFocusOnEnterProperty);
        public static void SetMoveFocusOnEnter(DependencyObject obj, bool value) => obj.SetValue(MoveFocusOnEnterProperty, value);

        private static void OnMoveFocusOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                if ((bool)e.NewValue)
                    tb.PreviewKeyDown += Tb_PreviewKeyDown;
                else
                    tb.PreviewKeyDown -= Tb_PreviewKeyDown;
            }
        }

        private static void Tb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            // Laisser passer Enter pour les TextBox qui en ont besoin (rename, édition inline, etc.)
            if (GetAllowEnterKey(tb))
                return;

            if (e.Key == Key.Enter && !tb.AcceptsReturn)
            {
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

    }
}
