using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace TOLTECH_APPLICATION.Models
{
    public class Part : INotifyPropertyChanged
    {
        // Événement standard pour WPF
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Méthode générique pour notifier les changements de propriété.
        /// </summary>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _id;
        private string _namePart;
        private double _masseVol;
        private byte[] _imagePart; 
        private string _comment;
        private bool _isFixed;
        private bool _isActive;


        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        } // Identifiant unique de l'enregistrement

        /// <summary>
        /// Nom de la pièce
        /// </summary>
        public string NamePart
        {
            get => _namePart;
            set => SetField(ref _namePart, value);
        }

        /// <summary>
        /// Masse volumique de la pièce
        /// </summary>
        public double MasseVol
        {
            get => _masseVol;
            set => SetField(ref _masseVol, value);
        }

        /// <summary>
        /// Image associée à la pièce (stockée en BLOB SQLite)
        /// </summary>
        public byte[] ImagePart
        {
            get => _imagePart;
            set => SetField(ref _imagePart, value);
        }

        /// <summary>
        /// Commentaire ou note associée à la pièce
        /// </summary>
        public string Comment
        {
            get => _comment;
            set => SetField(ref _comment, value);
        }
        public bool IsFixed
        {
            get => _isFixed;
            set => SetField(ref _isFixed, value);
        }
        public bool IsActive
        {
            get => _isActive;
            set => SetField(ref _isActive, value);
        }

    }
}
