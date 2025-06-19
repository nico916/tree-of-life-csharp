using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

namespace TreeOfLifeApp
{
    /// <summary>
    /// Contr�leur principal de l'application Arbre de Vie. 
    /// G�re l'interaction entre la vue (Form1) et le mod�le (TreeModel), ainsi que les �v�nements d'entr�e de l'utilisateur.
    /// </summary>
    public class ArbreDeVieController
    {
        private TreeModel model; // Mod�le de donn�es repr�sentant l'arbre
        private Form1 view; // Vue associ�e pour l'affichage
        private Quadtree? quadtree; // Quadtree pour optimiser la d�tection des n�uds dans la zone visible
        private Bitmap? treeBitmap; // Bitmap utilis� pour stocker l'image de l'arbre
        private bool needsRedraw = true; // Indique si l'arbre doit �tre redessin�
        private float zoomFactor = 1.0f; // Facteur de zoom global
        private PointF translation = new PointF(0, 0); // Translation appliqu�e pour ajuster la vue
        private Point lastMousePosition; // Derni�re position de la souris lors du d�placement
        private bool isDragging = false; // Indique si la vue est en cours de d�placement
        private Point origin = new Point(600, 400); // Centre par d�faut de l'arbre, o� se trouve le n�ud racine
        private Dictionary<Node, Point> nodePositions; // Dictionnaire stockant les positions des n�uds
        private Dictionary<Node, int> nodeLevels; // Dictionnaire stockant les niveaux des n�uds
        private Node? highlightedNode; // N�ud actuellement surlign�
        private HashSet<Node> expandedClusters = new HashSet<Node>(); // Ensemble des clusters actuellement ouverts
        private const float MIN_ZOOM_FACTOR = 0.5f; // Facteur de zoom minimal
        private const float CLUSTER_EXPANSION_THRESHOLD = 1.2f; // Seuil de zoom pour d�plier automatiquement les clusters
        private const float ANGLE_ADJUSTMENT = 0.1f; // Ajustement des angles pour �viter les chevauchements
        private Dictionary<(int, int), bool> descendantCache = new Dictionary<(int, int), bool>(); // Cache pour �viter de recalculer si un n�ud est un descendant

        // Propri�t� publique pour acc�der au facteur de zoom
        public float ZoomFactor
        {
            get { return zoomFactor; }
        }

        private Dictionary<int, float> zoomThresholds = new Dictionary<int, float>();

        /// <summary>
        /// Constructeur du contr�leur ArbreDeVie.
        /// Initialise le mod�le, la vue, et configure le zoom.
        /// </summary>
        /// <param name="nodesFilePath">Chemin vers le fichier des n�uds.</param>
        /// <param name="linksFilePath">Chemin vers le fichier des liens.</param>
        /// <param name="view">Instance de la vue associ�e (Form1).</param>
        public ArbreDeVieController(string nodesFilePath, string linksFilePath, Form1 view)
        {
            this.view = view;
            model = new TreeModel(nodesFilePath, linksFilePath); // Charger les donn�es du mod�le
            nodePositions = new Dictionary<Node, Point>();
            nodeLevels = new Dictionary<Node, int>();

            // Initialisation des seuils de zoom par niveau
            InitializeZoomThresholds();

            // Recentre la vue et calcule les positions initiales des n�uds
            RecalculateTranslation();
            CalculateNodePositions();
        }

        /// <summary>
        /// Initialise les seuils de zoom par niveau.
        /// </summary>
        private void InitializeZoomThresholds()
        {
            int maxLevel = 20;
            float baseThreshold = 0.8f;
            float zoomMultiplier = 1.2f;

            for (int level = 0; level <= maxLevel; level++)
            {
                zoomThresholds[level] = baseThreshold * (float)Math.Pow(zoomMultiplier, level);
            }
        }

        /// <summary>
        /// V�rifie si un n�ud est visible dans la zone actuelle.
        /// </summary>
        /// <param name="position">Position du n�ud.</param>
        /// <returns>Vrai si le n�ud est visible, sinon faux.</returns>
        private bool IsNodeVisible(Point position)
        {
            Rectangle visibleRect = new Rectangle(
                (int)(-translation.X / zoomFactor),
                (int)(-translation.Y / zoomFactor),
                (int)(view.GetPanelSize().Width / zoomFactor),
                (int)(view.GetPanelSize().Height / zoomFactor)
            );

            int nodeSize = 20; // Taille approximative du n�ud
            Rectangle nodeRect = new Rectangle(position.X - nodeSize / 2, position.Y - nodeSize / 2, nodeSize, nodeSize);

            // V�rifie si le n�ud est dans la zone visible
            return visibleRect.IntersectsWith(nodeRect);
        }

        /// <summary>
        /// Retourne le rectangle � invalider autour d'un n�ud.
        /// </summary>
        /// <param name="node">N�ud concern�.</param>
        /// <returns>Rectangle � invalider pour redessin.</returns>
        private Rectangle GetInvalidationRectangleForNode(Node node)
        {
            if (nodePositions.TryGetValue(node, out Point position))
            {
                int size = 50; // Taille autour du n�ud � invalider
                return new Rectangle(position.X - size, position.Y - size, size * 2, size * 2);
            }
            return Rectangle.Empty;
        }

