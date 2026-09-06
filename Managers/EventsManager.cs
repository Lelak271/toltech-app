using System.Diagnostics;
using Toltech.App.Models;
using static Toltech.App.Models.NodesDefinition;

namespace Toltech.App.Services
{
    public static class EventsManager
    {
        // ── Events signal (sans payload) ────────────────────────────────────


        // ── Events avec payload ──────────────────────────────────────────────
        public static event Func<int?, Task> PartSelectedChanged;

        // ── Events CRUD ──────────────────────────────────────────────────────
        public static event Func<PartCrudEvent, Task> PartCrud;
        public static event Func<RequirementCrudEvent, Task> RequirementCrud;
        public static event Func<ModelDataCrudEvent, Task> ModelDataCrud;

        public static Task RaisePartCrudAsync(PartCrudEvent e) => InvokeAsync(PartCrud, e);
        public static Task RaiseRequirementCrudAsync(RequirementCrudEvent e) => InvokeAsync(RequirementCrud, e);
        public static Task RaiseModelDataCrudAsync(ModelDataCrudEvent e) => InvokeAsync(ModelDataCrud, e);

        // ── Payloads ─────────────────────────────────────────────────────────
        public class RequirementEvent
        {
            public IReadOnlyCollection<int> RequirementIds { get; init; }
            public string NameParentFolder { get; init; }
        }

        public enum EventSource { Tree, Data, Req, Part, Model, Arrow }

        public enum CrudOperation { Added, Updated, Deleted, Move }

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


        public static event Func<ModelOpenedEvent, Task> ModelOpened;

        public class ModelOpenedEvent
        {
            public string Path { get; init; }

        }

        public static Task RaiseModelOpenedAsync(ModelOpenedEvent e)
    => InvokeAsync(ModelOpened, e);

        /// <summary>
        /// Pendant de NodeChangedEvent pour les modifications faites depuis la vue 3D
        /// (VSTWindow / V3DViewModel) sur une flèche, donc sur le ModelData qui lui est lié.
        ///
        /// Pour le moment cet event est seulement ENVOYÉ, via
        /// EventsManager.RaiseArrowChangedAsync(new ArrowChangedEvent { ... }).
        /// Personne ne l'écoute encore côté services : l'application au ModelData en base
        /// (SQLite) sera branchée dans un second temps, comme convenu.
        /// </summary>
        public class ArrowChangedEvent
        {
            /// <summary>Created / Updated / Deleted... même enum CrudOperation que pour NodeChangedEvent.</summary>
            public CrudOperation Operation { get; set; }

            /// <summary>Id du ModelData impacté (== ArrowVisual3D.LinkedOriginalId).</summary>
            public int? LinkedOriginalId { get; set; }

            /// <summary>
            /// Snapshot du ModelData après modification. Volontairement l'objet complet plutôt
            /// qu'un champ unique façon NodeChangedEvent.NewName : une flèche peut être modifiée
            /// sur plusieurs axes à la fois (position, vecteur, pièce liée...). Un futur listener
            /// pourra comparer avec l'existant en base pour ne persister que ce qui a changé.
            /// </summary>
            public ModelData NewValue { get; set; } // TODO: adapte au namespace réel de ModelData si besoin d'un using explicite

            /// <summary>TODO: ajoute le membre "Viewer3D" à ton enum EventSource existant (à côté de Tree, etc.).</summary>
            public EventSource Source { get; set; } = EventSource.Arrow;
        }
        public static event Func<ArrowChangedEvent, Task> ArrowChanged;
        public static Task RaiseArrowChangedAsync(ArrowChangedEvent e) => InvokeAsync(ArrowChanged, e);

    }

}
