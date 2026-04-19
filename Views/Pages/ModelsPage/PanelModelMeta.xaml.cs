using System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using TOLTECH_APPLICATION.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TOLTECH_APPLICATION.ViewModels;
using TOLTECH_APPLICATION.Services;

namespace TOLTECH_APPLICATION.FrontEnd.Controls
{
    // Logique d'interaction pour PanelModelMeta.xaml
    public partial class PanelModelMeta : UserControl, INotifyPropertyChanged
    {

        public event EventHandler EditClicked;
        public event EventHandler OpenClicked;
        public event EventHandler DuplicateClicked;
        public event EventHandler DeleteClicked;

        public PanelModelMeta()
        {
            InitializeComponent();
        }

        #region IsEditable (UI only)

        public bool IsEditable
        {
            get => (bool)GetValue(IsEditableProperty);
            private set => SetValue(IsEditableProperty, value);
        }

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(
                nameof(IsEditable),
                typeof(bool),
                typeof(PanelModelMeta),
                new PropertyMetadata(false));

        /// <summary>
        /// Active ou désactive l’édition du panel.
        /// Méthode UI uniquement (aucune logique métier).
        /// </summary>
        internal void SetEditable(bool value)
        {
            IsEditable = value;
        }

        #endregion


        public string Filepath_Model_Panel { get; set; }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                    yield return t;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        private void Edition_Click(object sender, RoutedEventArgs e) => EditClicked?.Invoke(this, EventArgs.Empty);
        private void Ouvrir_Click(object sender, RoutedEventArgs e) => OpenClicked?.Invoke(this, EventArgs.Empty);
        private void Dupliquer_Click(object sender, RoutedEventArgs e) => DuplicateClicked?.Invoke(this, EventArgs.Empty);
        private void Supprimer_Click(object sender, RoutedEventArgs e) => DeleteClicked?.Invoke(this, EventArgs.Empty);


        #region Gestion de la mise en place de l'image 

        // Charge et affiche l'image
        private void LoadImage(string path)
        {
            try
            {
                //BitmapImage bitmap = new BitmapImage();
                //bitmap.BeginInit();
                //bitmap.UriSource = new Uri(path);
                //bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //bitmap.EndInit();

                //Image_Model.Source = bitmap;
                if (!File.Exists(path))
                    return;

                byte[] imageBytes = File.ReadAllBytes(path);

                if (DataContext is ModelMeta meta)
                {
                    meta.ImageData = imageBytes;   // 🔹 déclenche le binding
                }

                //ImagePlaceholder.Visibility = Visibility.Collapsed;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement de l'image : " + ex.Message);
            }
        }
        //Méthode appelée lorsqu'on clique sur la zone image
        private void ImageBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            if(!IsEditable==true)
            {
                e.Handled = true;
                return;
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                LoadImage(openFileDialog.FileName);
            }
        }
      
        //Méthode appelée lorsqu'on glisse un fichier sur la zone
        private void ImageBorder_Drop(object sender, DragEventArgs e)
        {
            if (!IsEditable == true)
            {
                e.Handled = true;
                return;
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && File.Exists(files[0]))
                {
                    LoadImage(files[0]);
                }
            }
        }

        // Permet de changer le curseur en mode "copie" lors du drag
        private void ImageBorder_DragOver(object sender, DragEventArgs e)
        {
            if (!IsEditable == true)
            {
                e.Handled = true;
                return;
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        #endregion


        
    }
}

