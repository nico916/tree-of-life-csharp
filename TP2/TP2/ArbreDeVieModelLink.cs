using System;
using System.Collections.Generic;
using System.IO;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe représentant un lien entre un nœud parent et un nœud enfant dans l'arbre de vie.
    /// </summary>
    public class Link
    {
        /// <summary>
        /// ID du nœud parent.
        /// </summary>
        public int ParentNodeId { get; set; }

        /// <summary>
        /// ID du nœud enfant.
        /// </summary>
        public int ChildNodeId { get; set; }

        /// <summary>
        /// Méthode statique pour charger les liens parent-enfant à partir d'un fichier CSV.
        /// </summary>
        /// <param name="filePath">Chemin du fichier CSV contenant les liens</param>
        /// <returns>Retourne une liste de liens parent-enfant</returns>
        public static List<Link> LoadLinks(string filePath)
        {
            // Initialisation d'une liste vide pour stocker les liens.
            List<Link> links = new List<Link>();

            // Utilisation d'un StreamReader pour lire le fichier CSV ligne par ligne.
            using (StreamReader sr = new StreamReader(filePath))
            {
                string? line;
                // Lire et ignorer la première ligne (en-tête du CSV).
                sr.ReadLine();

                // Boucle pour lire chaque ligne du fichier jusqu'à la fin.
                while ((line = sr.ReadLine()) != null)
                {
                    // Diviser chaque ligne en colonnes (séparées par des virgules).
                    string[] values = line.Split(',');

                    // Récupérer l'ID du parent et de l'enfant à partir des colonnes.
                    int parentId = int.Parse(values[0] ?? "0");
                    int childId = int.Parse(values[1] ?? "0");

                    // Créer un nouveau lien avec les IDs du parent et de l'enfant.
                    Link link = new Link
                    {
                        ParentNodeId = parentId,
                        ChildNodeId = childId
                    };

                    // Ajouter le lien à la liste des liens.
                    links.Add(link);
                }
            }

            // Retourner la liste des liens une fois le fichier entièrement lu.
            return links;
        }
    }
}
