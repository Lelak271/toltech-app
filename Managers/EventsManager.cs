using System.Diagnostics;

namespace Toltech.App.Services
{
    public static class EventsManager
    {
        // ── Events async (Func<Task>) ────────────────────────────────────────
        public static event Func<Task> RequirementAddedOrDelete;
        public static event Func<Task> PartAddedOrDelete;
        public static event Func<Task> ModelDataAddedOrDelete;
        public static event Func<Task> RequirementUpdated;
        public static event Func<Task> TreeViewUpdated;
        public static event Func<Task> TreeViewDataNodeDrag;
        public static event Func<Task> TreeViewReqNodeDrag;
        public static event Func<Task> ModelOpen;
        public static event Func<Task> ModelDelete;

        // ── Events async avec paramètre ──────────────────────────────────────
        public static event Func<RequirementEvent, Task> RequirementVisibilityChanged;
        public static event Func<RequirementEvent, Task> RequirementSelectChanged;
        public static event Func<int?, Task> PartSelectedChanged;

        // ── Helper d'invocation centrale ────────────────────────────────────
        private static async Task InvokeAsync(Func<Task> eventDelegate)
        {
            var handlers = eventDelegate?.GetInvocationList();
            if (handlers == null) return;

            foreach (var handler in handlers)
                await ((Func<Task>)handler).Invoke();
        }

        private static async Task InvokeAsync<T>(Func<T, Task> eventDelegate, T arg)
        {
            if (eventDelegate == null)
                return;

            foreach (Func<T, Task> handler in eventDelegate.GetInvocationList())
            {
                try
                {
                    await handler(arg);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        // ── Raise methods ────────────────────────────────────────────────────
        public static Task RaiseRequirementAddOrDeletedAsync() => InvokeAsync(RequirementAddedOrDelete);
        public static Task RaisePartAddOrDeletedAsync() => InvokeAsync(PartAddedOrDelete);
        public static Task RaiseModelDataAddOrDeletedAsync() => InvokeAsync(ModelDataAddedOrDelete);
        public static Task RaiseRequirementUpdatedAsync() => InvokeAsync(RequirementUpdated);
        public static Task RaiseModelOpenAsync() => InvokeAsync(ModelOpen);
        public static Task RaiseModelDeleteAsync() => InvokeAsync(ModelDelete);

        public static Task RaiseNodesUpdatedAsync() => InvokeAsync(TreeViewUpdated);
        public static Task RaiseNodesDataDragAsync() => InvokeAsync(TreeViewDataNodeDrag);

        public class RequirementEvent
        {
            public IReadOnlyCollection<int> RequirementIds { get; set; }
            public string NameParentFolder { get; set; }
        }


        public static Task RaiseNodReqSelectChangedAsync(IEnumerable<int> reqIds, string nameParentFolder)
        {
            var evt = new RequirementEvent
            {
                RequirementIds = (IReadOnlyCollection<int>)(reqIds?.ToList() ?? new List<int>()),
                NameParentFolder = nameParentFolder
            };

            return InvokeAsync(RequirementSelectChanged, evt);
        }

        public static Task RaisePartSelectedChangedAsync(int? idPart)
            => InvokeAsync(PartSelectedChanged, idPart);
    }
}