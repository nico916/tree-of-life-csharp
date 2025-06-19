using System;
using System.Collections.Generic;
using System.IO;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe repr�sentant un lien entre un n�ud parent et un n�ud enfant dans l'arbre de vie.
    /// </summary>
    public class Link
    {
        /// <summary>
        /// ID du n�ud parent.
        /// </summary>
        public int ParentNodeId { get; set; }

        /// <summary>
        /// ID du n�ud enfant.
        /// </summary>
        public int ChildNodeId { get; set; }

        /// <summary>
        /// M�thode statique pour charger les liens parent-enfant � partir d'un fichier CSV.
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
                // Lire et ignorer la premi�re ligne (en-t�te du CSV).
                sr.ReadLine();

                // Boucle pour lire chaque ligne du fichier jusqu'� la fin.
                while ((line = sr.ReadLine()) != null)
                {
                    // Diviser chaque ligne en colonnes (s�par�es par des virgules).
                    string[] values = line.Split(',');

                    // R�cup�rer l'ID du parent et de l'enfant � partir des colonnes.
                    int parentId = int.Parse(values[0] ?? "0");
                    int childId = int.Parse(values[1] ?? "0");

                    // Cr�er un nouveau lien avec les IDs du parent et de l'enfant.
                    Link link = new Link
                    {
                        ParentNodeId = parentId,
                        ChildNodeId = childId
                    };

                    // Ajouter le lien � la liste des liens.
                    links.Add(link);
                }
            }

            // Retourner la liste des liens une fois le fichier enti�rement lu.
            return links;
        }
    }
}
