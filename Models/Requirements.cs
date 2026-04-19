using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Vml;
using SQLite;
using Toltech.App.ViewModels;
using Toltech.App.Views;

namespace Toltech.App.Models
{
    public class Requirements : BaseViewModel
    {

        #region Backing fields

        private double _coordX;
        private double _coordY;
        private double _coordZ;

        private string _nameReq;

        private string _nameTolOri;
        private string _nameTolExtre;

        private string _description1;
        private string _description2;

        private string _commentaire;

        private double _tol1;
        private double _tol2;
        private int _id_tol1;
        private int _id_tol2;

        private double _coordU;
        private double _coordV;
        private double _coordW;

        private bool _checkBox1;
        private bool _checkBox2;

        #endregion


        #region Propriétés éditables (dirty-aware)
        [PrimaryKey, AutoIncrement]
        public int Id_req { get; set; } // Identifiant unique de l'exigence

        public double CoordX
        {
            get => _coordX;
            set => SetAndDirty(ref _coordX, value);
        }

        public double CoordY
        {
            get => _coordY;
            set => SetAndDirty(ref _coordY, value);
        }

        public double CoordZ
        {
            get => _coordZ;
            set => SetAndDirty(ref _coordZ, value);
        }

        public string NameReq
        {
            get => _nameReq;
            set => SetAndDirty(ref _nameReq, value);
        }

        public string NameTolOri
        {
            get => _nameTolOri;
            set => SetAndDirty(ref _nameTolOri, value);
        }

        public string NameTolExtre
        {
            get => _nameTolExtre;
            set => SetAndDirty(ref _nameTolExtre, value);
        }

        public string Description1
        {
            get => _description1;
            set => SetAndDirty(ref _description1, value);
        }

        public string Description2
        {
            get => _description2;
            set => SetAndDirty(ref _description2, value);
        }

        public string Commentaire
        {
            get => _commentaire;
            set => SetAndDirty(ref _commentaire, value);
        }

        public double tol1
        {
            get => _tol1;
            set => SetAndDirty(ref _tol1, value);
        }

        public double tol2
        {
            get => _tol2;
            set => SetAndDirty(ref _tol2, value);
        }

        public int Id_tol1
        {
            get => _id_tol1;
            set => SetAndDirty(ref _id_tol1, value);
        }

        public int Id_tol2
        {
            get => _id_tol2;
            set => SetAndDirty(ref _id_tol2, value);
        }

        public double CoordU
        {
            get => _coordU;
            set => SetAndDirty(ref _coordU, value);
        }

        public double CoordV
        {
            get => _coordV;
            set => SetAndDirty(ref _coordV, value);
        }

        public double CoordW
        {
            get => _coordW;
            set => SetAndDirty(ref _coordW, value);
        }

        public bool CheckBox1
        {
            get => _checkBox1;
            set => SetAndDirty(ref _checkBox1, value);
        }

        public bool CheckBox2
        {
            get => _checkBox2;
            set => SetAndDirty(ref _checkBox2, value);
        }


        #region Parts 

        private int? _partReq1Id;
        public int? PartReq1Id
        {
            get => _partReq1Id;
            set => SetAndDirty(ref _partReq1Id, value);
        }

        private int? _partReq2Id;
        public int? PartReq2Id
        {
            get => _partReq2Id;
            set => SetAndDirty(ref _partReq2Id, value);
        }


        private Part _partReq1;

        private Part _partReq2;

        [Ignore]
        public Part PartReq1
        {
            get => _partReq1;
            set
            {
                if (SetProperty(ref _partReq1, value))
                {
                    PartReq1Id = value?.Id ?? 0;
                }
            }
        }

        [Ignore]
        public Part PartReq2
        {
            get => _partReq2;
            set
            {
                if (SetProperty(ref _partReq2, value))
                {
                    PartReq2Id = value?.Id ?? 0;
                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetAndDirty(ref _isActive, value);
        }

        [Ignore]
        public string Part1 => PartReq1?.NamePart;

        [Ignore]
        public string Part2 => PartReq2?.NamePart;


        #endregion

       
        /// <summary>
        /// Gestion des valeurs de la DB interne
        /// Exposition dans la classe et gestion dans le VM RequirementsViewModel
        /// </summary>
        #region CheckBox 1

        private double _calculatedTol1;
        [Ignore]
        public double DisplayTol1
        {
            get => CheckBox1 ? _calculatedTol1 : tol1;
            set
            {
                if (CheckBox1)
                    SetCalculatedTol1(value); // met à jour _calculatedTol + OnPropertyChanged
                else
                    tol1 = value;

                OnPropertyChanged(nameof(DisplayTol1));
            }
        }
        public void SetCalculatedTol1(double value)
        {
            _calculatedTol1 = value;
            OnPropertyChanged(nameof(DisplayTol1));
        }
        #endregion

        #region CheckBox 2

        private double _calculatedTol2;
        [Ignore]
        public double DisplayTol2
        {
            get => CheckBox2 ? _calculatedTol2 : tol2;
            set
            {
                if (CheckBox2)
                    SetCalculatedTol2(value); // met à jour _calculatedTol + OnPropertyChanged
                else
                    tol2 = value;

                OnPropertyChanged(nameof(DisplayTol2));
            }
        }
        public void SetCalculatedTol2(double value)
        {
            _calculatedTol2 = value;
            OnPropertyChanged(nameof(DisplayTol2));
        }
        #endregion

        #endregion

        #region Flags d’état

        private bool _isDirty;
        private bool _isSaving;
        private bool _isOutOfSync;
        private bool _isLoading;

        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
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

        public void LoadFromDb(Requirements db)
        {
            if (db == null)
                return;

            BeginLoad();

            Id_req = db.Id_req;

            IsActive = db.IsActive;

            PartReq1Id = db.PartReq1Id;
            PartReq2Id = db.PartReq2Id;

            CoordX = db.CoordX;
            CoordY = db.CoordY;
            CoordZ = db.CoordZ;

            NameReq = db.NameReq;

            NameTolOri = db.NameTolOri;
            NameTolExtre = db.NameTolExtre;

            Description1 = db.Description1;
            Description2 = db.Description2;

            Commentaire = db.Commentaire;

            tol1 = db.tol1;
            tol2 = db.tol2;
            Id_tol1 = db.Id_tol1;
            Id_tol2 = db.Id_tol2;

            CoordU = db.CoordU;
            CoordV = db.CoordV;
            CoordW = db.CoordW;

            CheckBox1 = db.CheckBox1;
            CheckBox2 = db.CheckBox2;

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
