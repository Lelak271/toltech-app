using System;
using System.Runtime.CompilerServices;
using SQLite;
using Toltech.App.ViewModels;

namespace Toltech.App.Models
{
    public partial class ModelData
    {

        [Ignore]
        public IReadOnlyList<ToleranceGroupViewModel> ToleranceGroups { get; }


        #region N

        // ==================== N - Origine ====================
        private double _nOriginValue;
        public double NOriginValue
        {
            get => _nOriginValue;
            set => SetAndDirty(ref _nOriginValue, value);
        } // Valeur de la tolérance (N - Origine)

        private string _nOriginDescription = string.Empty;
        public string NOriginDescription
        {
            get => _nOriginDescription;
            set => SetAndDirty(ref _nOriginDescription, value);
        } // Description de la tolérance (N - Origine)

        private string _nOriginName = string.Empty;
        public string NOriginName
        {
            get => _nOriginName;
            set => SetAndDirty(ref _nOriginName, value);
        } // Nom de la tolérance (N - Origine)

        private bool _nOriginUseDatabase;
        public bool NOriginUseDatabase
        {
            get => _nOriginUseDatabase;
            set => SetAndDirty(ref _nOriginUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (N - Origine)

        private int _nOriginToleranceId;
        public int NOriginToleranceId
        {
            get => _nOriginToleranceId;
            set => SetAndDirty(ref _nOriginToleranceId, value);
        } // Identifiant de la tolérance (N - Origine)

        // ==================== N - Intermédiaire ====================
        private double _nIntermediateValue;
        public double NIntermediateValue
        {
            get => _nIntermediateValue;
            set => SetAndDirty(ref _nIntermediateValue, value);
        } // Valeur de la tolérance (N - Intermédiaire)

        private string _nIntermediateDescription = string.Empty;
        public string NIntermediateDescription
        {
            get => _nIntermediateDescription;
            set => SetAndDirty(ref _nIntermediateDescription, value);
        } // Description de la tolérance (N - Intermédiaire)

        private string _nIntermediateName = string.Empty;
        public string NIntermediateName
        {
            get => _nIntermediateName;
            set => SetAndDirty(ref _nIntermediateName, value);
        } // Nom de la tolérance (N - Intermédiaire)

        private bool _nIntermediateUseDatabase;
        public bool NIntermediateUseDatabase
        {
            get => _nIntermediateUseDatabase;
            set => SetAndDirty(ref _nIntermediateUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (N - Intermédiaire)

        private int _nIntermediateToleranceId;
        public int NIntermediateToleranceId
        {
            get => _nIntermediateToleranceId;
            set => SetAndDirty(ref _nIntermediateToleranceId, value);
        } // Identifiant de la tolérance (N - Intermédiaire)

        // ==================== N - Extrémité ====================
        private double _nExtremityValue;
        public double NExtremityValue
        {
            get => _nExtremityValue;
            set => SetAndDirty(ref _nExtremityValue, value);
        } // Valeur de la tolérance (N - Extrémité)

        private string _nExtremityDescription = string.Empty;
        public string NExtremityDescription
        {
            get => _nExtremityDescription;
            set => SetAndDirty(ref _nExtremityDescription, value);
        } // Description de la tolérance (N - Extrémité)

        private string _nExtremityName = string.Empty;
        public string NExtremityName
        {
            get => _nExtremityName;
            set => SetAndDirty(ref _nExtremityName, value);
        } // Nom de la tolérance (N - Extrémité)

        private bool _nExtremityUseDatabase;
        public bool NExtremityUseDatabase
        {
            get => _nExtremityUseDatabase;
            set => SetAndDirty(ref _nExtremityUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (N - Extrémité)

        private int _nExtremityToleranceId;
        public int NExtremityToleranceId
        {
            get => _nExtremityToleranceId;
            set => SetAndDirty(ref _nExtremityToleranceId, value);
        } // Identifiant de la tolérance (N - Extrémité)

        #endregion

        #region T1

        // ==================== T1 - Origine ====================
        private double _t1OriginValue;
        public double T1OriginValue
        {
            get => _t1OriginValue;
            set => SetAndDirty(ref _t1OriginValue, value);
        } // Valeur de la tolérance (T1 - Origine)

        private string _t1OriginDescription = string.Empty;
        public string T1OriginDescription
        {
            get => _t1OriginDescription;
            set => SetAndDirty(ref _t1OriginDescription, value);
        } // Description de la tolérance (T1 - Origine)

        private string _t1OriginName = string.Empty;
        public string T1OriginName
        {
            get => _t1OriginName;
            set => SetAndDirty(ref _t1OriginName, value);
        } // Nom de la tolérance (T1 - Origine)

        private bool _t1OriginUseDatabase;
        public bool T1OriginUseDatabase
        {
            get => _t1OriginUseDatabase;
            set => SetAndDirty(ref _t1OriginUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (T1 - Origine)

        private int _t1OriginToleranceId;
        public int T1OriginToleranceId
        {
            get => _t1OriginToleranceId;
            set => SetAndDirty(ref _t1OriginToleranceId, value);
        } // Identifiant de la tolérance (T1 - Origine)

        // ==================== T1 - Intermédiaire ====================
        private double _t1IntermediateValue;
        public double T1IntermediateValue
        {
            get => _t1IntermediateValue;
            set => SetAndDirty(ref _t1IntermediateValue, value);
        } // Valeur de la tolérance (T1 - Intermédiaire)

        private string _t1IntermediateDescription = string.Empty;
        public string T1IntermediateDescription
        {
            get => _t1IntermediateDescription;
            set => SetAndDirty(ref _t1IntermediateDescription, value);
        } // Description de la tolérance (T1 - Intermédiaire)

        private string _t1IntermediateName = string.Empty;
        public string T1IntermediateName
        {
            get => _t1IntermediateName;
            set => SetAndDirty(ref _t1IntermediateName, value);
        } // Nom de la tolérance (T1 - Intermédiaire)

        private bool _t1IntermediateUseDatabase;
        public bool T1IntermediateUseDatabase
        {
            get => _t1IntermediateUseDatabase;
            set => SetAndDirty(ref _t1IntermediateUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (T1 - Intermédiaire)

        private int _t1IntermediateToleranceId;
        public int T1IntermediateToleranceId
        {
            get => _t1IntermediateToleranceId;
            set => SetAndDirty(ref _t1IntermediateToleranceId, value);
        } // Identifiant de la tolérance (T1 - Intermédiaire)

        // ==================== T1 - Extrémité ====================
        private double _t1ExtremityValue;
        public double T1ExtremityValue
        {
            get => _t1ExtremityValue;
            set => SetAndDirty(ref _t1ExtremityValue, value);
        } // Valeur de la tolérance (T1 - Extrémité)

        private string _t1ExtremityDescription = string.Empty;
        public string T1ExtremityDescription
        {
            get => _t1ExtremityDescription;
            set => SetAndDirty(ref _t1ExtremityDescription, value);
        } // Description de la tolérance (T1 - Extrémité)

        private string _t1ExtremityName = string.Empty;
        public string T1ExtremityName
        {
            get => _t1ExtremityName;
            set => SetAndDirty(ref _t1ExtremityName, value);
        } // Nom de la tolérance (T1 - Extrémité)

        private bool _t1ExtremityUseDatabase;
        public bool T1ExtremityUseDatabase
        {
            get => _t1ExtremityUseDatabase;
            set => SetAndDirty(ref _t1ExtremityUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (T1 - Extrémité)

        private int _t1ExtremityToleranceId;
        public int T1ExtremityToleranceId
        {
            get => _t1ExtremityToleranceId;
            set => SetAndDirty(ref _t1ExtremityToleranceId, value);
        } // Identifiant de la tolérance (T1 - Extrémité)

        #endregion

        #region T2

        // ==================== T2 - Origine ====================
        private double _t2OriginValue;
        public double T2OriginValue
        {
            get => _t2OriginValue;
            set => SetAndDirty(ref _t2OriginValue, value);
        } // Valeur de la tolérance (T2 - Origine)

        private string _t2OriginDescription = string.Empty;
        public string T2OriginDescription
        {
            get => _t2OriginDescription;
            set => SetAndDirty(ref _t2OriginDescription, value);
        } // Description de la tolérance (T2 - Origine)

        private string _t2OriginName = string.Empty;
        public string T2OriginName
        {
            get => _t2OriginName;
            set => SetAndDirty(ref _t2OriginName, value);
        } // Nom de la tolérance (T2 - Origine)

        private bool _t2OriginUseDatabase;
        public bool T2OriginUseDatabase
        {
            get => _t2OriginUseDatabase;
            set => SetAndDirty(ref _t2OriginUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (T2 - Origine)

        private int _t2OriginToleranceId;
        public int T2OriginToleranceId
        {
            get => _t2OriginToleranceId;
            set => SetAndDirty(ref _t2OriginToleranceId, value);
        } // Identifiant de la tolérance (T2 - Origine)

        // ==================== T2 - Intermédiaire ====================
        private double _t2IntermediateValue;
        public double T2IntermediateValue
        {
            get => _t2IntermediateValue;
            set => SetAndDirty(ref _t2IntermediateValue, value);
        } // Valeur de la tolérance (T2 - Intermédiaire)

        private string _t2IntermediateDescription = string.Empty;
        public string T2IntermediateDescription
        {
            get => _t2IntermediateDescription;
            set => SetAndDirty(ref _t2IntermediateDescription, value);
        } // Description de la tolérance (T2 - Intermédiaire)

        private string _t2IntermediateName = string.Empty;
        public string T2IntermediateName
        {
            get => _t2IntermediateName;
            set => SetAndDirty(ref _t2IntermediateName, value);
        } // Nom de la tolérance (T2 - Intermédiaire)

        private bool _t2IntermediateUseDatabase;
        public bool T2IntermediateUseDatabase
        {
            get => _t2IntermediateUseDatabase;
            set => SetAndDirty(ref _t2IntermediateUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (T2 - Intermédiaire)

        private int _t2IntermediateToleranceId;
        public int T2IntermediateToleranceId
        {
            get => _t2IntermediateToleranceId;
            set => SetAndDirty(ref _t2IntermediateToleranceId, value);
        } // Identifiant de la tolérance (T2 - Intermédiaire)

        // ==================== T2 - Extrémité ====================
        private double _t2ExtremityValue;
        public double T2ExtremityValue
        {
            get => _t2ExtremityValue;
            set => SetAndDirty(ref _t2ExtremityValue, value);
        } // Valeur de la tolérance (T2 - Extrémité)

        private string _t2ExtremityDescription = string.Empty;
        public string T2ExtremityDescription
        {
            get => _t2ExtremityDescription;
            set => SetAndDirty(ref _t2ExtremityDescription, value);
        } // Description de la tolérance (T2 - Extrémité)

        private string _t2ExtremityName = string.Empty;
        public string T2ExtremityName
        {
            get => _t2ExtremityName;
            set => SetAndDirty(ref _t2ExtremityName, value);
        } // Nom de la tolérance (T2 - Extrémité)

        private bool _t2ExtremityUseDatabase;
        public bool T2ExtremityUseDatabase
        {
            get => _t2ExtremityUseDatabase;
            set => SetAndDirty(ref _t2ExtremityUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (T2 - Extrémité)

        private int _t2ExtremityToleranceId;
        public int T2ExtremityToleranceId
        {
            get => _t2ExtremityToleranceId;
            set => SetAndDirty(ref _t2ExtremityToleranceId, value);
        } // Identifiant de la tolérance (T2 - Extrémité)

        #endregion

        #region Rn

        // ==================== Rn - Origine ====================
        private double _rnOriginValue;
        public double RnOriginValue
        {
            get => _rnOriginValue;
            set => SetAndDirty(ref _rnOriginValue, value);
        } // Valeur de la tolérance (Rn - Origine)

        private string _rnOriginDescription = string.Empty;
        public string RnOriginDescription
        {
            get => _rnOriginDescription;
            set => SetAndDirty(ref _rnOriginDescription, value);
        } // Description de la tolérance (Rn - Origine)

        private string _rnOriginName = string.Empty;
        public string RnOriginName
        {
            get => _rnOriginName;
            set => SetAndDirty(ref _rnOriginName, value);
        } // Nom de la tolérance (Rn - Origine)

        private bool _rnOriginUseDatabase;
        public bool RnOriginUseDatabase
        {
            get => _rnOriginUseDatabase;
            set => SetAndDirty(ref _rnOriginUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (Rn - Origine)

        private int _rnOriginToleranceId;
        public int RnOriginToleranceId
        {
            get => _rnOriginToleranceId;
            set => SetAndDirty(ref _rnOriginToleranceId, value);
        } // Identifiant de la tolérance (Rn - Origine)

        // ==================== Rn - Intermédiaire ====================
        private double _rnIntermediateValue;
        public double RnIntermediateValue
        {
            get => _rnIntermediateValue;
            set => SetAndDirty(ref _rnIntermediateValue, value);
        } // Valeur de la tolérance (Rn - Intermédiaire)

        private string _rnIntermediateDescription = string.Empty;
        public string RnIntermediateDescription
        {
            get => _rnIntermediateDescription;
            set => SetAndDirty(ref _rnIntermediateDescription, value);
        } // Description de la tolérance (Rn - Intermédiaire)

        private string _rnIntermediateName = string.Empty;
        public string RnIntermediateName
        {
            get => _rnIntermediateName;
            set => SetAndDirty(ref _rnIntermediateName, value);
        } // Nom de la tolérance (Rn - Intermédiaire)

        private bool _rnIntermediateUseDatabase;
        public bool RnIntermediateUseDatabase
        {
            get => _rnIntermediateUseDatabase;
            set => SetAndDirty(ref _rnIntermediateUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (Rn - Intermédiaire)

        private int _rnIntermediateToleranceId;
        public int RnIntermediateToleranceId
        {
            get => _rnIntermediateToleranceId;
            set => SetAndDirty(ref _rnIntermediateToleranceId, value);
        } // Identifiant de la tolérance (Rn - Intermédiaire)

        // ==================== Rn - Extrémité ====================
        private double _rnExtremityValue;
        public double RnExtremityValue
        {
            get => _rnExtremityValue;
            set => SetAndDirty(ref _rnExtremityValue, value);
        } // Valeur de la tolérance (Rn - Extrémité)

        private string _rnExtremityDescription = string.Empty;
        public string RnExtremityDescription
        {
            get => _rnExtremityDescription;
            set => SetAndDirty(ref _rnExtremityDescription, value);
        } // Description de la tolérance (Rn - Extrémité)

        private string _rnExtremityName = string.Empty;
        public string RnExtremityName
        {
            get => _rnExtremityName;
            set => SetAndDirty(ref _rnExtremityName, value);
        } // Nom de la tolérance (Rn - Extrémité)

        private bool _rnExtremityUseDatabase;
        public bool RnExtremityUseDatabase
        {
            get => _rnExtremityUseDatabase;
            set => SetAndDirty(ref _rnExtremityUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (Rn - Extrémité)

        private int _rnExtremityToleranceId;
        public int RnExtremityToleranceId
        {
            get => _rnExtremityToleranceId;
            set => SetAndDirty(ref _rnExtremityToleranceId, value);
        } // Identifiant de la tolérance (Rn - Extrémité)

        #endregion

        #region RT1

        // ==================== RT1 - Origine ====================
        private double _rt1OriginValue;
        public double RT1OriginValue
        {
            get => _rt1OriginValue;
            set => SetAndDirty(ref _rt1OriginValue, value);
        } // Valeur de la tolérance (RT1 - Origine)

        private string _rt1OriginDescription = string.Empty;
        public string RT1OriginDescription
        {
            get => _rt1OriginDescription;
            set => SetAndDirty(ref _rt1OriginDescription, value);
        } // Description de la tolérance (RT1 - Origine)

        private string _rt1OriginName = string.Empty;
        public string RT1OriginName
        {
            get => _rt1OriginName;
            set => SetAndDirty(ref _rt1OriginName, value);
        } // Nom de la tolérance (RT1 - Origine)

        private bool _rt1OriginUseDatabase;
        public bool RT1OriginUseDatabase
        {
            get => _rt1OriginUseDatabase;
            set => SetAndDirty(ref _rt1OriginUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (RT1 - Origine)

        private int _rt1OriginToleranceId;
        public int RT1OriginToleranceId
        {
            get => _rt1OriginToleranceId;
            set => SetAndDirty(ref _rt1OriginToleranceId, value);
        } // Identifiant de la tolérance (RT1 - Origine)

        // ==================== RT1 - Intermédiaire ====================
        private double _rt1IntermediateValue;
        public double RT1IntermediateValue
        {
            get => _rt1IntermediateValue;
            set => SetAndDirty(ref _rt1IntermediateValue, value);
        } // Valeur de la tolérance (RT1 - Intermédiaire)

        private string _rt1IntermediateDescription = string.Empty;
        public string RT1IntermediateDescription
        {
            get => _rt1IntermediateDescription;
            set => SetAndDirty(ref _rt1IntermediateDescription, value);
        } // Description de la tolérance (RT1 - Intermédiaire)

        private string _rt1IntermediateName = string.Empty;
        public string RT1IntermediateName
        {
            get => _rt1IntermediateName;
            set => SetAndDirty(ref _rt1IntermediateName, value);
        } // Nom de la tolérance (RT1 - Intermédiaire)

        private bool _rt1IntermediateUseDatabase;
        public bool RT1IntermediateUseDatabase
        {
            get => _rt1IntermediateUseDatabase;
            set => SetAndDirty(ref _rt1IntermediateUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (RT1 - Intermédiaire)

        private int _rt1IntermediateToleranceId;
        public int RT1IntermediateToleranceId
        {
            get => _rt1IntermediateToleranceId;
            set => SetAndDirty(ref _rt1IntermediateToleranceId, value);
        } // Identifiant de la tolérance (RT1 - Intermédiaire)

        // ==================== RT1 - Extrémité ====================
        private double _rt1ExtremityValue;
        public double RT1ExtremityValue
        {
            get => _rt1ExtremityValue;
            set => SetAndDirty(ref _rt1ExtremityValue, value);
        } // Valeur de la tolérance (RT1 - Extrémité)

        private string _rt1ExtremityDescription = string.Empty;
        public string RT1ExtremityDescription
        {
            get => _rt1ExtremityDescription;
            set => SetAndDirty(ref _rt1ExtremityDescription, value);
        } // Description de la tolérance (RT1 - Extrémité)

        private string _rt1ExtremityName = string.Empty;
        public string RT1ExtremityName
        {
            get => _rt1ExtremityName;
            set => SetAndDirty(ref _rt1ExtremityName, value);
        } // Nom de la tolérance (RT1 - Extrémité)

        private bool _rt1ExtremityUseDatabase;
        public bool RT1ExtremityUseDatabase
        {
            get => _rt1ExtremityUseDatabase;
            set => SetAndDirty(ref _rt1ExtremityUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (RT1 - Extrémité)

        private int _rt1ExtremityToleranceId;
        public int RT1ExtremityToleranceId
        {
            get => _rt1ExtremityToleranceId;
            set => SetAndDirty(ref _rt1ExtremityToleranceId, value);
        } // Identifiant de la tolérance (RT1 - Extrémité)

        #endregion

        #region RT2

        // ==================== RT2 - Origine ====================
        private double _rt2OriginValue;
        public double RT2OriginValue
        {
            get => _rt2OriginValue;
            set => SetAndDirty(ref _rt2OriginValue, value);
        } // Valeur de la tolérance (RT2 - Origine)

        private string _rt2OriginDescription = string.Empty;
        public string RT2OriginDescription
        {
            get => _rt2OriginDescription;
            set => SetAndDirty(ref _rt2OriginDescription, value);
        } // Description de la tolérance (RT2 - Origine)

        private string _rt2OriginName = string.Empty;
        public string RT2OriginName
        {
            get => _rt2OriginName;
            set => SetAndDirty(ref _rt2OriginName, value);
        } // Nom de la tolérance (RT2 - Origine)

        private bool _rt2OriginUseDatabase;
        public bool RT2OriginUseDatabase
        {
            get => _rt2OriginUseDatabase;
            set => SetAndDirty(ref _rt2OriginUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (RT2 - Origine)

        private int _rt2OriginToleranceId;
        public int RT2OriginToleranceId
        {
            get => _rt2OriginToleranceId;
            set => SetAndDirty(ref _rt2OriginToleranceId, value);
        } // Identifiant de la tolérance (RT2 - Origine)

        // ==================== RT2 - Intermédiaire ====================
        private double _rt2IntermediateValue;
        public double RT2IntermediateValue
        {
            get => _rt2IntermediateValue;
            set => SetAndDirty(ref _rt2IntermediateValue, value);
        } // Valeur de la tolérance (RT2 - Intermédiaire)

        private string _rt2IntermediateDescription = string.Empty;
        public string RT2IntermediateDescription
        {
            get => _rt2IntermediateDescription;
            set => SetAndDirty(ref _rt2IntermediateDescription, value);
        } // Description de la tolérance (RT2 - Intermédiaire)

        private string _rt2IntermediateName = string.Empty;
        public string RT2IntermediateName
        {
            get => _rt2IntermediateName;
            set => SetAndDirty(ref _rt2IntermediateName, value);
        } // Nom de la tolérance (RT2 - Intermédiaire)

        private bool _rt2IntermediateUseDatabase;
        public bool RT2IntermediateUseDatabase
        {
            get => _rt2IntermediateUseDatabase;
            set => SetAndDirty(ref _rt2IntermediateUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (RT2 - Intermédiaire)

        private int _rt2IntermediateToleranceId;
        public int RT2IntermediateToleranceId
        {
            get => _rt2IntermediateToleranceId;
            set => SetAndDirty(ref _rt2IntermediateToleranceId, value);
        } // Identifiant de la tolérance (RT2 - Intermédiaire)

        // ==================== RT2 - Extrémité ====================
        private double _rt2ExtremityValue;
        public double RT2ExtremityValue
        {
            get => _rt2ExtremityValue;
            set => SetAndDirty(ref _rt2ExtremityValue, value);
        } // Valeur de la tolérance (RT2 - Extrémité)

        private string _rt2ExtremityDescription = string.Empty;
        public string RT2ExtremityDescription
        {
            get => _rt2ExtremityDescription;
            set => SetAndDirty(ref _rt2ExtremityDescription, value);
        } // Description de la tolérance (RT2 - Extrémité)

        private string _rt2ExtremityName = string.Empty;
        public string RT2ExtremityName
        {
            get => _rt2ExtremityName;
            set => SetAndDirty(ref _rt2ExtremityName, value);
        } // Nom de la tolérance (RT2 - Extrémité)

        private bool _rt2ExtremityUseDatabase;
        public bool RT2ExtremityUseDatabase
        {
            get => _rt2ExtremityUseDatabase;
            set => SetAndDirty(ref _rt2ExtremityUseDatabase, value);
        } // Indique si la tolérance provient de la base de données (RT2 - Extrémité)

        private int _rt2ExtremityToleranceId;
        public int RT2ExtremityToleranceId
        {
            get => _rt2ExtremityToleranceId;
            set => SetAndDirty(ref _rt2ExtremityToleranceId, value);
        } // Identifiant de la tolérance (RT2 - Extrémité)

        #endregion

        #region Tolerances Types
        public ToleranceGroupViewModel N { get; private set; }
        public ToleranceGroupViewModel T1 { get; private set; }

        public ToleranceGroupViewModel T2 { get; private set; }

        public ToleranceGroupViewModel Rn { get; private set; }

        public ToleranceGroupViewModel RT1 { get; private set; }

        public ToleranceGroupViewModel RT2 { get; private set; }

        /// <summary>
        /// Initializes tolerance groups for categories N, T1, T2, Rn, RT1, and RT2, each with extremity, intermediate,
        /// and origin slots.
        /// </summary>
        private void CreateToleranceGroups()
        {
            // =====================================================
            // N
            // =====================================================

            N = new ToleranceGroupViewModel(

                new ToleranceSlotViewModel(
                    this,
                    x => x.NExtremityName,
                    x => x.NExtremityDescription,
                    x => x.NExtremityValue,
                    x => x.NExtremityToleranceId,
                    x => x.NExtremityUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.NIntermediateName,
                    x => x.NIntermediateDescription,
                    x => x.NIntermediateValue,
                    x => x.NIntermediateToleranceId,
                    x => x.NIntermediateUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.NOriginName,
                    x => x.NOriginDescription,
                    x => x.NOriginValue,
                    x => x.NOriginToleranceId,
                    x => x.NOriginUseDatabase));

            // =====================================================
            // T1
            // =====================================================

            T1 = new ToleranceGroupViewModel(

                new ToleranceSlotViewModel(
                    this,
                    x => x.T1ExtremityName,
                    x => x.T1ExtremityDescription,
                    x => x.T1ExtremityValue,
                    x => x.T1ExtremityToleranceId,
                    x => x.T1ExtremityUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.T1IntermediateName,
                    x => x.T1IntermediateDescription,
                    x => x.T1IntermediateValue,
                    x => x.T1IntermediateToleranceId,
                    x => x.T1IntermediateUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.T1OriginName,
                    x => x.T1OriginDescription,
                    x => x.T1OriginValue,
                    x => x.T1OriginToleranceId,
                    x => x.T1OriginUseDatabase));

            // =====================================================
            // T2
            // =====================================================

            T2 = new ToleranceGroupViewModel(

                new ToleranceSlotViewModel(
                    this,
                    x => x.T2ExtremityName,
                    x => x.T2ExtremityDescription,
                    x => x.T2ExtremityValue,
                    x => x.T2ExtremityToleranceId,
                    x => x.T2ExtremityUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.T2IntermediateName,
                    x => x.T2IntermediateDescription,
                    x => x.T2IntermediateValue,
                    x => x.T2IntermediateToleranceId,
                    x => x.T2IntermediateUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.T2OriginName,
                    x => x.T2OriginDescription,
                    x => x.T2OriginValue,
                    x => x.T2OriginToleranceId,
                    x => x.T2OriginUseDatabase));

            // =====================================================
            // Rn
            // =====================================================

            Rn = new ToleranceGroupViewModel(

                new ToleranceSlotViewModel(
                    this,
                    x => x.RnExtremityName,
                    x => x.RnExtremityDescription,
                    x => x.RnExtremityValue,
                    x => x.RnExtremityToleranceId,
                    x => x.RnExtremityUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.RnIntermediateName,
                    x => x.RnIntermediateDescription,
                    x => x.RnIntermediateValue,
                    x => x.RnIntermediateToleranceId,
                    x => x.RnIntermediateUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.RnOriginName,
                    x => x.RnOriginDescription,
                    x => x.RnOriginValue,
                    x => x.RnOriginToleranceId,
                    x => x.RnOriginUseDatabase));

            // =====================================================
            // RT1
            // =====================================================

            RT1 = new ToleranceGroupViewModel(

                new ToleranceSlotViewModel(
                    this,
                    x => x.RT1ExtremityName,
                    x => x.RT1ExtremityDescription,
                    x => x.RT1ExtremityValue,
                    x => x.RT1ExtremityToleranceId,
                    x => x.RT1ExtremityUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.RT1IntermediateName,
                    x => x.RT1IntermediateDescription,
                    x => x.RT1IntermediateValue,
                    x => x.RT1IntermediateToleranceId,
                    x => x.RT1IntermediateUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.RT1OriginName,
                    x => x.RT1OriginDescription,
                    x => x.RT1OriginValue,
                    x => x.RT1OriginToleranceId,
                    x => x.RT1OriginUseDatabase));

            // =====================================================
            // RT2
            // =====================================================

            RT2 = new ToleranceGroupViewModel(

                new ToleranceSlotViewModel(
                    this,
                    x => x.RT2ExtremityName,
                    x => x.RT2ExtremityDescription,
                    x => x.RT2ExtremityValue,
                    x => x.RT2ExtremityToleranceId,
                    x => x.RT2ExtremityUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.RT2IntermediateName,
                    x => x.RT2IntermediateDescription,
                    x => x.RT2IntermediateValue,
                    x => x.RT2IntermediateToleranceId,
                    x => x.RT2IntermediateUseDatabase),

                new ToleranceSlotViewModel(
                    this,
                    x => x.RT2OriginName,
                    x => x.RT2OriginDescription,
                    x => x.RT2OriginValue,
                    x => x.RT2OriginToleranceId,
                    x => x.RT2OriginUseDatabase));
        }

        #endregion

    }


}