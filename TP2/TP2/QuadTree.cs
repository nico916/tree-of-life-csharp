using System;
using System.Collections.Generic;
using System.Drawing;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe représentant une structure de données Quadtree, utilisée pour diviser un espace 2D en sections
    /// plus petites afin de faciliter des opérations de recherche rapide.
    /// </summary>
    public class Quadtree
    {
        // Limites du quadtree sous forme de rectangle.
        private Rectangle bounds;

        // Capacité maximale de nœuds avant subdivision.
        private int capacity;

        // Liste pour stocker les nœuds avec leurs positions.
        private List<(Node node, Point position)> nodeEntries;

        // Tableaux des sous-quadrants une fois que le Quadtree est subdivisé.
        private Quadtree[]? quadrants;

        /// <summary>
        /// Constructeur pour initialiser un Quadtree avec des limites spécifiques et une capacité maximale.
        /// </summary>
        /// <param name="bounds">Les limites du Quadtree.</param>
        /// <param name="capacity">La capacité maximale avant subdivision (par défaut 4).</param>
        public Quadtree(Rectangle bounds, int capacity = 4)
        {
            this.bounds = bounds;
            this.capacity = capacity;
            this.nodeEntries = new List<(Node node, Point position)>();
            this.quadrants = null; // Les sous-quadrants ne sont créés qu'à la subdivision.
        }

        /// <summary>
        /// Méthode pour insérer un nœud dans le Quadtree à une position donnée.
        /// Si la capacité est atteinte, le Quadtree se subdivise.
        /// </summary>
        /// <param name="node">Le nœud à insérer.</param>
        /// <param name="position">La position du nœud dans l'espace 2D.</param>
        public void Insert(Node node, Point position)
        {
            // Si la position du nœud est en dehors des limites du Quadtree, l'insertion échoue.
            if (!bounds.Contains(position))
                return;

            // Si la capacité du Quadtree n'est pas encore atteinte, ajouter simplement le nœud.
            if (nodeEntries.Count < capacity)
            {
                nodeEntries.Add((node, position)); // Ajouter le nœud avec sa position.
            }
            else
            {
                // Si le Quadtree n'a pas encore été subdivisé, le subdiviser.
                if (quadrants == null)
                {
                    Subdivide();
                }

                // Vérifier que les quadrants existent avant d'y insérer le nœud.
                if (quadrants != null)
                {
                    foreach (var quadrant in quadrants)
                    {
                        quadrant.Insert(node, position);
                    }
                }
            }
        }

        /// <summary>
        /// Méthode pour subdiviser le Quadtree en quatre quadrants plus petits.
        /// </summary>
        private void Subdivide()
        {
            int subWidth = bounds.Width / 2;
            int subHeight = bounds.Height / 2;
            int x = bounds.X;
            int y = bounds.Y;

            // Créer les quatre nouveaux quadrants avec des sous-limites.
            quadrants = new Quadtree[4];
            quadrants[0] = new Quadtree(new Rectangle(x, y, subWidth, subHeight), capacity);
            quadrants[1] = new Quadtree(new Rectangle(x + subWidth, y, subWidth, subHeight), capacity);
            quadrants[2] = new Quadtree(new Rectangle(x, y + subHeight, subWidth, subHeight), capacity);
            quadrants[3] = new Quadtree(new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight), capacity);
        }

        /// <summary>
        /// Méthode pour rechercher un nœud dans le Quadtree à partir d'une position donnée.
        /// Si le nœud est trouvé dans les limites, il est retourné.
        /// </summary>
        /// <param name="point">Le point de recherche dans l'espace 2D.</param>
        /// <returns>Retourne le nœud s'il est trouvé, sinon null.</returns>
        public Node? Query(PointF point)
        {
            // Si le point est en dehors des limites du Quadtree, retourner null.
            if (!bounds.Contains(Point.Round(point)))
                return null;

            // Rechercher dans les nœuds stockés dans ce Quadtree.
            foreach (var entry in nodeEntries)
            {
                // Vérifier si le point est dans le rectangle associé au nœud.
                Rectangle nodeRect = new Rectangle(entry.position.X - 10, entry.position.Y - 10, 20, 20);
                if (nodeRect.Contains(Point.Round(point)))
                    return entry.node; // Si trouvé, retourner le nœud.
            }

            // Si des quadrants existent, continuer la recherche dans ces quadrants.
            if (quadrants != null)
            {
                foreach (var quadrant in quadrants)
                {
                    Node? result = quadrant.Query(point);
                    if (result != null)
                        return result; // Retourner le nœud s'il est trouvé dans un quadrant.
                }
            }

            // Si le nœud n'est pas trouvé, retourner null.
            return null;
        }
    }
}
