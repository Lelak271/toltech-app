using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace TOLTECH_APPLICATION.Models
{
    public class ModelDB
    {
        [PrimaryKey]
        public int IdModel { get; set; }
        [Unique]
        public string ModelName { get; set; }
        public string DescriptionModel { get; set; } // Description du modèle
        public string FilePathModel { get; set; } // Chemin du fichier du modèle
        [Unique]
        public DateTime CreatedAtmodel { get; set; } = DateTime.Now; // Date de création du modèle
    }

}
