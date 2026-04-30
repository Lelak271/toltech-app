using System;
using System.Runtime.CompilerServices;
using SQLite;
using Toltech.App.ViewModels;

namespace Toltech.App.Models
{
    public class ModelMeta : BaseViewModel
    {
        #region Champs DB (non dirty)

        [PrimaryKey, AutoIncrement]
        public Guid IdModel { get; set; }

        public DateTime CreatedAtmodel { get; set; }

        public DateTime LastModified { get; set; }

        #endregion

        #region Backing fields

        private string _nameData;
        private string _filePathModel;
        private string _descriptionModel;
        private byte[] _imageData;
        private string _modelStatus;
        private int _partCount;
        private int _requirementCount;
        private string _responsibleEngineer;
        private string _softwareVersion;
        private bool _isLocked;
        private double _optionMM1;
        private double _optionMM2;

        #endregion

        #region Propriétés éditables (dirty-aware)

        public string NameData
        {
            get => _nameData;
            set => SetAndDirty(ref _nameData, value);
        }

        public string FilePathModel
        {
            get => _filePathModel;
            set => SetAndDirty(ref _filePathModel, value);
        }

        public string DescriptionModel
        {
            get => _descriptionModel;
            set => SetAndDirty(ref _descriptionModel, value);
        }

        public byte[] ImageData
        {
            get => _imageData;
            set => SetAndDirty(ref _imageData, value);
        }

        public string ModelStatus
        {
            get => _modelStatus;
            set => SetAndDirty(ref _modelStatus, value);
        }

        public int PartCount
        {
            get => _partCount;
            set => SetAndDirty(ref _partCount, value);
        }

        public int RequirementCount
        {
            get => _requirementCount;
            set => SetAndDirty(ref _requirementCount, value);
        }

        public string ResponsibleEngineer
        {
            get => _responsibleEngineer;
            set => SetAndDirty(ref _responsibleEngineer, value);
        }

        public string SoftwareVersion
        {
            get => _softwareVersion;
            set => SetAndDirty(ref _softwareVersion, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => SetAndDirty(ref _isLocked, value);
        }

        public double OptionMM1
        {
            get => _optionMM1;
            set => SetAndDirty(ref _optionMM1, value);
        }

        public double OptionMM2
        {
            get => _optionMM2;
            set => SetAndDirty(ref _optionMM2, value);
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

        public void LoadFromDb(ModelMeta db)
        {
            if (db == null)
                return;

            BeginLoad();

            IdModel = db.IdModel;
            NameData = db.NameData;
            DescriptionModel = db.DescriptionModel;
            ImageData = db.ImageData;
            FilePathModel = db.FilePathModel;
            ModelStatus = db.ModelStatus;
            PartCount = db.PartCount;
            RequirementCount = db.RequirementCount;
            ResponsibleEngineer = db.ResponsibleEngineer;
            SoftwareVersion = db.SoftwareVersion;
            IsLocked = db.IsLocked;
            OptionMM1 = db.OptionMM1;
            OptionMM2 = db.OptionMM2;
            LastModified = db.LastModified;

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
