using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using Toltech.App.Models;
using static Toltech.App.Models.NodesDefinition;
using static Toltech.App.Services.EventsManager;

namespace Toltech.App.Services
{
    public class MetaModelSyncService
    {
        private readonly MetaModelDatabaseService _metaDb;
        private readonly DatabaseService _databaseService;

        public event Action<ModelMetaChangedEvent>? MetaChanged;

        public MetaModelSyncService(MetaModelDatabaseService metaDb, DatabaseService databaseService)
        {
            _metaDb = metaDb;
            _databaseService = databaseService;
            EventsManager.PartCrud += OnPartCrudAsync;
            EventsManager.RequirementCrud += OnRequirementCrudAsync;
        }

        // ── Parts ────────────────────────────────────────────────────────

        private async Task OnPartCrudAsync(PartCrudEvent e)
        {
            Guid idModel = await _databaseService.GetModelIdAsync();
            int numberPart = await _databaseService.GetPartsCountAsync();

            switch (e.Operation)
            {
                case CrudOperation.Added:
                case CrudOperation.Deleted:
                    await _metaDb.UpdatePartCountAsync(idModel, numberPart);
                    break;

                case CrudOperation.Updated:
                    break;
            }

            MetaChanged?.Invoke(new ModelMetaChangedEvent
            {
                ModelId = idModel,
                PartCount = numberPart,
            });
        }

        // ── Requirements ─────────────────────────────────────────────────

        private async Task OnRequirementCrudAsync(RequirementCrudEvent e)
        {
            Guid idModel = await _databaseService.GetModelIdAsync();
            int numberReq = await _databaseService.GetNumberReqAsync();

            switch (e.Operation)
            {
                case CrudOperation.Added:
                case CrudOperation.Deleted:
                    await _metaDb.UpdateRequirementCountAsync(idModel, numberReq);
                    break;

                case CrudOperation.Updated:
                    break;  
            }

            MetaChanged?.Invoke(new ModelMetaChangedEvent
            {
                ModelId = idModel,
                ReqCount = numberReq,
            });
        }

        // ── ModelData ────────────────────────────────────────────────────

        //private async Task OnModelDataCrudAsync(ModelDataCrudEvent e)
        //{
        //    switch (e.Operation)
        //    {
        //        case CrudOperation.Added:
        //            var datas = e.Entities?.Any() == true
        //                ? e.Entities : new List<ModelData> { e.Entity };
        //            await _metaDb.InsertRangeAsync(datas);
        //            break;
        //        case CrudOperation.Deleted:
        //            var ids = e.EntityIds?.Any() == true
        //                ? e.EntityIds : new List<int> { e.EntityId };
        //            await _metaDb.DeleteModelDataByIdsAsync(ids);
        //            break;
        //        case CrudOperation.Updated:
        //            await _metaDb.UpdateAsync(e.Entity);
        //            break;
        //    }
        //}

        // ── Rename depuis le tree ────────────────────────────────────────

        //private async Task OnNodeChangedAsync(NodeChangedEvent e)
        //{
        //    if (e.Operation != CrudOperation.Updated) return;

        //    switch (e.Type)
        //    {
        //        case NodeType.PartNode:
        //            await _metaDb.UpdatePartNameAsync(e.LinkedOriginalId, e.NewName);
        //            break;
        //        case NodeType.RequirementNode:
        //            await _metaDb.UpdateRequirementNameAsync(
        //                e.LinkedRequirementId ?? 0, e.NewName);
        //            break;
        //        case NodeType.DataNode:
        //            await _metaDb.UpdateModelDataNameAsync(e.LinkedOriginalId, e.NewName);
        //            break;
        //    }
        //}

        public void Dispose()
        {
            EventsManager.PartCrud -= OnPartCrudAsync;
            EventsManager.RequirementCrud -= OnRequirementCrudAsync;
        }
    }
}
