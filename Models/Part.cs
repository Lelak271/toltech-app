using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using SQLite;
using Toltech.App.ViewModels;

namespace Toltech.App.Models
{
    public class Part : BaseViewModel
    {
        #region Backing fields

        private int _id;
        private string _namePart;
        private double _masseVol;
        private byte[] _imagePart; 
        private string _comment;
        private bool _isFixed;
        private bool _isActive;

        #endregion

        #region Propriétés éditables (dirty-aware)
        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetAndDirty(ref _id, value);
        } // Identifiant unique de l'enregistrement

        /// <summary>
        /// Nom de la pièce
        /// </summary>
        public string NamePart
        {
            get => _namePart;
            set => SetAndDirty(ref _namePart, value);
        }

        /// <summary>
        /// Masse volumique de la pièce
        /// </summary>
        public double MasseVol
        {
            get => _masseVol;
            set => SetAndDirty(ref _masseVol, value);
        }

        /// <summary>
        /// Image associée à la pièce (stockée en BLOB SQLite)
        /// </summary>
        public byte[] ImagePart
        {
            get => _imagePart;
            set => SetAndDirty(ref _imagePart, value);
        }

        /// <summary>
        /// Commentaire ou note associée à la pièce
        /// </summary>
        public string Comment
        {
            get => _comment;
            set => SetAndDirty(ref _comment, value);
        }
        public bool IsFixed
        {
            get => _isFixed;
            set => SetAndDirty(ref _isFixed, value);
        }
        public bool IsActive
        {
            get => _isActive;
            set => SetAndDirty(ref _isActive, value);
        }
        #endregion

        #region Flags d’état

        private bool _isDirty;
        private bool _isSaving;
        private bool _isOutOfSync;
        private bool _isLoading;

        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value)
                    return;

                _isDirty = value;
                OnPropertyChanged();
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            private set => SetProperty(ref _isSaving, value);
        }

        public bool IsOutOfSync
        {
            get => _isOutOfSync;
            private set => SetProperty(ref _isOutOfSync, value);
        }

        #endregion

        #region Dirty helpers

        protected bool SetAndDirty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;

            OnPropertyChanged(propertyName);

            if (!_isLoading)
                MarkDirty();

            return true;
        }

        private void MarkDirty()
        {
            if (!IsDirty)
                IsDirty = true;

            IsOutOfSync = false;
        }


        // Méthodes publiques pour contrôler les flags
        public void ClearDirty() => IsDirty = false;
        public void MarkSaving() => IsSaving = true;
        public void ClearSaving() => IsSaving = false;
        public void MarkOutOfSync() => IsOutOfSync = true;

        #endregion

        #region Load / Sync
        public void LoadFromDb(Part db)
        {
            if (db == null)
                return;

            BeginLoad();

            Id = db.Id;
            NamePart = db.NamePart;
            MasseVol = db.MasseVol;
            ImagePart = db.ImagePart;
            Comment = db.Comment;
            IsFixed = db.IsFixed;
            IsActive = db.IsActive;

            EndLoad();
        }

        private void BeginLoad()
        {
            _isLoading = true;
        }

        private void EndLoad()
        {
            _isLoading = false;
            IsDirty = false;
            IsOutOfSync = false;
        }

        #endregion
    }
}
