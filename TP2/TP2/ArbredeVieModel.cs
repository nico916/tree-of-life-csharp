using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe représentant le modèle de l'arbre de vie, gérant les nœuds et les liens entre eux.
    /// </summary>
    public class TreeModel
    {
        // Dictionnaire contenant les nœuds identifiés par leur ID.
        public Dictionary<int, Node> Nodes { get; private set; }

        // Liste des liens entre les nœuds.
        public List<Link> Links { get; private set; }

        // Dictionnaire pour accéder rapidement aux enfants d'un nœud en fonction de son ID.
        private Dictionary<int, List<Node>> childrenLookup = new Dictionary<int, List<Node>>();

        /// <summary>
        /// Constructeur de la classe TreeModel. 
        /// Il charge les nœuds et les liens à partir des fichiers CSV spécifiés.
        /// </summary>
        /// <param name="nodesFilePath">Chemin du fichier CSV des nœuds</param>
        /// <param name="linksFilePath">Chemin du fichier CSV des liens</param>
        public TreeModel(string nodesFilePath, string linksFilePath)
        {
            // Démarrer le chronomètre pour mesurer le temps de chargement des données.
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Charger les nœuds et les liens à partir des fichiers CSV.
            Nodes = Node.LoadNodesAsDictionary(nodesFilePath);
            Links = Link.LoadLinks(linksFilePath);

            // Construire le dictionnaire des enfants à partir des liens.
            foreach (var link in Links)
            {
                if (!childrenLookup.ContainsKey(link.ParentNodeId))
                {
                    childrenLookup[link.ParentNodeId] = new List<Node>();
                }

                Node childNode = Nodes[link.ChildNodeId];
                childrenLookup[link.ParentNodeId].Add(childNode);
            }
            // Calculer les nombres de descendants pour chaque nœud.
            CalculateDescendantCounts();
        }

        /// <summary>
        /// Méthode pour obtenir un nœud à partir de son ID.
        /// </summary>
        /// <param name="nodeId">ID du nœud recherché</param>
        /// <returns>Retourne le nœud correspondant à l'ID ou null s'il n'existe pas.</returns>
        public Node? GetNodeById(int nodeId)
        {
            Nodes.TryGetValue(nodeId, out Node? node);
            return node;
        }

        /// <summary>
        /// Méthode pour obtenir les enfants d'un nœud à partir de son ID.
        /// </summary>
        /// <param name="parentId">ID du parent</param>
        /// <returns>Liste des nœuds enfants ou une liste vide s'il n'y en a pas.</returns>
        public List<Node> GetChildren(int parentId)
        {
            if (childrenLookup.TryGetValue(parentId, out List<Node>? children))
            {
                return children;
            }
            else
            {
                return new List<Node>(); // Retourner une liste vide s'il n'y a pas d'enfants.
            }
        }

        /// <summary>
        /// Méthode pour compter les nœuds ayant plus de 8 enfants dans un certain intervalle de niveaux.
        /// </summary>
        /// <param name="node">Nœud à partir duquel commencer la recherche</param>
        /// <param name="minLevel">Niveau minimum pour compter les nœuds</param>
        /// <param name="maxLevel">Niveau maximum pour compter les nœuds</param>
        /// <returns>Nombre de nœuds ayant plus de 8 enfants</returns>
        public int CountNodesWithMoreThanEightChildren(Node node, int minLevel, int maxLevel)
        {
            return CountNodesWithMoreThanEightChildrenRecursive(node, 1, minLevel, maxLevel);
        }

        // Méthode récursive pour compter les nœuds ayant plus de 8 enfants dans un intervalle donné.
        private int CountNodesWithMoreThanEightChildrenRecursive(Node node, int currentLevel, int minLevel, int maxLevel)
        {
            int count = 0;

            if (currentLevel >= minLevel && currentLevel <= maxLevel)
            {
                var children = GetChildren(node.NodeId);
                if (children.Count > 8)
                {
                    count++;
                }
            }

            if (currentLevel >= maxLevel)
            {
                return count;
            }

            foreach (var child in GetChildren(node.NodeId))
            {
                count += CountNodesWithMoreThanEightChildrenRecursive(child, currentLevel + 1, minLevel, maxLevel);
            }

            return count;
        }

        /// <summary>
        /// Méthode pour vérifier s'il existe un nœud ayant au moins 10 enfants dans un intervalle de niveaux.
        /// </summary>
        /// <param name="node">Nœud à partir duquel commencer la recherche</param>
        /// <param name="minLevel">Niveau minimum pour la recherche</param>
        /// <param name="maxLevel">Niveau maximum pour la recherche</param>
        /// <returns>Vrai si un tel nœud existe, sinon faux</returns>
        public bool HasNodeWithAtLeastTenChildren(Node node, int minLevel, int maxLevel)
        {
            return HasNodeWithAtLeastTenChildrenRecursive(node, 1, minLevel, maxLevel);
        }

        // Méthode récursive pour vérifier s'il existe un nœud avec au moins 10 enfants.
        private bool HasNodeWithAtLeastTenChildrenRecursive(Node node, int currentLevel, int minLevel, int maxLevel)
        {
            if (currentLevel >= minLevel && currentLevel <= maxLevel)
            {
                var children = GetChildren(node.NodeId);
                if (children.Count >= 10)
                {
                    return true;
                }
            }

            if (currentLevel >= maxLevel)
            {
                return false;
            }

            foreach (var child in GetChildren(node.NodeId))
            {
                if (HasNodeWithAtLeastTenChildrenRecursive(child, currentLevel + 1, minLevel, maxLevel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Méthode pour obtenir le nombre de nœuds avec plus de 8 enfants entre deux niveaux.
        /// </summary>
        /// <param name="node">Nœud à partir duquel commencer la recherche</param>
        /// <param name="currentLevel">Niveau actuel</param>
        /// <param name="minLevel">Niveau minimum pour compter les nœuds</param>
        /// <param name="maxLevel">Niveau maximum pour compter les nœuds</param>
        /// <returns>Nombre de nœuds ayant plus de 8 enfants</returns>
        public int GetNodesWithMoreThanEightChildrenBetweenLevels(Node node, int currentLevel, int minLevel, int maxLevel)
        {
            int count = 0;

            if (currentLevel >= minLevel && currentLevel <= maxLevel)
            {
                var children = GetChildren(node.NodeId);
                if (children.Count > 8)
                {
                    count++;
                }
            }

            if (currentLevel >= maxLevel)
            {
                return count;
            }

            foreach (var child in GetChildren(node.NodeId))
            {
                count += GetNodesWithMoreThanEightChildrenBetweenLevels(child, currentLevel + 1, minLevel, maxLevel);
            }

            return count;
        }

        /// <summary>
        /// Méthode pour obtenir l'ID du parent d'un nœud.
        /// </summary>
        /// <param name="nodeId">ID du nœud</param>
        /// <returns>ID du parent ou 0 si aucun parent n'existe</returns>
        public int GetParentId(int nodeId)
        {
            var link = Links.Find(l => l.ChildNodeId == nodeId);
            return link != null ? link.ParentNodeId : 0;
        }

        /// <summary>
        /// Méthode pour calculer et mettre à jour le nombre de descendants de chaque nœud.
        /// </summary>
        public void CalculateDescendantCounts()
        {
            foreach (var node in Nodes.Values)
            {
                node.DescendantCount = -1; // Initialiser à -1 pour indiquer que le calcul n'a pas encore été fait.
            }

            foreach (var node in Nodes.Values)
            {
                GetDescendantCount(node.NodeId);
            }
        }

        /// <summary>
        /// Méthode pour obtenir le nombre de descendants d'un nœud.
        /// </summary>
        /// <param name="nodeId">ID du nœud</param>
        /// <returns>Nombre total de descendants</returns>
        public int GetDescendantCount(int nodeId)
        {
            Node node = Nodes[nodeId];
            if (node.DescendantCount != -1)
            {
                return node.DescendantCount;
            }

            var children = GetChildren(nodeId);
            int count = children.Count;

            foreach (var child in children)
            {
                count += GetDescendantCount(child.NodeId);
            }

            node.DescendantCount = count;
            return count;
        }
    }
}
