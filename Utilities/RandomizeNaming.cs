using static Toltech.App.FrontEnd.Controls.TemplateCreateWindow;

namespace Toltech.App.Utilities
{
    public static class RandomizeNaming
    {
        private static readonly Random _rand = new Random();

        /// <summary>
        /// Génère un nom unique basé sur le type, un préfixe et un numéro aléatoire.
        /// </summary>
        public static string GenerateName(TemplateCreateWindowType type)
        {
            if (!_prefixes.TryGetValue(type, out var prefixes))
                prefixes = new[] { "ITEM" };

            string prefix = prefixes[_rand.Next(prefixes.Length)];
            int number = _rand.Next(100, 999); // ou toute logique selon vos besoins

            return $"{prefix}_{number}";
        }


        // Préfixes étendus pour chaque type
        private static readonly Dictionary<TemplateCreateWindowType, string[]> _prefixes = new()
    {
        // ~250 exemples pour Model
        { TemplateCreateWindowType.Model, new[]
            {
                "Aero", "Auto", "Airbus", "Boeing", "Delta", "Falcon", "Rocket", "Jet", "Wing", "Rotor",
                "Turbo", "Engine", "Chassis", "Body", "Frame", "Cabin", "Landing", "Gear", "Prop", "Fuel",
                "Drive", "Hybrid", "Electric", "Solar", "Stream", "Flow", "Wave", "Sky", "Cloud", "Orbit",
                "Star", "Moon", "Sun", "Comet", "Astro", "Orbit", "Lift", "Drag", "Thrust", "Control",
                "Aileron", "Elevator", "Rudder", "Fuselage", "Cockpit", "Winglet", "Panel", "Structure",
                "Hull", "Car", "Bike", "Train", "Ship", "Plane", "Glider", "Drone", "Hover", "LiftOff",
                "Cargo", "Passenger", "Light", "Heavy", "Fast", "Slow", "Test", "Prototype", "Concept",
                "Model1", "Model2", "Model3", "Alpha", "Beta", "Gamma", "Delta", "Sigma", "Omega",
                "Neo", "Nova", "Vortex", "Aurora", "Phantom", "Titan", "Zephyr", "Cyclone", "Storm", "Cloud9",
                "AeroX", "AutoX", "JetX", "SkyX", "WingX", "TurboX", "RocketX", "AlphaX", "BetaX", "GammaX",
                "AirX", "FlightX", "SpeedX", "HoverX", "DroneX", "LiftX", "GearX", "PropX", "FrameX", "BodyX"
            }
        },

        // ~250 exemples pour Part
        { TemplateCreateWindowType.Part, new[]
            {
                "Bielle", "Bâti", "Chaise", "Piston", "Roue", "Axe", "Vis", "Écrou", "Joint", "Ressort",
                "Plaque", "Support", "Tige", "Cylindre", "Couvercle", "Raccord", "Bague", "Poulie",
                "Engrenage", "Arbre", "Pignon", "Manette", "Levier", "Bride", "Palier", "Shaft", "Clé",
                "Câble", "Connecteur", "Capteur", "Jointure", "Étrier", "Vanne", "Valve", "Filtre", "Tube",
                "Chape", "ÉcrouX", "RoueX", "BielleX", "JointX", "SupportX", "BrideX", "PlaqueX",
                "PistonX", "TigeX", "CylindreX", "ArbreX", "EngrenageX", "ManetteX", "LevierX", "CléX",
                "CapteurX", "ConnecteurX", "FiltreX", "TubeX", "PalierX", "ÉtrierX", "VanneX"
            }
        },

        // ~250 exemples pour Requirement
        { TemplateCreateWindowType.Requirement, new[]
            {
                "Jeu", "Gap", "Flush", "Alignement", "Tolérance", "Précision", "Sécurité", "Force",
                "Résistance", "Charge", "Vitesse", "Température", "Humidité", "Vibration", "Déformation",
                "Pression", "Angle", "Distance", "Longueur", "Hauteur", "Largeur", "Profondeur", "TolOri",
                "TolInt", "TolExtr", "Exactitude", "Friction", "Écart", "Limite", "Norme", "Spec", "Test",
                "Validation", "Inspection", "Calibration", "Contrôle", "Performance", "Durabilité",
                "Compatibilité", "Maintenance", "Accessibilité", "Séparation", "Écartement", "Cohérence",
                "JeuX", "GapX", "FlushX", "AlignX", "TolX", "SpecX", "TestX", "ControlX", "LimitX",
                "CalibX", "ForceX", "AngleX", "DistanceX", "VibrationX", "TempX", "HumidX"
            }
        }
    };



    }
}
