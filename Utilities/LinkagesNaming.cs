using System;
using Toltech.Solver.Contracts;

namespace Toltech.App.Utilities
{
    public class LinkageTypeItem
    {
        public LinkageType Value { get; init; }
    }

    /// <summary>
    /// Fournit les clés de localisation associées aux types de liaison.
    /// </summary>
    public static class LinkageTypeExtensions
    {


        /// <summary>
        /// Retourne la clé de ressource utilisée par le système de localisation.
        /// </summary>
        public static string GetLocalizationKey(
            this LinkageType type)
        {
            return type switch
            {
                LinkageType.PointContact =>
                    "Liaison_PointContact",

                LinkageType.PrismaticContact =>
                    "Liaison_PrismaticContact",

                LinkageType.SphericalContact =>
                    "Liaison_SphericalContact",

                LinkageType.AnnularContact =>
                    "Liaison_AnnularContact",

                LinkageType.PlanarContact =>
                    "Liaison_PlanarContact",

                LinkageType.RevoluteContact =>
                    "Liaison_RevoluteContact",

                LinkageType.LinearContact =>
                    "Liaison_LinearContact",

                LinkageType.CylindricalContact =>
                    "Liaison_CylindricalContact",

                LinkageType.FixedContact =>
                    "Liaison_FixedContact",

                LinkageType.Requirement =>
                    "Liaison_Requirement",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Unknown linkage type.")
            };
        }
    }
}