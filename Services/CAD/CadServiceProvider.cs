using Toltech.Cad;
using Toltech.Cad.Abstractions;
using Toltech.FreeCAD;

namespace Toltech.App.Services.CAD
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CadServiceProvider
    {
        private readonly Dictionary<string, ICadApplication>
            _applications = new();

        /// <summary>
        /// Enregistre une application CAO.
        /// </summary>
        public void Register(
            ICadApplication application)
        {
            if (!_applications.TryAdd(
                    application.Id,
                    application))
            {
                throw new InvalidOperationException(
                    $"Le logiciel CAO '{application.Id}' " +
                    "est déjà enregistré.");
            }
        }

        /// <summary>
        /// Retourne une application CAO enregistrée.
        /// </summary>
        public ICadApplication Get(
            string id)
        {
            if (!_applications.TryGetValue(
                    id,
                    out ICadApplication? application))
            {
                throw new KeyNotFoundException(
                    $"Le logiciel CAO '{id}' " +
                    "n'est pas enregistré.");
            }

            return application;
        }

        /// <summary>
        /// Retourne toutes les applications CAO enregistrées.
        /// </summary>
        public IReadOnlyCollection<ICadApplication>
            Applications =>
            _applications.Values;
    }
}