        /// <summary>
        /// Ajuste l'affichage des clusters en fonction du niveau de zoom.
        /// </summary>
        /// <returns>Vrai si des clusters ont �t� ajust�s, sinon faux.</returns>
        private bool AdjustClustersBasedOnZoomLevel()
        {
            bool clustersChanged = false;

            foreach (var kvp in zoomThresholds)
            {
                int level = kvp.Key;
                float threshold = kvp.Value;

                if (zoomFactor >= threshold)
                {
                    // Ouvre les clusters au niveau actuel
                    var clustersAtLevel = nodeLevels
                        .Where(nl => nl.Value == level && IsCluster(nl.Key))
                        .Select(nl => nl.Key);
                    foreach (var cluster in clustersAtLevel)
                    {
                        if (expandedClusters.Add(cluster))
                        {
                            clustersChanged = true;
                        }
                    }
                }
                else
                {
                    // Ferme les clusters au niveau actuel
                    var clustersAtLevel = nodeLevels
                        .Where(nl => nl.Value == level && IsCluster(nl.Key))
                        .Select(nl => nl.Key)
                        .ToList();
                    foreach (var cluster in clustersAtLevel)
                    {
                        if (expandedClusters.Remove(cluster))
                        {
                            clustersChanged = true;
                        }
                    }
                }
            }

            if (clustersChanged)
            {
                needsRedraw = true; // Marque que le redessin est n�cessaire
            }

            return clustersChanged;
        }

        /// <summary>
        /// V�rifie si un n�ud est proche de la zone visible.
        /// </summary>
        /// <param name="position">Position du n�ud.</param>
        /// <returns>Vrai si le n�ud est proche, sinon faux.</returns>
        private bool IsNodeNearVisibleArea(Point position)
        {
            int margin = 100; // Marge autour de la zone visible

            Rectangle visibleRect = new Rectangle(
                (int)(-translation.X / zoomFactor) - margin,
                (int)(-translation.Y / zoomFactor) - margin,
                (int)(view.GetPanelSize().Width / zoomFactor) + 2 * margin,
                (int)(view.GetPanelSize().Height / zoomFactor) + 2 * margin
            );

            int nodeSize = 20;
            Rectangle nodeRect = new Rectangle(position.X - nodeSize / 2, position.Y - nodeSize / 2, nodeSize, nodeSize);

            return visibleRect.IntersectsWith(nodeRect);
        }

        /// <summary>
        /// G�re l'�v�nement de la molette de la souris pour zoomer.
        /// </summary>
        public void OnMouseWheel(MouseEventArgs? e)
        {
            if (e == null) return;

            float previousZoom = zoomFactor;
            bool zoomingIn = e.Delta > 0;

            if (zoomingIn)
            {
                zoomFactor += 0.1f;
            }
            else
            {
                zoomFactor = Math.Max(0.1f, zoomFactor - 0.1f);
            }

            Node? nodeUnderCursor = GetNodeUnderCursor(e.Location); // D�tecte le n�ud sous le curseur
            bool clustersChanged = false;

            if (nodeUnderCursor != null && IsCluster(nodeUnderCursor))
            {
                int level = nodeLevels[nodeUnderCursor];

                if (zoomingIn)
                {
                    // Ouvre les clusters du m�me niveau que celui sous le curseur
                    var clustersAtLevel = nodeLevels
                        .Where(nl => nl.Value == level && IsCluster(nl.Key))
                        .Select(nl => nl.Key);
                    foreach (var cluster in clustersAtLevel)
                    {
                        if (expandedClusters.Add(cluster))
                        {
                            clustersChanged = true;
                        }
                    }
                }
                else
                {
                    // Ferme les clusters du m�me niveau que celui sous le curseur
                    var clustersAtLevel = nodeLevels
                        .Where(nl => nl.Value == level && IsCluster(nl.Key))
                        .Select(nl => nl.Key)
                        .ToList();
                    foreach (var cluster in clustersAtLevel)
                    {
                        if (expandedClusters.Remove(cluster))
                        {
                            clustersChanged = true;
                        }
                    }
                }
            }
            else
            {
                clustersChanged = AdjustClustersBasedOnZoomLevel(); // Ajuste les clusters en fonction du zoom global
            }

            if (clustersChanged)
            {
                CalculateNodePositions(); // Recalculer uniquement si les clusters ont chang�
            }

            needsRedraw = true;

            // Mise � jour de la translation pour centrer sur la position du curseur
            PointF mousePositionBeforeZoom = new PointF(
                (e.Location.X - translation.X) / previousZoom,
                (e.Location.Y - translation.Y) / previousZoom
            );
            PointF mousePositionAfterZoom = new PointF(
                (e.Location.X - translation.X) / zoomFactor,
                (e.Location.Y - translation.Y) / zoomFactor
            );
            translation.X += (mousePositionAfterZoom.X - mousePositionBeforeZoom.X) * zoomFactor;
            translation.Y += (mousePositionAfterZoom.Y - mousePositionBeforeZoom.Y) * zoomFactor;

            view.RefreshPanel(); // Rafra�chit l'affichage
        }



        /// <summary>
        /// Ouvre les clusters globalement en fonction du niveau de zoom.
        /// Peut �tre supprim�e si elle n'est pas utilis�e.
        /// </summary>
        private void ExpandClustersGlobally()
        {
            foreach (var nodePosition in nodePositions)
            {
                Node node = nodePosition.Key;
                if (IsCluster(node) && zoomFactor >= CLUSTER_EXPANSION_THRESHOLD)
                {
                    expandedClusters.Add(node); // Ouvre tous les clusters atteignant ce niveau de zoom
                }
            }
        }

        /// <summary>
        /// D�tecte le n�ud sous le curseur de la souris.
        /// </summary>
        /// <param name="cursorPosition">Position actuelle du curseur de la souris.</param>
        /// <returns>Le n�ud sous le curseur ou null s'il n'y en a pas.</returns>
        private Node? GetNodeUnderCursor(Point cursorPosition)
        {
            PointF adjustedCursorPosition = new PointF(
                (cursorPosition.X - translation.X) / zoomFactor,
                (cursorPosition.Y - translation.Y) / zoomFactor
            );

            foreach (var entry in nodePositions)
            {
                Point nodePos = entry.Value;
                Rectangle nodeRect = new Rectangle(nodePos.X - 10, nodePos.Y - 10, 20, 20);

                if (nodeRect.Contains(Point.Round(adjustedCursorPosition)))
                {
                    return entry.Key; // Retourne le n�ud sous le curseur
                }
            }
            return null;
        }

