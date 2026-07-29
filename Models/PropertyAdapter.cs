using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Toltech.App.Models;

namespace Toltech.App.ViewModels
{
    /// <summary>
    /// Adapte une propriété plate de <see cref="ModelData"/> vers un accès typé.
    /// Ne stocke aucune donnée : redirige lecture/écriture vers le modèle.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class PropertyAdapter<T>
    {
        private readonly ModelData _model;
        private readonly PropertyInfo _property;

        public string PropertyName => _property.Name;

        public PropertyAdapter(ModelData model, Expression<Func<ModelData, T>> expression)
        {
            _model = model;

            if (expression.Body is not MemberExpression member)
                throw new ArgumentException("L'expression doit être une propriété.");

            _property = (PropertyInfo)member.Member;
        }

        public T Value
        {
            get => (T)_property.GetValue(_model)!;
            set => _property.SetValue(_model, value);
        }
    }
}