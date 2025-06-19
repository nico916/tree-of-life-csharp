using System;
using System.Collections.Generic;
using System.Drawing;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Classe repr�sentant une structure de donn�es Quadtree, utilis�e pour diviser un espace 2D en sections
    /// plus petites afin de faciliter des op�rations de recherche rapide.
    /// </summary>
    public class Quadtree
    {
        // Limites du quadtree sous forme de rectangle.
        private Rectangle bounds;

        // Capacit� maximale de n�uds avant subdivision.
        private int capacity;

        // Liste pour stocker les n�uds avec leurs positions.
        private List<(Node node, Point position)> nodeEntries;

        // Tableaux des sous-quadrants une fois que le Quadtree est subdivis�.
        private Quadtree[]? quadrants;

        /// <summary>
        /// Constructeur pour initialiser un Quadtree avec des limites sp�cifiques et une capacit� maximale.
        /// </summary>
        /// <param name="bounds">Les limites du Quadtree.</param>
        /// <param name="capacity">La capacit� maximale avant subdivision (par d�faut 4).</param>
        public Quadtree(Rectangle bounds, int capacity = 4)
        {
            this.bounds = bounds;
            this.capacity = capacity;
            this.nodeEntries = new List<(Node node, Point position)>();
            this.quadrants = null; // Les sous-quadrants ne sont cr��s qu'� la subdivision.
        }

        /// <summary>
        /// M�thode pour ins�rer un n�ud dans le Quadtree � une position donn�e.
        /// Si la capacit� est atteinte, le Quadtree se subdivise.
        /// </summary>
        /// <param name="node">Le n�ud � ins�rer.</param>
        /// <param name="position">La position du n�ud dans l'espace 2D.</param>
        public void Insert(Node node, Point position)
        {
            // Si la position du n�ud est en dehors des limites du Quadtree, l'insertion �choue.
            if (!bounds.Contains(position))
                return;

            // Si la capacit� du Quadtree n'est pas encore atteinte, ajouter simplement le n�ud.
            if (nodeEntries.Count < capacity)
            {
                nodeEntries.Add((node, position)); // Ajouter le n�ud avec sa position.
            }
            else
            {
                // Si le Quadtree n'a pas encore �t� subdivis�, le subdiviser.
                if (quadrants == null)
                {
                    Subdivide();
                }

                // V�rifier que les quadrants existent avant d'y ins�rer le n�ud.
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
        /// M�thode pour subdiviser le Quadtree en quatre quadrants plus petits.
        /// </summary>
        private void Subdivide()
        {
            int subWidth = bounds.Width / 2;
            int subHeight = bounds.Height / 2;
            int x = bounds.X;
            int y = bounds.Y;

            // Cr�er les quatre nouveaux quadrants avec des sous-limites.
            quadrants = new Quadtree[4];
            quadrants[0] = new Quadtree(new Rectangle(x, y, subWidth, subHeight), capacity);
            quadrants[1] = new Quadtree(new Rectangle(x + subWidth, y, subWidth, subHeight), capacity);
            quadrants[2] = new Quadtree(new Rectangle(x, y + subHeight, subWidth, subHeight), capacity);
            quadrants[3] = new Quadtree(new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight), capacity);
        }

        /// <summary>
        /// M�thode pour rechercher un n�ud dans le Quadtree � partir d'une position donn�e.
        /// Si le n�ud est trouv� dans les limites, il est retourn�.
        /// </summary>
        /// <param name="point">Le point de recherche dans l'espace 2D.</param>
        /// <returns>Retourne le n�ud s'il est trouv�, sinon null.</returns>
        public Node? Query(PointF point)
        {
            // Si le point est en dehors des limites du Quadtree, retourner null.
            if (!bounds.Contains(Point.Round(point)))
                return null;

            // Rechercher dans les n�uds stock�s dans ce Quadtree.
            foreach (var entry in nodeEntries)
            {
                // V�rifier si le point est dans le rectangle associ� au n�ud.
                Rectangle nodeRect = new Rectangle(entry.position.X - 10, entry.position.Y - 10, 20, 20);
                if (nodeRect.Contains(Point.Round(point)))
                    return entry.node; // Si trouv�, retourner le n�ud.
            }

            // Si des quadrants existent, continuer la recherche dans ces quadrants.
            if (quadrants != null)
            {
                foreach (var quadrant in quadrants)
                {
                    Node? result = quadrant.Query(point);
                    if (result != null)
                        return result; // Retourner le n�ud s'il est trouv� dans un quadrant.
                }
            }

            // Si le n�ud n'est pas trouv�, retourner null.
            return null;
        }
    }
}
