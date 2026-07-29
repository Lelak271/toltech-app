using System.Runtime.CompilerServices;
using SQLite;
using Toltech.App.ViewModels;

namespace Toltech.App.Models
{
    public partial class ModelData : BaseViewModel
    {
        public ModelData()
        {
            CreateToleranceGroups();
        }

        #region Backing fields

        // Champs privés
        private int _id;
        private double _coordX;
        private double _coordY;
        private double _coordZ;

        private double _coordU;
        private double _coordV;
        private double _coordW;

        private double _coordU2;
        private double _coordV2;
        private double _coordW2;

        private string _origine;
        private string _extremite;
        private string _model;

        private bool _active;

        private string _commentaire;

        private bool _checkBoxOri;
        private bool _checkBoxInt;
        private bool _checkBoxExtre;

        private int _idTolOri;
        private int _idTolInt;
        private int _idTolExtre;

        private LiaisonType _linkage = LiaisonType.PointContact;

        #endregion

        /// <summary>
        /// Types de liaison mécaniques possibles
        /// </summary>
        public enum LiaisonType
        {
            PointContact = 0,       // Liaison ponctuelle
            LinearContact = 1,      // Liaison linéaire rectiligne
            AnnularContact = 2,     // Liaison linéaire annulaire
            PlanarContact = 3,      // Appui plan

            RevoluteContact = 4,    // Liaison pivot
            PrismaticContact = 5,   // Liaison glissière
            CylindricalContact = 6, // Liaison pivot glissant

            //HelicalContact = 7,   // Liaison hélicoïdale

            SphericalContact = 8,   // Liaison rotule

            //PinSlotContact = 9,   // Rotule à doigt

            FixedContact = 10       // Liaison encastrement
        }

        private int _selectedToleranceGroup;
        private bool _toleranceGroupLoaded;

        [Ignore] // pur état d'affichage, jamais persisté en base
        public int SelectedToleranceGroup
        {
            get
            {
                if (!_toleranceGroupLoaded)
                {
                    _toleranceGroupLoaded = true;
                    if (ToleranceGroupSelectionCache.TryGet(Id, out int cached))
                    {
                        _selectedToleranceGroup = cached;
                    }
                }
                return _selectedToleranceGroup;
            }
            set
            {
                _toleranceGroupLoaded = true; // évite un re-load qui écraserait la valeur choisie par l'utilisateur
                if (SetProperty(ref _selectedToleranceGroup, value))
                {
                    ToleranceGroupSelectionCache.Save(Id, value);
                }
            }
        }

        public static class LinkagePanelMap
        {
            public static readonly Dictionary<ModelData.LiaisonType, int[]> Map = new()
            {
                [ModelData.LiaisonType.PointContact] = new[] { 1 },
                [ModelData.LiaisonType.LinearContact] = new[] { 1, 4 },
                [ModelData.LiaisonType.AnnularContact] = new[] { 2, 3 },
                [ModelData.LiaisonType.PlanarContact] = new[] { 1, 5, 6 },
                [ModelData.LiaisonType.RevoluteContact] = new[] { 1, 2, 3, 5, 6 },
                [ModelData.LiaisonType.PrismaticContact] = new[] { 2, 3, 4, 5, 6 },
                [ModelData.LiaisonType.CylindricalContact] = new[] { 2, 3, 5, 6 },
                [ModelData.LiaisonType.SphericalContact] = new[] { 1, 2, 3 },
                [ModelData.LiaisonType.FixedContact] = new[] { 1, 2, 3, 4, 5, 6 },
            };

            public static bool IsPanelAllowed(ModelData.LiaisonType linkage, int panelIndex) =>
                Map.TryGetValue(linkage, out var panels) && panels.Contains(panelIndex);

            public static int FirstAllowedPanel(ModelData.LiaisonType linkage) =>
                Map.TryGetValue(linkage, out var panels) && panels.Length > 0 ? panels[0] : 0;
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
        public double CoordU2
        {
            get => _coordU2;
            set => SetAndDirty(ref _coordU2, value);
        } // Coordonnée U

        public double CoordV2
        {
            get => _coordV2;
            set => SetAndDirty(ref _coordV2, value);
        } // Coordonnée V

        public double CoordW2   
        {
            get => _coordW2;
            set => SetAndDirty(ref _coordW2, value);
        } // Coordonnée W


        #region Parts 


        private int _originePartId;
        public int OriginePartId
        {
            get => _originePartId;
            set => SetAndDirty(ref _originePartId, value);
        }

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



