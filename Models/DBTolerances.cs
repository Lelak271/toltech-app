using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Toltech.App.Models
{
    public class DBTolerances
    {
        [PrimaryKey, AutoIncrement]
        public int Id_tol { get; set; } // Identifiant unique de la tolérance DB
        public string DescriptionTolInt { get; set; } // Description de la tolérance DB
        public string NameTolInt { get; set; } // Nom de la tolérance DB
        public double tolInt { get; set; } // Tolérance de la tol DB
        public double TBD4 { get; set; } // TBD
        public double TBD5 { get; set; } // TBD

    }


}
