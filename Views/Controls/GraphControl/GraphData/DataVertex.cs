using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Westermo.GraphX.Common.Models;

namespace TOLTECH_APPLICATION.Views.Controls.GrapheControl.GraphData
{
    public class DataVertex : VertexBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string Text { get; set; } = string.Empty;
        public int ImageId { get; set; }

        private double _imageHeight = 100; // Valeur par défaut cohérente avec le slider

        public double ImageHeight
        {
            get => _imageHeight;
            set
            {
                if (Math.Abs(_imageHeight - value) > 0.1) // Comparaison précise pour les doubles
                {
                    _imageHeight = value;
                    Notify(nameof(ImageHeight)); // Spécifiez explicitement le nom de la propriété
                    Notify(nameof(ImageSource)); // Rafraîchir aussi l'image si nécessaire
                }
            }
        }

        public BitmapImage ImageSource { get; set; }

        public override string ToString() => Text;

        public DataVertex(string text = "") => Text = text;


    }
}