        /// <summary>
        /// V�rifie si un n�ud est un cluster (c'est-�-dire s'il a suffisamment de descendants).
        /// </summary>
        /// <param name="node">Le n�ud � v�rifier.</param>
        /// <returns>True si le n�ud est un cluster, sinon False.</returns>
        public bool IsCluster(Node node)
        {
            return node.NodeId != 1 && node.DescendantCount >= 5;
        }

        /// <summary>
        /// G�re le d�placement de la vue lorsqu'on d�place la souris.
        /// </summary>
        /// <param name="e">Les arguments de l'�v�nement de d�placement de la souris.</param>
        public void OnMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                int deltaX = e.Location.X - lastMousePosition.X;
                int deltaY = e.Location.Y - lastMousePosition.Y;
                translation.X += deltaX;
                translation.Y += deltaY;
                lastMousePosition = e.Location;

                // Marque que la bitmap doit �tre redessin�e
                needsRedraw = true;

                // Invalider tout le panneau ou une zone sp�cifique
                Rectangle invalidRect = new Rectangle(0, 0, view.GetPanelSize().Width, view.GetPanelSize().Height);
                view.InvalidatePanel(invalidRect);
            }
        }

        /// <summary>
        /// G�re le clic de la souris. S�lectionne un n�ud ou d�marre un d�placement.
        /// </summary>
        /// <param name="e">Les arguments de l'�v�nement de clic de la souris.</param>
        public void OnMouseDown(MouseEventArgs e)
        {
            Node? clickedNode = GetClickedNode(e.Location);
            if (clickedNode != null)
            {
                Node? previousHighlightedNode = highlightedNode;
                highlightedNode = clickedNode == highlightedNode ? null : clickedNode;

                int parentId = model.GetParentId(clickedNode.NodeId);

                // V�rifie si le n�ud est une feuille (sans enfants)
                if (model.GetChildren(clickedNode.NodeId).Count == 0)
                {
                    // Affiche les informations de la feuille
                    view.DisplayLeafNodeDetails(clickedNode, parentId);
                }
                else
                {
                    // Affiche les informations de la racine ou d'un n�ud interm�diaire
                    view.DisplayBranchNodeDetails(clickedNode, parentId);
                }

                needsRedraw = true; // Indique que le redessin est n�cessaire

                // Calculer la zone � invalider
                Rectangle invalidRect = GetInvalidationRectangleForNode(clickedNode);
                if (previousHighlightedNode != null)
                {
                    invalidRect = Rectangle.Union(invalidRect, GetInvalidationRectangleForNode(previousHighlightedNode));
                }

                view.InvalidatePanel(invalidRect);
            }
            else
            {
                isDragging = true;
                lastMousePosition = e.Location;
            }
        }

        /// <summary>
        /// Retourne le nombre d'enfants d'un n�ud.
        /// </summary>
        /// <param name="node">Le n�ud � v�rifier.</param>
        /// <returns>Le nombre d'enfants du n�ud.</returns>
        public int GetChildrenCount(Node node)
        {
            var children = model.GetChildren(node.NodeId);
            return children.Count;
        }

        /// <summary>
        /// G�re le rel�chement du clic de la souris. Arr�te le d�placement de la vue.
        /// </summary>
        /// <param name="e">Les arguments de l'�v�nement de rel�chement de la souris.</param>
        public void OnMouseUp(MouseEventArgs e)
        {
            isDragging = false;
        }

        /// <summary>
        /// Retourne le n�ud cliqu� par l'utilisateur.
        /// </summary>
        /// <param name="clickPosition">Position du clic de la souris.</param>
        /// <returns>Le n�ud cliqu� ou null si aucun n�ud n'est trouv�.</returns>
        private Node? GetClickedNode(Point clickPosition)
        {
            PointF adjustedClickPosition = new PointF(
                (clickPosition.X - translation.X) / zoomFactor,
                (clickPosition.Y - translation.Y) / zoomFactor
            );

            return quadtree?.Query(adjustedClickPosition);
        }

        /// <summary>
        /// Dessine l'arbre de mani�re radiale.
        /// </summary>
        /// <param name="g">Objet Graphics utilis� pour le dessin.</param>
        public void DrawTreeRadial(Graphics g)
        {
            if (needsRedraw)
            {
                if (treeBitmap != null)
                {
                    treeBitmap.Dispose(); // Lib�re la m�moire de la bitmap pr�c�dente si elle existe
                }

                // Cr�er une nouvelle bitmap avec la taille du panneau
                Size panelSize = view.GetPanelSize();
                treeBitmap = new Bitmap(panelSize.Width, panelSize.Height);

                // Dessiner sur la bitmap avec un nouvel objet Graphics
                using (Graphics bitmapGraphics = Graphics.FromImage(treeBitmap))
                {
                    // Appliquer les transformations � la bitmap
                    bitmapGraphics.TranslateTransform(translation.X, translation.Y);
                    bitmapGraphics.ScaleTransform(zoomFactor, zoomFactor);

                    // Dessiner l'arbre sur le Graphics
                    DrawTreeOnGraphics(bitmapGraphics);
                }

                needsRedraw = false; // Ne pas redessiner jusqu'� un autre changement
            }

            // Dessiner la bitmap si elle n'est pas null
            if (treeBitmap != null)
            {
                g.DrawImageUnscaled(treeBitmap, 0, 0);
            }
        }

        /// <summary>
        /// Dessine l'arbre sur un objet Graphics.
        /// </summary>
        /// <param name="g">L'objet Graphics sur lequel dessiner l'arbre.</param>
        private void DrawTreeOnGraphics(Graphics g)
        {
            if (nodePositions.Count == 0)
            {
                CalculateNodePositions(); // Calculer les positions des n�uds si ce n'est pas d�j� fait
            }

            // Dessiner les lignes reliant les n�uds
            foreach (var nodePosition in nodePositions)
            {
                Node node = nodePosition.Key;
                Point position = nodePosition.Value;

                var children = model.GetChildren(node.NodeId);
                foreach (var child in children)
                {
                    if (nodePositions.TryGetValue(child, out Point childPos))
                    {
                        // V�rifier si la ligne est visible avant de dessiner
                        if (IsLineVisible(position, childPos))
                        {
                            bool isHighlighted = (highlightedNode != null) && (node == highlightedNode || IsDescendantOfHighlighted(node));
                            view.DrawLine(g, position, childPos, isHighlighted, nodeLevels[node]);
                        }
                    }
                }
            }

            // Dessiner les n�uds eux-m�mes
            foreach (var nodePosition in nodePositions)
            {
                Node node = nodePosition.Key;
                Point position = nodePosition.Value;

                if (IsNodeNearVisibleArea(position))
                {
                    bool isHighlighted = (highlightedNode != null) && (node == highlightedNode || IsDescendantOfHighlighted(node));
                    float nodeSize = 15; // Taille du n�ud
                    view.DrawNode(g, node, position, isHighlighted, nodeSize);
                }
            }
        }

        /// <summary>
        /// V�rifie si une ligne entre deux points est visible.
        /// </summary>
        /// <param name="p1">Premier point de la ligne.</param>
        /// <param name="p2">Deuxi�me point de la ligne.</param>
        /// <returns>True si la ligne est visible, sinon False.</returns>
        private bool IsLineVisible(Point p1, Point p2)
        {
            RectangleF visibleRect = new RectangleF(
                -translation.X / zoomFactor,
                -translation.Y / zoomFactor,
                view.GetPanelSize().Width / zoomFactor,
                view.GetPanelSize().Height / zoomFactor
            );

            float margin = 50f;

            RectangleF extendedRect = new RectangleF(
                visibleRect.Left - margin,
                visibleRect.Top - margin,
                visibleRect.Width + 2 * margin,
                visibleRect.Height + 2 * margin
            );

            return LineIntersectsRect(p1, p2, extendedRect);
        }

        /// <summary>
        /// V�rifie si une ligne croise un rectangle.
        /// </summary>
        /// <param name="p1">Premier point de la ligne.</param>
        /// <param name="p2">Deuxi�me point de la ligne.</param>
        /// <param name="rect">Le rectangle � v�rifier.</param>
        /// <returns>True si la ligne croise le rectangle, sinon False.</returns>
        private bool LineIntersectsRect(PointF p1, PointF p2, RectangleF rect)
        {
            return LineIntersectsLine(p1, p2, new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top)) ||
                   LineIntersectsLine(p1, p2, new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom)) ||
                   LineIntersectsLine(p1, p2, new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom)) ||
                   LineIntersectsLine(p1, p2, new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top)) ||
                   (rect.Contains(p1) && rect.Contains(p2));
        }

        /// <summary>
        /// V�rifie si deux segments de ligne se croisent.
        /// </summary>
        /// <param name="p1">Premier point du premier segment.</param>
        /// <param name="p2">Deuxi�me point du premier segment.</param>
        /// <param name="q1">Premier point du deuxi�me segment.</param>
        /// <param name="q2">Deuxi�me point du deuxi�me segment.</param>
        /// <returns>True si les segments se croisent, sinon False.</returns>
        private bool LineIntersectsLine(PointF p1, PointF p2, PointF q1, PointF q2)
        {
            float s1_x = p2.X - p1.X;
            float s1_y = p2.Y - p1.Y;
            float s2_x = q2.X - q1.X;
            float s2_y = q2.Y - q1.Y;

            float denominator = (-s2_x * s1_y + s1_x * s2_y);

            if (denominator == 0)
                return false; // Les lignes sont parall�les

            float s = (-s1_y * (p1.X - q1.X) + s1_x * (p1.Y - q1.Y)) / denominator;
            float t = (s2_x * (p1.Y - q1.Y) - s2_y * (p1.X - q1.X)) / denominator;

            return (s >= 0 && s <= 1 && t >= 0 && t <= 1);
        }

        /// <summary>
        /// Dessine un n�ud et ses enfants de mani�re radiale.
        /// </summary>
        /// <param name="g">Objet Graphics pour dessiner.</param>
        /// <param name="node">Le n�ud � dessiner.</param>
        /// <param name="position">Position du n�ud.</param>
        /// <param name="level">Niveau du n�ud dans l'arbre.</param>
        /// <param name="startAngle">Angle de d�part pour dessiner les enfants.</param>
        /// <param name="endAngle">Angle de fin pour dessiner les enfants.</param>
        /// <param name="maxDepth">Profondeur maximale � dessiner.</param>
        public void DrawNodeAndChildrenRadial(Graphics g, Node node, Point position, int level, float startAngle, float endAngle, int maxDepth)
        {
            if (level > maxDepth)
            {
                view.DrawCollapsedNodeIndicator(g, position);
                return;
            }

            // V�rifier si le n�ud est visible avant de le dessiner
            if (!IsNodeVisible(position))
            {
                // Ne pas dessiner ce n�ud ni ses enfants s'ils sont en dehors de la zone visible
                return;
            }

            nodePositions[node] = position;
            nodeLevels[node] = level; // Assigner le niveau

            // V�rifier si le n�ud est un cluster non expans�
            if (IsCluster(node) && !expandedClusters.Contains(node))
            {
                view.DrawCollapsedNodeIndicator(g, position);
                return;
            }

            // Dessiner le n�ud
            bool isHighlighted = (highlightedNode != null) && (node == highlightedNode || IsDescendantOfHighlighted(node));
            float nodeSize = 15; // Taille de base du n�ud
            view.DrawNode(g, node, position, isHighlighted, nodeSize);

            // Dessiner les enfants
            var children = model.GetChildren(node.NodeId);
            if (children.Count == 0 || level >= maxDepth) return;

            // Calculer l'angle pour bien r�partir les enfants
            float angleStep = (endAngle - startAngle) / children.Count;

            for (int i = 0; i < children.Count; i++)
            {
                Node child = children[i];
                float childStartAngle = startAngle + i * angleStep;
                float childEndAngle = childStartAngle + angleStep;
                float angle = (childStartAngle + childEndAngle) / 2;
                float radians = (float)(angle * Math.PI / 180);
                float baseRadius = 300 + (level * 100);
                Point childPos = new Point(
                    (int)(position.X + baseRadius * Math.Cos(radians)),
                    (int)(position.Y + baseRadius * Math.Sin(radians))
                );
                DrawNodeAndChildrenRadial(g, child, childPos, level + 1, childStartAngle, childEndAngle, maxDepth);
            }
        }

        /// <summary>
        /// V�rifie si un n�ud est un descendant du n�ud surlign�.
        /// </summary>
        /// <param name="node">Le n�ud � v�rifier.</param>
        /// <returns>True si le n�ud est un descendant du n�ud surlign�, sinon False.</returns>
        private bool IsDescendantOfHighlighted(Node node)
        {
            if (highlightedNode == null) return false;
            return IsDescendant(node, highlightedNode);
        }

        /// <summary>
        /// V�rifie si un n�ud est un descendant d'un autre n�ud.
        /// </summary>
        /// <param name="node">Le n�ud � v�rifier.</param>
        /// <param name="ancestor">Le n�ud anc�tre.</param>
        /// <returns>True si le n�ud est un descendant de l'anc�tre, sinon False.</returns>
        private bool IsDescendant(Node node, Node ancestor)
        {
            // Utiliser une cl� compos�e des ID du n�ud et de l'anc�tre
            var key = (node.NodeId, ancestor.NodeId);

            // Si le r�sultat est dans le cache, le retourner
            if (descendantCache.TryGetValue(key, out bool isDescendant))
            {
                return isDescendant;
            }

            // Si le n�ud est lui-m�me l'anc�tre, il est descendant
            if (node == ancestor)
            {
                descendantCache[key] = true;
                return true;
            }

            int parentId = model.GetParentId(node.NodeId);
            if (parentId == 0)
            {
                descendantCache[key] = false;
                return false;
            }

            Node? parentNode = model.GetNodeById(parentId);
            if (parentNode == null)
            {
                descendantCache[key] = false;
                return false;
            }

            // Calculer le r�sultat r�cursivement et le stocker dans le cache
            bool result = IsDescendant(parentNode, ancestor);
            descendantCache[key] = result;
            return result;
        }

        /// <summary>
        /// Calcule un facteur de zoom bas� sur la profondeur du n�ud surlign�.
        /// </summary>
        /// <returns>Le facteur de zoom calcul�.</returns>
        private float GetZoomDepthFactor()
        {
            if (highlightedNode != null)
            {
                int depth = GetNodeDepth(highlightedNode);
                return 1.0f / Math.Max(1, depth); // �vite la division par 0
            }
            return 1.0f;
        }


        /// <summary>
        /// Obtenir la profondeur d'un n�ud.
        /// La racine a une profondeur de 1, et chaque enfant a une profondeur �gale � celle de son parent + 1.
        /// </summary>
        /// <param name="node">Le n�ud dont on souhaite obtenir la profondeur.</param>
        /// <returns>La profondeur du n�ud.</returns>
        private int GetNodeDepth(Node node)
        {
            int depth = 0;
            Node? currentNode = node;
            // Parcourt les parents du n�ud jusqu'� la racine
            while (currentNode != null)
            {
                int parentId = model.GetParentId(currentNode.NodeId);
                currentNode = model.GetNodeById(parentId);
                depth++;
            }
            return depth;
        }

        /// <summary>
        /// R�initialise la vue, r�tablit le zoom par d�faut et recalcule les positions des n�uds.
        /// </summary>
        public void ResetView()
        {
            zoomFactor = 1.0f; // R�initialise le zoom au niveau par d�faut
            expandedClusters.Clear(); // Ferme tous les clusters
            RecalculateTranslation(); // Recentre l'arbre
            CalculateNodePositions(); // Recalcule les positions des n�uds
            needsRedraw = true; // Indique que le redessin de l'arbre est n�cessaire
            view.RefreshPanel(); // Rafra�chit l'affichage du panneau
        }

        /// <summary>
        /// Calcule les positions de tous les n�uds de l'arbre en partant de la racine.
        /// </summary>
        public void CalculateNodePositions()
        {
            Stopwatch stopwatch = Stopwatch.StartNew(); // D�marre un chronom�tre pour mesurer le temps de calcul

            // Sauvegarde les positions et niveaux des n�uds existants
            var existingNodePositions = new Dictionary<Node, Point>(nodePositions);
            var existingNodeLevels = new Dictionary<Node, int>(nodeLevels);

            nodePositions.Clear();
            nodeLevels.Clear();

            Node? rootNode = model.GetNodeById(1); // R�cup�re la racine de l'arbre
            if (rootNode != null)
            {
                // D�finit les limites du Quadtree pour optimiser les recherches spatiales
                Rectangle bounds = new Rectangle(-10000, -10000, 20000, 20000);
                quadtree = new Quadtree(bounds);

                nodePositions[rootNode] = origin; // Place la racine � la position d'origine
                nodeLevels[rootNode] = 0; // Le niveau de la racine est 0
                quadtree?.Insert(rootNode, Point.Round(origin)); // Ins�re la racine dans le Quadtree

                // R�cup�re les sous-arbres principaux (les enfants directs de la racine)
                var mainSubTrees = model.GetChildren(rootNode.NodeId);

                // Calcule les comptes des n�uds et d'autres informations pour chaque sous-arbre principal
                Dictionary<Node, int> subTreeCounts = new Dictionary<Node, int>();
                Dictionary<Node, bool> subTreeHasNodeWithAtLeastTenChildren = new Dictionary<Node, bool>();
                int totalCount = 0;

                foreach (var subTree in mainSubTrees)
                {
                    // Calculer le nombre de n�uds ayant plus de 8 enfants dans chaque sous-arbre
                    int count = model.CountNodesWithMoreThanEightChildren(subTree, 2, 10);
                    subTreeCounts[subTree] = count;
                    totalCount += count;

                    // V�rifie si le sous-arbre contient un n�ud avec au moins 10 enfants
                    bool hasNodeWithAtLeastTenChildren = model.HasNodeWithAtLeastTenChildren(subTree, 2, 10);
                    subTreeHasNodeWithAtLeastTenChildren[subTree] = hasNodeWithAtLeastTenChildren;
                }

                // Allouer les angles � chaque sous-arbre en fonction de leur importance
                float totalAngle = 360f; // Le cercle complet de l'arbre est 360 degr�s
                float minimumAnglePerSubTree = 36f; // Angle minimum par sous-arbre (10% de 360)
                float increasedMinimumAngle = 54f;  // Angle minimum augment� pour certains sous-arbres (15%)

                // Allocation initiale des angles
                Dictionary<Node, float> allocatedAngles = new Dictionary<Node, float>();
                float totalAllocatedAngle = 0f;

                foreach (var subTree in mainSubTrees)
                {
                    float minAngle = minimumAnglePerSubTree;

                    if (subTreeHasNodeWithAtLeastTenChildren[subTree])
                    {
                        minAngle = increasedMinimumAngle; // Donne plus d'angle si le sous-arbre est important
                    }

                    allocatedAngles[subTree] = minAngle;
                    totalAllocatedAngle += minAngle;
                }

                // Calcule l'angle restant � r�partir entre les sous-arbres
                float remainingAngle = totalAngle - totalAllocatedAngle;

                // Allouer l'angle restant proportionnellement au nombre de n�uds
                int totalCountsForAllocation = subTreeCounts.Values.Sum();
                foreach (var subTree in mainSubTrees)
                {
                    int count = subTreeCounts[subTree];
                    if (count > 0)
                    {
                        float additionalAngle = (count / (float)totalCountsForAllocation) * remainingAngle;
                        allocatedAngles[subTree] += additionalAngle;
                    }
                }

                // Calcul des angles de d�part et de fin pour chaque sous-arbre
                float currentAngle = 0f;
                Dictionary<Node, (float startAngle, float endAngle)> angleAllocations = new Dictionary<Node, (float, float)>();
                foreach (var subTree in mainSubTrees)
                {
                    float angle = allocatedAngles[subTree];
                    float startAngle = currentAngle;
                    float endAngle = currentAngle + angle;
                    angleAllocations[subTree] = (startAngle, endAngle);
                    currentAngle = endAngle;
                }

                // Positionner les sous-arbres principaux et calculer les positions r�cursivement
                foreach (var subTree in mainSubTrees)
                {
                    var angles = angleAllocations[subTree];
                    float angle = (angles.startAngle + angles.endAngle) / 2;
                    float radians = angle * (float)Math.PI / 180;

                    // Calculer la position du n�ud de niveau 1 (enfants directs de la racine)
                    int level = 1;
                    float baseRadius = 100 + level * 50;

                    PointF subTreePosition = new PointF(
                        origin.X + baseRadius * (float)Math.Cos(radians),
                        origin.Y + baseRadius * (float)Math.Sin(radians)
                    );

                    // Utiliser la position existante si disponible
                    if (existingNodePositions.TryGetValue(subTree, out Point existingPos))
                    {
                        subTreePosition = new PointF(existingPos.X, existingPos.Y);
                    }

                    nodePositions[subTree] = Point.Round(subTreePosition);
                    nodeLevels[subTree] = level;
                    quadtree?.Insert(subTree, Point.Round(subTreePosition));

                    // Appel r�cursif pour calculer la position des enfants du sous-arbre
                    CalculateNodePositionRecursive(subTree, subTreePosition, level + 1, angles.startAngle, angles.endAngle, existingNodePositions);
                }
            }
        }

        /// <summary>
        /// Calcule les positions des n�uds de mani�re r�cursive.
        /// </summary>
        /// <param name="node">Le n�ud courant.</param>
        /// <param name="position">La position du n�ud.</param>
        /// <param name="level">Le niveau du n�ud dans l'arbre.</param>
        /// <param name="startAngle">L'angle de d�part pour le positionnement des enfants.</param>
        /// <param name="endAngle">L'angle de fin pour le positionnement des enfants.</param>
        /// <param name="existingNodePositions">Les positions existantes des n�uds.</param>
        private void CalculateNodePositionRecursive(Node node, PointF position, int level, float startAngle, float endAngle, Dictionary<Node, Point> existingNodePositions)
        {
            // V�rifie si la position du n�ud est d�j� calcul�e
            if (existingNodePositions.TryGetValue(node, out Point existingPosition))
            {
                nodePositions[node] = existingPosition;
            }
            else
            {
                nodePositions[node] = Point.Round(position);
            }

            nodeLevels[node] = level;
            quadtree?.Insert(node, nodePositions[node]);

            var children = model.GetChildren(node.NodeId);
            if (children.Count == 0) return;

            if (IsCluster(node) && !expandedClusters.Contains(node))
            {
                return; // Ne pas calculer les enfants si le n�ud est un cluster non expans�
            }

            // R�arranger les enfants avant de calculer leurs positions
            var rearrangedChildren = RearrangeChildren(node, children);

            // Calcul de l'angle allou� pour les enfants
            float allocatedAngle = (endAngle - startAngle + 360f) % 360f;

            // Calcul de la taille du n�ud actuel et de la distance minimale entre n�uds
            float nodeSize = GetNodeSize(level);
            float D = nodeSize - 8; // Distance minimale entre les n�uds

            // Calcul du rayon de base pour le positionnement des enfants
            float baseRadius = level < 6 ? 300 + level * 100 : 300 + 5 * 100 + (level - 5) * 200;

            // Calcul de l'angle minimal pour �viter les chevauchements
            float theta_min = baseRadius > 0 ? (float)(2 * Math.Asin(D / (2 * baseRadius)) * (180 / Math.PI)) : 0f;
            float angleStep = Math.Max(allocatedAngle / rearrangedChildren.Count, theta_min);

            for (int i = 0; i < rearrangedChildren.Count; i++)
            {
                Node child = rearrangedChildren[i];
                float childStartAngle = startAngle + i * angleStep;
                float childEndAngle = childStartAngle + angleStep;
                float angle = (childStartAngle + childEndAngle) / 2;
                float radians = angle * (float)Math.PI / 180;

                // Calculer la position du n�ud enfant
                PointF childPos = new PointF(
                    position.X + baseRadius * (float)Math.Cos(radians),
                    position.Y + baseRadius * (float)Math.Sin(radians)
                );

                // Appel r�cursif pour calculer la position des enfants
                CalculateNodePositionRecursive(child, childPos, level + 1, childStartAngle, childEndAngle, existingNodePositions);
            }
        }

        /// <summary>
        /// Retourne la taille du n�ud en fonction de son niveau.
        /// </summary>
        /// <param name="level">Le niveau du n�ud dans l'arbre.</param>
        /// <returns>La taille du n�ud.</returns>
        private float GetNodeSize(int level)
        {
            float baseSize = 15f; // Taille de base
            float sizeReductionFactor = level >= 4 ? 0.5f : 1.0f;
            float size = baseSize / (1 + level * 0.3f) * zoomFactor * sizeReductionFactor;

            if (level > 7)
            {
                size *= 0.05f; // R�duire fortement la taille pour les niveaux �lev�s
            }

            return Math.Max(6f, Math.Min(20f, size)); // Limite la taille entre 6 et 20
        }


        /// <summary>
        /// R�arrange les enfants d'un n�ud parent en fonction de certaines conditions.
        /// Si un seul cluster est pr�sent et que les autres enfants sont des feuilles, ou si un seul enfant a des descendants,
        /// ils sont plac�s au centre, sinon un r�arrangement normal est effectu�.
        /// </summary>
        /// <param name="parentNode">Le n�ud parent dont les enfants doivent �tre r�arrang�s.</param>
        /// <param name="children">La liste des enfants du n�ud parent.</param>
        /// <returns>Une liste r�arrang�e des enfants.</returns>
        private List<Node> RearrangeChildren(Node parentNode, List<Node> children)
        {
            // V�rifier si tous les enfants sauf un sont des feuilles, et un est un cluster
            var clusters = children.Where(c => IsCluster(c)).ToList();
            var leaves = children.Where(c => !IsCluster(c) && model.GetDescendantCount(c.NodeId) == 0).ToList();

            if (clusters.Count == 1 && leaves.Count == children.Count - 1)
            {
                // Placer le cluster au milieu des feuilles
                return PlaceNodeInMiddle(clusters[0], leaves);
            }

            // V�rifier si un seul enfant a des descendants, et tous les autres sont des feuilles
            var nodesWithDescendants = children.Where(c => model.GetDescendantCount(c.NodeId) > 0).ToList();
            var nodesWithoutDescendants = children.Where(c => model.GetDescendantCount(c.NodeId) == 0).ToList();

            if (nodesWithDescendants.Count == 1 && nodesWithoutDescendants.Count == children.Count - 1)
            {
                // Placer cet enfant avec des descendants au milieu
                return PlaceNodeInMiddle(nodesWithDescendants[0], nodesWithoutDescendants);
            }

            // Si aucune condition sp�ciale n'est remplie, faire un r�arrangement normal
            return RearrangeChildrenNormally(children);
        }

        /// <summary>
        /// Place un n�ud au milieu de la liste et r�partit les autres n�uds autour de lui.
        /// </summary>
        /// <param name="middleNode">Le n�ud � placer au centre.</param>
        /// <param name="otherNodes">La liste des autres n�uds � r�partir autour.</param>
        /// <returns>La liste r�arrang�e avec le n�ud central au milieu.</returns>
        private List<Node> PlaceNodeInMiddle(Node middleNode, List<Node> otherNodes)
        {
            List<Node> rearrangedChildren = new List<Node>();
            int totalChildren = otherNodes.Count + 1;
            int middleIndex = totalChildren / 2;

            // Initialiser la liste avec des �l�ments nuls
            for (int i = 0; i < totalChildren; i++)
            {
                rearrangedChildren.Add(null);
            }

            // Placer le n�ud au centre
            rearrangedChildren[middleIndex] = middleNode;

            // R�partir les autres n�uds � gauche et � droite
            int left = middleIndex - 1;
            int right = middleIndex + 1;
            int index = 0;

            while (index < otherNodes.Count)
            {
                if (left >= 0)
                {
                    rearrangedChildren[left] = otherNodes[index];
                    left--;
                    index++;
                }
                if (index < otherNodes.Count && right < totalChildren)
                {
                    rearrangedChildren[right] = otherNodes[index];
                    right++;
                    index++;
                }
            }

            return rearrangedChildren;
        }

        /// <summary>
        /// R�arrange les enfants en utilisant un tri normal bas� sur le nombre de descendants.
        /// </summary>
        /// <param name="children">La liste des enfants � r�arranger.</param>
        /// <returns>Une liste r�arrang�e des enfants.</returns>
        private List<Node> RearrangeChildrenNormally(List<Node> children)
        {
            // Trier les enfants par ordre d�croissant du nombre de descendants
            var sortedChildren = children.OrderByDescending(c => c.DescendantCount).ToList();

            int count = sortedChildren.Count;
            if (count == 0) return new List<Node>();

            Node[] positions = new Node[count];

            // Placer les n�uds principaux : premier, dernier et milieu
            positions[0] = sortedChildren[0];
            if (count >= 2)
            {
                positions[count - 1] = sortedChildren[1];
            }
            if (count >= 3)
            {
                int middleIndex = count / 2;
                positions[middleIndex] = sortedChildren[2];
            }

            // R�partir les autres n�uds � gauche et � droite
            int leftIndex = 1;
            int rightIndex = count - 2;
            int sortedIndex = 3;

            while (sortedIndex < count)
            {
                if (leftIndex < count / 2)
                {
                    positions[leftIndex] = sortedChildren[sortedIndex];
                    leftIndex++;
                    sortedIndex++;
                }

                if (sortedIndex < count && rightIndex > count / 2)
                {
                    positions[rightIndex] = sortedChildren[sortedIndex];
                    rightIndex--;
                    sortedIndex++;
                }
            }

            // Si la condition sp�ciale des clusters est remplie, r�arranger les n�uds encore une fois
            int totalSiblings = count;
            var siblingsWithChildClusters = children.Where(c =>
                model.GetChildren(c.NodeId).Any(child => IsCluster(child))).ToList();

            int countSiblingsWithChildClusters = siblingsWithChildClusters.Count;

            if ((totalSiblings / 2) > countSiblingsWithChildClusters)
            {
                bool needsRearrangement = true;

                while (needsRearrangement)
                {
                    needsRearrangement = false;

                    for (int i = 0; i < positions.Length - 1; i++)
                    {
                        Node currentNode = positions[i];
                        Node nextNode = positions[i + 1];

                        bool currentHasChildCluster = model.GetChildren(currentNode.NodeId).Any(child => IsCluster(child));
                        bool nextHasChildCluster = model.GetChildren(nextNode.NodeId).Any(child => IsCluster(nextNode));

                        if (currentHasChildCluster && nextHasChildCluster)
                        {
                            // Trouver un candidat pour l'�change
                            int swapIndex = FindSwapCandidate(positions, i, i + 1);

                            if (swapIndex != -1)
                            {
                                // �changer les positions
                                Node temp = positions[i + 1];
                                positions[i + 1] = positions[swapIndex];
                                positions[swapIndex] = temp;

                                needsRearrangement = true;
                                break; // Recommencer la v�rification
                            }
                        }
                    }
                }
            }

            return positions.ToList();
        }

        /// <summary>
        /// Trouve un n�ud candidat pour un �change dans la liste des positions.
        /// </summary>
        /// <param name="positions">Le tableau des positions actuelles.</param>
        /// <param name="index1">L'index du premier n�ud.</param>
        /// <param name="index2">L'index du deuxi�me n�ud.</param>
        /// <returns>L'index du candidat pour l'�change, ou -1 si aucun candidat n'est trouv�.</returns>
        private int FindSwapCandidate(Node[] positions, int index1, int index2)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index1 || i == index2)
                    continue;

                Node potentialSwapNode = positions[i];
                bool potentialHasChildCluster = model.GetChildren(potentialSwapNode.NodeId).Any(child => IsCluster(child));

                if (!potentialHasChildCluster)
                {
                    // V�rifier si le n�ud a peu d'enfants directs
                    int directChildrenCount = model.GetChildren(potentialSwapNode.NodeId).Count;

                    // V�rifier si ses voisins ont peu d'enfants directs
                    bool neighborsHaveFewChildren = true;

                    if (i > 0)
                    {
                        Node leftNeighbor = positions[i - 1];
                        int leftNeighborChildren = model.GetChildren(leftNeighbor.NodeId).Count;
                        if (leftNeighborChildren > 5)
                            neighborsHaveFewChildren = false;
                    }

                    if (i < positions.Length - 1)
                    {
                        Node rightNeighbor = positions[i + 1];
                        int rightNeighborChildren = model.GetChildren(rightNeighbor.NodeId).Count;
                        if (rightNeighborChildren > 5)
                            neighborsHaveFewChildren = false;
                    }

                    if (neighborsHaveFewChildren && directChildrenCount <= 5)
                    {
                        return i; // Retourner l'indice du n�ud candidat
                    }
                }
            }

            // Si aucun candidat id�al n'est trouv�, rechercher un n�ud sans enfants clusters
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index1 || i == index2)
                    continue;

                Node potentialSwapNode = positions[i];
                bool potentialHasChildCluster = model.GetChildren(potentialSwapNode.NodeId).Any(child => IsCluster(child));

                if (!potentialHasChildCluster)
                {
                    return i; // Retourner l'indice du candidat pour l'�change
                }
            }

            return -1; // Aucun candidat trouv�
        }




        /// <summary>
        /// Obtient le niveau d'un n�ud dans l'arbre.
        /// </summary>
        /// <param name="node">Le n�ud pour lequel on veut conna�tre le niveau.</param>
        /// <returns>Le niveau du n�ud, ou 0 si le n�ud n'est pas trouv� dans les niveaux calcul�s.</returns>
        public int GetNodeLevel(Node node)
        {
            // V�rifie si le niveau du n�ud est d�j� calcul� et stock� dans nodeLevels
            if (nodeLevels.TryGetValue(node, out int level))
            {
                return level; // Retourner le niveau si trouv�
            }
            return 0; // Retourner 0 si le niveau du n�ud n'est pas trouv�
        }

        /// <summary>
        /// Recalcule la translation n�cessaire pour centrer l'arbre dans le panneau d'affichage.
        /// Cette m�thode ajuste les coordonn�es de translation pour que l'arbre soit correctement centr�,
        /// en tenant compte du facteur de zoom actuel.
        /// </summary>
        public void RecalculateTranslation()
        {
            // R�cup�re la taille actuelle du panneau o� l'arbre est affich�
            Size panelSize = view.GetPanelSize();

            // Calcule les coordonn�es du centre du panneau
            float panelCenterX = panelSize.Width / 2;
            float panelCenterY = panelSize.Height / 2;

            // Position d'origine du n�ud racine sans prendre en compte le zoom
            float nodeCenterX = origin.X;
            float nodeCenterY = origin.Y;

            // Calcule la translation n�cessaire pour centrer l'arbre � l'�cran
            translation.X = panelCenterX - (nodeCenterX * zoomFactor); // Ajustement horizontal
            translation.Y = panelCenterY - (nodeCenterY * zoomFactor); // Ajustement vertical
        }


    }
}