        public LiaisonType Linkage
        {
            get => _linkage;
            set
            {
                if (SetAndDirty(ref _linkage, value))
                {
                    // Sélectionne automatiquement le premier groupe valide pour cette liaison
                    SelectedToleranceGroup = LinkagePanelMap.FirstAllowedPanel(value);
                }
            }
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
            Linkage = db.Linkage;

            #region Proprietes de tolérances
            #region N

            NOriginValue = db.NOriginValue;
            NIntermediateValue = db.NIntermediateValue;
            NExtremityValue = db.NExtremityValue;

            NOriginDescription = db.NOriginDescription;
            NIntermediateDescription = db.NIntermediateDescription;
            NExtremityDescription = db.NExtremityDescription;

            NOriginName = db.NOriginName;
            NIntermediateName = db.NIntermediateName;
            NExtremityName = db.NExtremityName;
            #endregion

            #region T1

            T1OriginValue = db.T1OriginValue;
            T1IntermediateValue = db.T1IntermediateValue;
            T1ExtremityValue = db.T1ExtremityValue;

            T1OriginDescription = db.T1OriginDescription;
            T1IntermediateDescription = db.T1IntermediateDescription;
            T1ExtremityDescription = db.T1ExtremityDescription;
            T1OriginName = db.T1OriginName;
            T1IntermediateName = db.T1IntermediateName;
            T1ExtremityName = db.T1ExtremityName;

            #endregion

            #region T2

            T2OriginValue = db.T2OriginValue;
            T2IntermediateValue = db.T2IntermediateValue;
            T2ExtremityValue = db.T2ExtremityValue;

            T2OriginDescription = db.T2OriginDescription;
            T2IntermediateDescription = db.T2IntermediateDescription;
            T2ExtremityDescription = db.T2ExtremityDescription;
            T2OriginName = db.T2OriginName;
            T2IntermediateName = db.T2IntermediateName;
            T2ExtremityName = db.T2ExtremityName;

            #endregion

            #region Rn

            RnOriginValue = db.RnOriginValue;
            RnIntermediateValue = db.RnIntermediateValue;
            RnExtremityValue = db.RnExtremityValue;

            RnOriginDescription = db.RnOriginDescription;
            RnIntermediateDescription = db.RnIntermediateDescription;
            RnExtremityDescription = db.RnExtremityDescription;
            RnOriginName = db.RnOriginName;
            RnIntermediateName = db.RnIntermediateName;
            RnExtremityName = db.RnExtremityName;

            #endregion

            #region RT1

            RT1OriginValue = db.RT1OriginValue;
            RT1IntermediateValue = db.RT1IntermediateValue;
            RT1ExtremityValue = db.RT1ExtremityValue;

            RT1OriginDescription = db.RT1OriginDescription;
            RT1IntermediateDescription = db.RT1IntermediateDescription;
            RT1ExtremityDescription = db.RT1ExtremityDescription;   
            RT1OriginName = db.RT1OriginName;
            RT1IntermediateName = db.RT1IntermediateName;
            RT1ExtremityName = db.RT1ExtremityName;

            #endregion

            #region RT2

            RT2OriginValue = db.RT2OriginValue;
            RT2IntermediateValue = db.RT2IntermediateValue;
            RT2ExtremityValue = db.RT2ExtremityValue;

            RT2OriginDescription = db.RT2OriginDescription;
            RT2IntermediateDescription = db.RT2IntermediateDescription;
            RT2ExtremityDescription = db.RT2ExtremityDescription;
            RT2OriginName = db.RT2OriginName;
            RT2IntermediateName = db.RT2IntermediateName;
            RT2ExtremityName = db.RT2ExtremityName;

            #endregion
            #endregion

            CheckBoxOri = db.CheckBoxOri;
            CheckBoxInt = db.CheckBoxInt;
            CheckBoxExtre = db.CheckBoxExtre;

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


            // Priorité 1 : le choix précédent de l'utilisateur, s'il est toujours valide
            // pour le type de liaison actuel.
            if (ToleranceGroupSelectionCache.TryGet(Id, out int cachedGroup)
                && LinkagePanelMap.IsPanelAllowed(Linkage, cachedGroup))
            {
                _selectedToleranceGroup = cachedGroup; // affectation directe : pas de re-sauvegarde inutile
                OnPropertyChanged(nameof(SelectedToleranceGroup));
            }
            else
            {
                // Priorité 2 : repli sur le premier groupe valide (première visite, ou choix
                // devenu incompatible suite à un changement de Linkage entretemps en base)
                SelectedToleranceGroup = LinkagePanelMap.FirstAllowedPanel(Linkage);
            }

        }

        #endregion



    }

}
