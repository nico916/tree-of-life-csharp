using System;
using System.Collections.Generic;
using System.IO;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe repr�sentant un n�ud dans l'arbre de vie.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Identifiant unique du n�ud.
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Nom du n�ud.
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// Identifiant du n�ud parent.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Indique si le n�ud repr�sente une esp�ce �teinte.
        /// </summary>
        public bool IsExtinct { get; set; }

        /// <summary>
        /// Lien vers l'organisation Tree of Life pour des informations suppl�mentaires sur le n�ud.
        /// </summary>
        public string TolOrgLink { get; set; }

        /// <summary>
        /// Niveau de confiance dans la position du n�ud dans l'arbre phylog�n�tique.
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Type de phyl�tisme associ� au n�ud (monophyl�tique, paraphyl�tique, etc.).
        /// </summary>
        public string Phylesis { get; set; }

        /// <summary>
        /// Indique si le n�ud repr�sente un cluster de plusieurs esp�ces.
        /// </summary>
        public bool IsCluster { get; set; }

        /// <summary>
        /// Compte des descendants de ce n�ud dans l'arbre (initialis� � -1, recalcul� plus tard).
        /// </summary>
        public int DescendantCount { get; set; } = -1;

        /// <summary>
        /// Constructeur par d�faut de la classe Node, initialisant les propri�t�s � des valeurs par d�faut.
        /// </summary>
        public Node()
        {
            NodeName = string.Empty;
            TolOrgLink = string.Empty;
            Phylesis = string.Empty;
        }

        /// <summary>
        /// M�thode statique pour charger les n�uds � partir d'un fichier CSV et les retourner sous forme de dictionnaire.
        /// </summary>
        /// <param name="filePath">Le chemin du fichier CSV contenant les informations des n�uds.</param>
        /// <returns>Retourne un dictionnaire de n�uds, o� la cl� est l'ID du n�ud.</returns>
        public static Dictionary<int, Node> LoadNodesAsDictionary(string filePath)
        {
            // Dictionnaire qui va stocker les n�uds avec leurs ID comme cl�.
            Dictionary<int, Node> nodes = new Dictionary<int, Node>();

            // Lire le fichier CSV ligne par ligne.
            using (StreamReader sr = new StreamReader(filePath))
            {
                string? line;
                sr.ReadLine(); // Ignorer la premi�re ligne (en-t�te).

                // Parcourir chaque ligne restante du fichier.
                while ((line = sr.ReadLine()) != null)
                {
                    // Diviser la ligne en colonnes � partir des virgules.
                    string[] values = line.Split(',');

                    // Cr�er un nouveau n�ud avec les donn�es extraites.
                    Node node = new Node
                    {
                        NodeId = int.Parse(values[0]), // ID du n�ud
                        NodeName = values[1] ?? string.Empty, // Nom du n�ud
                        ParentId = 0, // Parent ID (mis � jour plus tard)
                        IsExtinct = values[5] == "1", // Si l'esp�ce est �teinte
                        TolOrgLink = values[4] ?? string.Empty, // Lien vers Tree of Life
                        Confidence = float.Parse(values[6] ?? "0"), // Niveau de confiance
                        Phylesis = values[7] ?? string.Empty, // Phyl�tisme du n�ud
                        IsCluster = false // Par d�faut, le n�ud n'est pas un cluster
                    };

                    // Ajouter le n�ud au dictionnaire, avec son ID comme cl�.
                    nodes[node.NodeId] = node;
                }
            }

            // Retourner le dictionnaire rempli de n�uds.
            return nodes;
        }
    }
}
