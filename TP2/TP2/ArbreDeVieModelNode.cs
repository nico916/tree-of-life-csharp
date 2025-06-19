using System;
using System.Collections.Generic;
using System.IO;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe représentant un nœud dans l'arbre de vie.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Identifiant unique du nœud.
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Nom du nœud.
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// Identifiant du nœud parent.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Indique si le nœud représente une espèce éteinte.
        /// </summary>
        public bool IsExtinct { get; set; }

        /// <summary>
        /// Lien vers l'organisation Tree of Life pour des informations supplémentaires sur le nœud.
        /// </summary>
        public string TolOrgLink { get; set; }

        /// <summary>
        /// Niveau de confiance dans la position du nœud dans l'arbre phylogénétique.
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Type de phylétisme associé au nœud (monophylétique, paraphylétique, etc.).
        /// </summary>
        public string Phylesis { get; set; }

        /// <summary>
        /// Indique si le nœud représente un cluster de plusieurs espèces.
        /// </summary>
        public bool IsCluster { get; set; }

        /// <summary>
        /// Compte des descendants de ce nœud dans l'arbre (initialisé à -1, recalculé plus tard).
        /// </summary>
        public int DescendantCount { get; set; } = -1;

        /// <summary>
        /// Constructeur par défaut de la classe Node, initialisant les propriétés à des valeurs par défaut.
        /// </summary>
        public Node()
        {
            NodeName = string.Empty;
            TolOrgLink = string.Empty;
            Phylesis = string.Empty;
        }

        /// <summary>
        /// Méthode statique pour charger les nœuds à partir d'un fichier CSV et les retourner sous forme de dictionnaire.
        /// </summary>
        /// <param name="filePath">Le chemin du fichier CSV contenant les informations des nœuds.</param>
        /// <returns>Retourne un dictionnaire de nœuds, où la clé est l'ID du nœud.</returns>
        public static Dictionary<int, Node> LoadNodesAsDictionary(string filePath)
        {
            // Dictionnaire qui va stocker les nœuds avec leurs ID comme clé.
            Dictionary<int, Node> nodes = new Dictionary<int, Node>();

            // Lire le fichier CSV ligne par ligne.
            using (StreamReader sr = new StreamReader(filePath))
            {
                string? line;
                sr.ReadLine(); // Ignorer la première ligne (en-tête).

                // Parcourir chaque ligne restante du fichier.
                while ((line = sr.ReadLine()) != null)
                {
                    // Diviser la ligne en colonnes à partir des virgules.
                    string[] values = line.Split(',');

                    // Créer un nouveau nœud avec les données extraites.
                    Node node = new Node
                    {
                        NodeId = int.Parse(values[0]), // ID du nœud
                        NodeName = values[1] ?? string.Empty, // Nom du nœud
                        ParentId = 0, // Parent ID (mis à jour plus tard)
                        IsExtinct = values[5] == "1", // Si l'espèce est éteinte
                        TolOrgLink = values[4] ?? string.Empty, // Lien vers Tree of Life
                        Confidence = float.Parse(values[6] ?? "0"), // Niveau de confiance
                        Phylesis = values[7] ?? string.Empty, // Phylétisme du nœud
                        IsCluster = false // Par défaut, le nœud n'est pas un cluster
                    };

                    // Ajouter le nœud au dictionnaire, avec son ID comme clé.
                    nodes[node.NodeId] = node;
                }
            }

            // Retourner le dictionnaire rempli de nœuds.
            return nodes;
        }
    }
}
