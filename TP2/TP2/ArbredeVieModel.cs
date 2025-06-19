using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe repr�sentant le mod�le de l'arbre de vie, g�rant les n�uds et les liens entre eux.
    /// </summary>
    public class TreeModel
    {
        // Dictionnaire contenant les n�uds identifi�s par leur ID.
        public Dictionary<int, Node> Nodes { get; private set; }

        // Liste des liens entre les n�uds.
        public List<Link> Links { get; private set; }

        // Dictionnaire pour acc�der rapidement aux enfants d'un n�ud en fonction de son ID.
        private Dictionary<int, List<Node>> childrenLookup = new Dictionary<int, List<Node>>();

        /// <summary>
        /// Constructeur de la classe TreeModel. 
        /// Il charge les n�uds et les liens � partir des fichiers CSV sp�cifi�s.
        /// </summary>
        /// <param name="nodesFilePath">Chemin du fichier CSV des n�uds</param>
        /// <param name="linksFilePath">Chemin du fichier CSV des liens</param>
        public TreeModel(string nodesFilePath, string linksFilePath)
        {
            // D�marrer le chronom�tre pour mesurer le temps de chargement des donn�es.
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Charger les n�uds et les liens � partir des fichiers CSV.
            Nodes = Node.LoadNodesAsDictionary(nodesFilePath);
            Links = Link.LoadLinks(linksFilePath);

            // Construire le dictionnaire des enfants � partir des liens.
            foreach (var link in Links)
            {
                if (!childrenLookup.ContainsKey(link.ParentNodeId))
                {
                    childrenLookup[link.ParentNodeId] = new List<Node>();
                }

                Node childNode = Nodes[link.ChildNodeId];
                childrenLookup[link.ParentNodeId].Add(childNode);
            }
            // Calculer les nombres de descendants pour chaque n�ud.
            CalculateDescendantCounts();
        }

        /// <summary>
        /// M�thode pour obtenir un n�ud � partir de son ID.
        /// </summary>
        /// <param name="nodeId">ID du n�ud recherch�</param>
        /// <returns>Retourne le n�ud correspondant � l'ID ou null s'il n'existe pas.</returns>
        public Node? GetNodeById(int nodeId)
        {
            Nodes.TryGetValue(nodeId, out Node? node);
            return node;
        }

        /// <summary>
        /// M�thode pour obtenir les enfants d'un n�ud � partir de son ID.
        /// </summary>
        /// <param name="parentId">ID du parent</param>
        /// <returns>Liste des n�uds enfants ou une liste vide s'il n'y en a pas.</returns>
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
        /// M�thode pour compter les n�uds ayant plus de 8 enfants dans un certain intervalle de niveaux.
        /// </summary>
        /// <param name="node">N�ud � partir duquel commencer la recherche</param>
        /// <param name="minLevel">Niveau minimum pour compter les n�uds</param>
        /// <param name="maxLevel">Niveau maximum pour compter les n�uds</param>
        /// <returns>Nombre de n�uds ayant plus de 8 enfants</returns>
        public int CountNodesWithMoreThanEightChildren(Node node, int minLevel, int maxLevel)
        {
            return CountNodesWithMoreThanEightChildrenRecursive(node, 1, minLevel, maxLevel);
        }

        // M�thode r�cursive pour compter les n�uds ayant plus de 8 enfants dans un intervalle donn�.
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
        /// M�thode pour v�rifier s'il existe un n�ud ayant au moins 10 enfants dans un intervalle de niveaux.
        /// </summary>
        /// <param name="node">N�ud � partir duquel commencer la recherche</param>
        /// <param name="minLevel">Niveau minimum pour la recherche</param>
        /// <param name="maxLevel">Niveau maximum pour la recherche</param>
        /// <returns>Vrai si un tel n�ud existe, sinon faux</returns>
        public bool HasNodeWithAtLeastTenChildren(Node node, int minLevel, int maxLevel)
        {
            return HasNodeWithAtLeastTenChildrenRecursive(node, 1, minLevel, maxLevel);
        }

        // M�thode r�cursive pour v�rifier s'il existe un n�ud avec au moins 10 enfants.
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
        /// M�thode pour obtenir le nombre de n�uds avec plus de 8 enfants entre deux niveaux.
        /// </summary>
        /// <param name="node">N�ud � partir duquel commencer la recherche</param>
        /// <param name="currentLevel">Niveau actuel</param>
        /// <param name="minLevel">Niveau minimum pour compter les n�uds</param>
        /// <param name="maxLevel">Niveau maximum pour compter les n�uds</param>
        /// <returns>Nombre de n�uds ayant plus de 8 enfants</returns>
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
        /// M�thode pour obtenir l'ID du parent d'un n�ud.
        /// </summary>
        /// <param name="nodeId">ID du n�ud</param>
        /// <returns>ID du parent ou 0 si aucun parent n'existe</returns>
        public int GetParentId(int nodeId)
        {
            var link = Links.Find(l => l.ChildNodeId == nodeId);
            return link != null ? link.ParentNodeId : 0;
        }

        /// <summary>
        /// M�thode pour calculer et mettre � jour le nombre de descendants de chaque n�ud.
        /// </summary>
        public void CalculateDescendantCounts()
        {
            foreach (var node in Nodes.Values)
            {
                node.DescendantCount = -1; // Initialiser � -1 pour indiquer que le calcul n'a pas encore �t� fait.
            }

            foreach (var node in Nodes.Values)
            {
                GetDescendantCount(node.NodeId);
            }
        }

        /// <summary>
        /// M�thode pour obtenir le nombre de descendants d'un n�ud.
        /// </summary>
        /// <param name="nodeId">ID du n�ud</param>
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
