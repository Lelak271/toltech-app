using System.Diagnostics;
using Toltech.App.Models;
using static Toltech.App.Models.NodesDefinition;
using static Toltech.App.Services.EventsManager;

namespace Toltech.App.Services
{
    public static class EventsManager
    {
        // ── Events signal (sans payload) ────────────────────────────────────
        public static event Func<Task> ModelOpen;
        public static event Func<Task> ModelDelete;

        // ── Events avec payload ──────────────────────────────────────────────
        public static event Func<RequirementEvent, Task> RequirementVisibilityChanged;
        public static event Func<RequirementEvent, Task> RequirementSelectChanged;
        public static event Func<int?, Task> PartSelectedChanged;

        // ── Events CRUD ──────────────────────────────────────────────────────
        public static event Func<PartCrudEvent, Task> PartCrud;
        public static event Func<RequirementCrudEvent, Task> RequirementCrud;
        public static event Func<ModelDataCrudEvent, Task> ModelDataCrud;

        // ── Raise methods ────────────────────────────────────────────────────
        public static Task RaiseModelOpenAsync() => InvokeAsync(ModelOpen);
        public static Task RaiseModelDeleteAsync() => InvokeAsync(ModelDelete);

        public static Task RaiseRequirementVisibilityChangedAsync(IEnumerable<int> reqIds, string nameParentFolder)
            => InvokeAsync(RequirementVisibilityChanged, BuildRequirementEvent(reqIds, nameParentFolder));

        public static Task RaiseRequirementSelectChangedAsync(IEnumerable<int> reqIds, string nameParentFolder)
            => InvokeAsync(RequirementSelectChanged, BuildRequirementEvent(reqIds, nameParentFolder));

        public static Task RaisePartSelectedChangedAsync(int? idPart)
            => InvokeAsync(PartSelectedChanged, idPart);

        public static Task RaisePartCrudAsync(PartCrudEvent e) => InvokeAsync(PartCrud, e);
        public static Task RaiseRequirementCrudAsync(RequirementCrudEvent e) => InvokeAsync(RequirementCrud, e);
        public static Task RaiseModelDataCrudAsync(ModelDataCrudEvent e) => InvokeAsync(ModelDataCrud, e);

        // ── Payloads ─────────────────────────────────────────────────────────
        public class RequirementEvent
        {
            public IReadOnlyCollection<int> RequirementIds { get; init; }
            public string NameParentFolder { get; init; }
        }

        public enum EventSource { Tree, Data, Req, Part, Model }

        public enum CrudOperation { Added, Updated, Deleted }

        public class CrudEvent<T>
        {
            public CrudOperation Operation { get; init; }
            public T Entity { get; init; }                    // modification unitaire
            public IReadOnlyCollection<T> Entities { get; init; }  // modification en masse
            public IReadOnlyCollection<int> EntityIds { get; init; } // delete en masse
            public int EntityId { get; init; }                // delete unitaire
            public int? ParentId { get; init; }
            public EventSource Source { get; init; }

        }

        public class RequirementCrudEvent : CrudEvent<Requirements> { }
        public class PartCrudEvent : CrudEvent<Part> { }
        public class ModelDataCrudEvent : CrudEvent<ModelData> { }

        // ── Helpers privés ───────────────────────────────────────────────────
        private static RequirementEvent BuildRequirementEvent(IEnumerable<int> reqIds, string nameParentFolder)
            => new RequirementEvent
            {
                RequirementIds = reqIds?.ToList() ?? new List<int>(),
                NameParentFolder = nameParentFolder
            };

        private static async Task InvokeAsync(Func<Task> eventDelegate)
        {
            if (eventDelegate == null) return;
            foreach (var handler in eventDelegate.GetInvocationList())
            {
                try { await ((Func<Task>)handler).Invoke(); }
                catch (Exception ex) { Debug.WriteLine(ex); }
            }
        }

        private static async Task InvokeAsync<T>(Func<T, Task> eventDelegate, T arg)
        {
            if (eventDelegate == null) return;
            foreach (Func<T, Task> handler in eventDelegate.GetInvocationList())
            {
                try { await handler(arg); }
                catch (Exception ex) { Debug.WriteLine(ex); }
            }
        }

        // EventsManager — event générique nœud modifié
        public class NodeChangedEvent
        {
            public NodeType Type { get; init; }
            public CrudOperation Operation { get; init; }
            public int LinkedOriginalId { get; init; }
            public int? LinkedRequirementId { get; init; }
            public string NewName { get; init; }
            public EventSource Source { get; init; }
        }
            public static event Func<NodeChangedEvent, Task> NodeChanged;
            public static Task RaiseNodeChangedAsync(NodeChangedEvent e) => InvokeAsync(NodeChanged, e);


        public class ModelMetaChangedEvent
        {
            public Guid ModelId { get; set; }
            public int? PartCount { get; set; }
            public int? ReqCount { get; set; }
        }




    }
   
}
