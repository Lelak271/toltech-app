using System.Runtime.CompilerServices;
using SQLite;
using Toltech.App.ViewModels;

namespace Toltech.App.Models
{
    public class ModelData : BaseViewModel
    {

        #region Backing fields

        // Champs privés
        private int _id;
        private double _coordX;
        private double _coordY;
        private double _coordZ;

        private double _coordU;
        private double _coordV;
        private double _coordW;

        private string _origine;
        private string _extremite;
        private string _model;

        private bool _active;

        private double _tolOri;
        private double _tolInt;
        private double _tolExtr;

        private string _descriptionTolOri;
        private string _descriptionTolInt;
        private string _descriptionTolExtre;

        private string _nameTolOri;
        private string _nameTolInt;
        private string _nameTolExtre;

        private string _commentaire;

        private bool _checkBoxOri;
        private bool _checkBoxInt;
        private bool _checkBoxExtre;

        private int _idTolOri;
        private int _idTolInt;
        private int _idTolExtre;

        private LiaisonType _type = LiaisonType.Ponctuelle;

        #endregion

        /// <summary>
        /// Types de liaison mécaniques possibles
        /// </summary>
        public enum LiaisonType
        {
            Ponctuelle = 0,
            LineaireAnnulaire = 1,
            Rotule = 2,
            Fixe = 3
        }


        #region Propriétés éditables (dirty-aware)

        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetAndDirty(ref _id, value);
        } // Identifiant unique de l'enregistrement


        public double CoordX
        {
            get => _coordX;
            set => SetAndDirty(ref _coordX, value);
        } // Coordonnée X

        public double CoordY
        {
            get => _coordY;
            set => SetAndDirty(ref _coordY, value);
        } // Coordonnée Y

        public double CoordZ
        {
            get => _coordZ;
            set => SetAndDirty(ref _coordZ, value);
        } // Coordonnée Z


        public double CoordU
        {
            get => _coordU;
            set => SetAndDirty(ref _coordU, value);
        } // Coordonnée U

        public double CoordV
        {
            get => _coordV;
            set => SetAndDirty(ref _coordV, value);
        } // Coordonnée V

        public double CoordW
        {
            get => _coordW;
            set => SetAndDirty(ref _coordW, value);
        } // Coordonnée W


        #region Parts 

        public int OriginePartId { get; set; }

        private int? _extremitePartId;
        public int? ExtremitePartId
        {
            get => _extremitePartId;
            set => SetAndDirty(ref _extremitePartId, value);
        }


        private Part _originePart;

        private Part _extremitePart;

        [Ignore]
        public Part OriginePart
        {
            get => _originePart;
            set
            {
                if (SetProperty(ref _originePart, value))
                {
                    OriginePartId = value?.Id ?? 0;
                }
            }
        }

        [Ignore]
        public Part ExtremitePart
        {
            get => _extremitePart;
            set
            {
                if (SetProperty(ref _extremitePart, value))
                {
                    ExtremitePartId = value?.Id ?? 0;
                }
            }
        }

        [Ignore]
        public string Origine => OriginePart?.NamePart;

        [Ignore]
        public string Extremite => ExtremitePart?.NamePart;


        #endregion

        public string Model
        {
            get => _model;
            set => SetAndDirty(ref _model, value);
        } // Nom de la ponctuelle

        public bool Active
        {
            get => _active;
            set => SetAndDirty(ref _active, value);
        } // Contact actif ou non 


        public double TolOri
        {
            get => _tolOri;
            set => SetAndDirty(ref _tolOri, value);
        } // Tolérance de l'origine

        public double TolInt
        {
            get => _tolInt;
            set => SetAndDirty(ref _tolInt, value);
        } // Tolérance intermédiaire

        public double TolExtr
        {
            get => _tolExtr;
            set => SetAndDirty(ref _tolExtr, value);
        } // Tolérance de l'extrémité


        public string DescriptionTolOri
        {
            get => _descriptionTolOri;
            set => SetAndDirty(ref _descriptionTolOri, value);
        } // Description 1

        public string DescriptionTolInt
        {
            get => _descriptionTolInt;
            set => SetAndDirty(ref _descriptionTolInt, value);
        } // Description 2

        public string DescriptionTolExtre
        {
            get => _descriptionTolExtre;
            set => SetAndDirty(ref _descriptionTolExtre, value);
        } // Description 3


        public string NameTolOri
        {
            get => _nameTolOri;
            set => SetAndDirty(ref _nameTolOri, value);
        } // Nom de la tolérance Origine

        public string NameTolInt
        {
            get => _nameTolInt;
            set => SetAndDirty(ref _nameTolInt, value);
        } // Nom de la tolérance intermédiaire

        public string NameTolExtre
        {
            get => _nameTolExtre;
            set => SetAndDirty(ref _nameTolExtre, value);
        } // Nom de la tolérance extrémité


        public string Commentaire
        {
            get => _commentaire;
            set => SetAndDirty(ref _commentaire, value);
        } // Commentaire


        public bool CheckBoxOri
        {
            get => _checkBoxOri;
            set => SetAndDirty(ref _checkBoxOri, value);
        } // DB ou non de la tol ORIGINE

        public bool CheckBoxInt
        {
            get => _checkBoxInt;
            set => SetAndDirty(ref _checkBoxInt, value);
        } // DB ou non de la tol INT

        public bool CheckBoxExtre
        {
            get => _checkBoxExtre;
            set => SetAndDirty(ref _checkBoxExtre, value);
        } // DB ou non de la tol EXTREMITE


        public int IdTolOri
        {
            get => _idTolOri;
            set => SetAndDirty(ref _idTolOri, value);
        } // DB ou non de la tol ORIGINE

        public int IdTolInt
        {
            get => _idTolInt;
            set => SetAndDirty(ref _idTolInt, value);
        } // DB ou non de la tol INT

        public int IdTolExtre
        {
            get => _idTolExtre;
            set => SetAndDirty(ref _idTolExtre, value);
        } // DB ou non de la tol EXTREMITE


        public LiaisonType Type
        {
            get => _type;
            set => SetAndDirty(ref _type, value);
        } // Type de liaison mécanique (Ponctuelle, Linéaire, Rotule, Fixe)

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

        public void LoadFromDb(ModelData db)
        {
            if (db == null)
                return;

            BeginLoad();

            Id = db.Id;

            CoordX = db.CoordX;
            CoordY = db.CoordY;
            CoordZ = db.CoordZ;

            CoordU = db.CoordU;
            CoordV = db.CoordV;
            CoordW = db.CoordW;

            OriginePartId = db.OriginePartId;
            ExtremitePartId = db.ExtremitePartId;

            Model = db.Model;
            Active = db.Active;
            Type = db.Type;

            TolOri = db.TolOri;
            TolInt = db.TolInt;
            TolExtr = db.TolExtr;

            DescriptionTolOri = db.DescriptionTolOri;
            DescriptionTolInt = db.DescriptionTolInt;
            DescriptionTolExtre = db.DescriptionTolExtre;


            NameTolOri = db.NameTolOri;
            NameTolInt = db.NameTolInt;
            NameTolExtre = db.NameTolExtre;


            CheckBoxOri = db.CheckBoxOri;
            CheckBoxInt = db.CheckBoxInt;
            CheckBoxExtre = db.CheckBoxExtre;

            IdTolOri = db.IdTolOri;
            IdTolInt = db.IdTolInt;
            IdTolExtre = db.IdTolExtre;

            Commentaire = db.Commentaire;


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
