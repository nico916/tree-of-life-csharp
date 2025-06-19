using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace TreeOfLifeApp
{
    /// <summary>
    /// La classe Form1 représente la fenêtre principale de l'application "Arbre de Vie".
    /// Elle gère l'affichage de l'arbre, les interactions utilisateur et l'affichage des détails des nœuds.
    /// </summary>
    public partial class Form1 : Form
    {
        private ArbreDeVieController controller;
        private Panel panelNodeDetails; // Panel pour contenir le Label et le LinkLabel
        private DoubleBufferedPanel panelArbreDeVie;
        private Font nodeFont;
        private float lastFontSize = -1f;

        /// <summary>
        /// Initialise une nouvelle instance de la classe Form1.
        /// Configure les composants de l'interface utilisateur et initialise le contrôleur.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // Initialisation du Panel pour dessiner l'arbre (avec double-buffering activé)
            panelArbreDeVie = new DoubleBufferedPanel
            {
                Location = new Point(12, 12),
                Size = new Size(1200, 800),
                AutoScroll = true
            };

            panelArbreDeVie.Paint += PanelArbreDeVie_Paint;
            this.Controls.Add(panelArbreDeVie);

            // Instancie le contrôleur avec les chemins vers les fichiers CSV
            controller = new ArbreDeVieController("treeoflife_nodes.csv", "treeoflife_links.csv", this);

            // Initialisation du Panel pour afficher les détails du nœud
            panelNodeDetails = new Panel
            {
                Location = new Point(1250, 50),
                Size = new Size(250, 180),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelNodeDetails);

            // Ajouter le bouton de recentrage
            Button recenterButton = new Button
            {
                Text = "Recentrer l'arbre",
                Location = new Point(1250, 250),
                Size = new Size(150, 30)
            };
            recenterButton.Click += RecenterButton_Click;
            this.Controls.Add(recenterButton);

            nodeFont = new Font("Arial", 10);

            // Associer les événements de la souris et des touches pour zoomer et naviguer
            panelArbreDeVie.MouseMove += PanelArbreDeVie_MouseMove;
            panelArbreDeVie.MouseWheel += PanelArbreDeVie_MouseWheel;
            panelArbreDeVie.MouseDown += PanelArbreDeVie_MouseDown;
            panelArbreDeVie.MouseUp += PanelArbreDeVie_MouseUp;

            panelArbreDeVie.Invalidate(); // Dessiner l'arbre au démarrage
        }

        /// <summary>
        /// Invalide une région spécifique du panelArbreDeVie, déclenchant un redessin.
        /// </summary>
        /// <param name="rect">La région à invalider.</param>
        public void InvalidatePanel(Rectangle rect)
        {
            panelArbreDeVie.Invalidate(rect);
        }

        /// <summary>
        /// Gère l'événement Paint du panelArbreDeVie.
        /// Dessine l'arrière-plan dégradé et l'arbre radial.
        /// </summary>
        private void PanelArbreDeVie_Paint(object? sender, PaintEventArgs? e)
        {
            if (e != null)
            {
                Graphics g = e.Graphics;

                // Dessiner un dégradé pour l'arrière-plan
                using (LinearGradientBrush brush = new LinearGradientBrush(panelArbreDeVie.ClientRectangle, Color.LightGreen, Color.LightBlue, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, panelArbreDeVie.ClientRectangle);
                }

                controller.DrawTreeRadial(g);
            }
        }

        /// <summary>
        /// Gère l'événement MouseMove du panelArbreDeVie.
        /// Transmet l'événement au contrôleur pour traitement.
        /// </summary>
        private void PanelArbreDeVie_MouseMove(object? sender, MouseEventArgs? e)
        {
            if (e != null)
            {
                controller.OnMouseMove(e);
            }
        }

        /// <summary>
        /// Gère l'événement MouseWheel du panelArbreDeVie.
        /// Transmet l'événement au contrôleur pour traitement du zoom.
        /// </summary>
        private void PanelArbreDeVie_MouseWheel(object? sender, MouseEventArgs? e)
        {
            if (e != null)
            {
                controller.OnMouseWheel(e);
            }
        }

        /// <summary>
        /// Gère l'événement MouseDown du panelArbreDeVie.
        /// Transmet l'événement au contrôleur et invalide le panel pour redessin.
        /// </summary>
        private void PanelArbreDeVie_MouseDown(object? sender, MouseEventArgs? e)
        {
            if (e != null)
            {
                controller.OnMouseDown(e);
                panelArbreDeVie.Invalidate();
            }
        }

        /// <summary>
        /// Gère l'événement MouseUp du panelArbreDeVie.
        /// Transmet l'événement au contrôleur.
        /// </summary>
        private void PanelArbreDeVie_MouseUp(object? sender, MouseEventArgs? e)
        {
            if (e != null)
            {
                controller.OnMouseUp(e);
            }
        }

        /// <summary>
        /// Rafraîchit l'affichage du panelArbreDeVie en invalidant l'ensemble du panel.
        /// </summary>
        public void RefreshPanel()
        {
            panelArbreDeVie.Invalidate();
        }

        /// <summary>
        /// Affiche les détails d'un nœud feuille dans le panelNodeDetails.
        /// </summary>
        /// <param name="node">Le nœud feuille à afficher.</param>
        /// <param name="parentId">L'ID du parent du nœud.</param>
        public void DisplayLeafNodeDetails(Node node, int parentId)
        {
            // Description basée sur le niveau de confiance
            string confidenceDescription = node.Confidence switch
            {
                0 => "Position confiante",
                1 => "Position problématique",
                2 => "Position non spécifiée",
                _ => "Inconnue"
            };

            // Description basée sur la phylesis
            string phylesisDescription = node.Phylesis switch
            {
                "0" => "Monophylétique",
                "1" => "Monophylie incertaine",
                "2" => "Non monophylétique",
                _ => "Inconnu"
            };

            // Effacer les contrôles existants pour mettre à jour les informations
            panelNodeDetails.Controls.Clear();

            // Créer un Label pour indiquer si l'espèce est éteinte ou vivante
            Label statusLabel = new Label
            {
                AutoSize = true,
                Text = node.IsExtinct ? "Espèce éteinte" : "Espèce vivante",
                ForeColor = node.IsExtinct ? Color.Red : Color.Green, // Rouge pour éteinte, vert pour vivante
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(5, 5)
            };
            panelNodeDetails.Controls.Add(statusLabel);

            // Création d'un Label pour les détails du nœud
            Label labelNodeInfo = new Label
            {
                AutoSize = true,
                Text = $"Nom : {node.NodeName}\n" +
                       $"ParentId : {parentId}\n" +
                       $"Est éteint : {(node.IsExtinct ? "Oui" : "Non")}\n" +
                       $"Confiance : {confidenceDescription}\n" +
                       $"Phylesis : {phylesisDescription}",
                Location = new Point(5, statusLabel.Bottom + 10) // Placer sous le label de statut
            };
            panelNodeDetails.Controls.Add(labelNodeInfo);

            // Création du LinkLabel pour le TolOrgLink si disponible
            if (!string.IsNullOrEmpty(node.TolOrgLink) && node.TolOrgLink != "0")
            {
                LinkLabel linkTolOrg = new LinkLabel
                {
                    AutoSize = true,
                    Text = $"http://tolweb.org/{node.NodeName}/{node.NodeId}",
                    Location = new Point(5, labelNodeInfo.Bottom + 10),
                    Visible = true
                };
                linkTolOrg.LinkClicked += (s, e) =>
                {
                    Process.Start(new ProcessStartInfo(linkTolOrg.Text) { UseShellExecute = true });
                };
                panelNodeDetails.Controls.Add(linkTolOrg);
            }
        }

        /// <summary>
        /// Affiche les détails d'un nœud branche (racine ou avec des enfants) dans le panelNodeDetails.
        /// </summary>
        /// <param name="node">Le nœud à afficher.</param>
        /// <param name="parentId">L'ID du parent du nœud.</param>
        public void DisplayBranchNodeDetails(Node node, int parentId)
        {
            // Description basée sur le niveau de confiance
            string confidenceDescription = node.Confidence switch
            {
                0 => "Position confiante",
                1 => "Position problématique",
                2 => "Position non spécifiée",
                _ => "Inconnue"
            };

            // Description basée sur la phylesis
            string phylesisDescription = node.Phylesis switch
            {
                "0" => "Monophylétique",
                "1" => "Monophylie incertaine",
                "2" => "Non monophylétique",
                _ => "Inconnu"
            };

            // Effacer les contrôles existants pour mettre à jour les informations
            panelNodeDetails.Controls.Clear();

            // Créer un Label pour indiquer si l'espèce est éteinte ou vivante
            Label statusLabel = new Label
            {
                AutoSize = true,
                Text = node.IsExtinct ? "Espèce éteinte" : "Espèce vivante",
                ForeColor = node.IsExtinct ? Color.Red : Color.Green, // Rouge pour éteinte, vert pour vivante
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(5, 5)
            };
            panelNodeDetails.Controls.Add(statusLabel);

            // Création d'un Label pour les détails du nœud
            Label labelNodeInfo = new Label
            {
                AutoSize = true,
                Text = $"Nom : {node.NodeName}\n" +
                       $"ParentId : {parentId}\n" +
                       $"Nombre d'enfants : {controller.GetChildrenCount(node)}\n" +
                       $"Est éteint : {(node.IsExtinct ? "Oui" : "Non")}\n" +
                       $"Confiance : {confidenceDescription}\n" +
                       $"Phylesis : {phylesisDescription}",
                Location = new Point(5, statusLabel.Bottom + 10) // Placer sous le label de statut
            };
            panelNodeDetails.Controls.Add(labelNodeInfo);

            // Création du LinkLabel pour le TolOrgLink si disponible
            if (!string.IsNullOrEmpty(node.TolOrgLink) && node.TolOrgLink != "0")
            {
                LinkLabel linkTolOrg = new LinkLabel
                {
                    AutoSize = true,
                    Text = $"http://tolweb.org/{node.NodeName}/{node.NodeId}",
                    Location = new Point(5, labelNodeInfo.Bottom + 10),
                    Visible = true
                };
                linkTolOrg.LinkClicked += (s, e) =>
                {
                    Process.Start(new ProcessStartInfo(linkTolOrg.Text) { UseShellExecute = true });
                };
                panelNodeDetails.Controls.Add(linkTolOrg);
            }
        }

        /// <summary>
        /// Gère le clic sur le bouton de recentrage.
        /// Réinitialise la vue et rafraîchit le panel d'arbre de vie.
        /// </summary>
        private void RecenterButton_Click(object? sender, EventArgs? e)
        {
            controller.ResetView();
            RefreshPanel();
        }

        /// <summary>
        /// Dessine un nœud et ses informations sur le Graphics fourni.
        /// </summary>
        /// <param name="g">Le contexte graphique pour le dessin.</param>
        /// <param name="node">Le nœud à dessiner.</param>
        /// <param name="position">La position du nœud sur le panel.</param>
        /// <param name="isHighlighted">Indique si le nœud est mis en évidence.</param>
        /// <param name="nodeSize">La taille du nœud.</param>
        public void DrawNode(Graphics g, Node node, Point position, bool isHighlighted, float nodeSize)
        {
            int level = controller.GetNodeLevel(node);

            // Ajuster la taille du nœud en fonction du niveau
            float sizeReductionFactor = level >= 4 ? 0.5f : 1.0f;
            float adjustedNodeSize = nodeSize / (1 + level * 0.3f) * sizeReductionFactor;
            adjustedNodeSize = Math.Max(2, Math.Min(20, adjustedNodeSize)); // Limiter la taille du nœud

            Rectangle rect = new Rectangle(
                position.X - (int)(adjustedNodeSize / 2),
                position.Y - (int)(adjustedNodeSize / 2),
                (int)adjustedNodeSize,
                (int)adjustedNodeSize
            );

            // Définir la couleur du nœud en fonction de ses propriétés
            Brush nodeBrush;
            if (isHighlighted)
            {
                nodeBrush = new SolidBrush(Color.Gold); // Surbrillance
            }
            else if (controller.IsCluster(node))
            {
                nodeBrush = new SolidBrush(Color.Orange); // Couleur pour les clusters
            }
            else if (node.IsExtinct)
            {
                nodeBrush = new SolidBrush(Color.Gray); // Couleur pour les espèces éteintes
            }
            else
            {
                // Couleur dynamique basée sur le niveau de profondeur
                float hue = (level / 10f) * 360f; // Changement de teinte en fonction du niveau
                nodeBrush = new SolidBrush(ColorFromHSV(hue, 0.7, 0.9)); // Utilisation d'une méthode HSV pour générer les couleurs
            }

            // Dessiner le nœud (utilisation d'une ellipse pour représenter les nœuds)
            g.FillEllipse(nodeBrush, rect);

            // Ajuster l'épaisseur du contour en fonction du niveau (plus profond = trait plus fin)
            float borderThickness = Math.Max(0.1f, 0.8f - (level * 0.2f)); // Réduire l'épaisseur avec la profondeur
            using (Pen borderPen = new Pen(isHighlighted ? Color.Gold : Color.Black, borderThickness))
            {
                g.DrawEllipse(borderPen, rect); // Dessiner le contour
            }

            // Ajout de l'effet de lueur pour les nœuds sélectionnés
            if (isHighlighted)
            {
                using (Pen glowPen = new Pen(Color.FromArgb(128, Color.Gold), borderThickness + 2f))
                {
                    g.DrawEllipse(glowPen, rect); // Effet de lueur en dehors du nœud
                }
            }

            // Ajuster la taille de la police en fonction du niveau de profondeur
            float baseFontSize = adjustedNodeSize / 2;
            if (level >= 5)
            {
                baseFontSize *= 0.50f; // Diminuer la taille de la police au-delà du niveau 5
            }
            baseFontSize = Math.Max(6, baseFontSize); // Taille minimale de police

            if (Math.Abs(lastFontSize - baseFontSize) > 0.1f)
            {
                nodeFont.Dispose();
                nodeFont = new Font("Segoe UI", baseFontSize); // Utilisation d'une police moderne et lisible
                lastFontSize = baseFontSize;
            }

            // Affichage du nom et de l'ID du nœud (affiché uniquement pour les clusters ou les nœuds de haut niveau)
            if (controller.IsCluster(node) || level <= 2 || isHighlighted)
            {
                g.DrawString($"{node.NodeName} (ID: {node.NodeId})", nodeFont, Brushes.Black, position.X + adjustedNodeSize, position.Y - adjustedNodeSize);
            }
        }

        /// <summary>
        /// Génère une couleur basée sur les valeurs HSV.
        /// </summary>
        /// <param name="hue">La teinte (0-360 degrés).</param>
        /// <param name="saturation">La saturation (0-1).</param>
        /// <param name="value">La valeur (0-1).</param>
        /// <returns>La couleur correspondante en RGB.</returns>
        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0:
                    return Color.FromArgb(255, v, t, p);
                case 1:
                    return Color.FromArgb(255, q, v, p);
                case 2:
                    return Color.FromArgb(255, p, v, t);
                case 3:
                    return Color.FromArgb(255, p, q, v);
                case 4:
                    return Color.FromArgb(255, t, p, v);
                default:
                    return Color.FromArgb(255, v, p, q);
            }
        }

        /// <summary>
        /// Dessine une ligne entre deux points avec un dégradé et des effets de mise en évidence.
        /// </summary>
        /// <param name="g">Le contexte graphique pour le dessin.</param>
        /// <param name="parentPos">La position du parent.</param>
        /// <param name="childPos">La position de l'enfant.</param>
        /// <param name="isHighlighted">Indique si la ligne est mise en évidence.</param>
        /// <param name="level">Le niveau de profondeur de la ligne.</param>
        public void DrawLine(Graphics g, Point parentPos, Point childPos, bool isHighlighted, int level)
        {
            // Ajuster l'épaisseur des lignes en fonction de la profondeur
            float lineWidth = Math.Max(0.5f, 1.5f - (level * 0.2f)); // Plus profond = trait plus fin, avec un minimum de 0.5

            // Couleurs dynamiques basées sur la profondeur et la mise en évidence
            Color baseColor = isHighlighted ? Color.Gold : Color.FromArgb(50 + level * 20, 0, 0, 128); // Couleur dégradée vers bleu foncé
            Color gradientColor = isHighlighted ? Color.Orange : Color.FromArgb(100 + level * 15, 0, 128, 128); // Transition vers bleu turquoise

            // Création d'un pinceau linéaire avec un dégradé pour la ligne
            using (LinearGradientBrush gradientBrush = new LinearGradientBrush(parentPos, childPos, baseColor, gradientColor))
            {
                using (Pen pen = new Pen(gradientBrush, lineWidth))
                {
                    // Ajustement pour la mise en évidence : ajouter un effet de surbrillance
                    if (isHighlighted)
                    {
                        pen.DashStyle = DashStyle.Dot; // Ligne pointillée si mise en évidence
                        pen.Width += 2f; // Augmenter la largeur de la ligne si surbrillance
                    }
                    else
                    {
                        pen.DashStyle = DashStyle.Solid; // Ligne solide par défaut
                    }

                    // Dessiner la ligne entre le parent et l'enfant
                    g.DrawLine(pen, parentPos.X, parentPos.Y, childPos.X, childPos.Y);
                }

                // Ajout d'un effet lumineux pour les lignes en surbrillance
                if (isHighlighted)
                {
                    using (Pen glowPen = new Pen(Color.FromArgb(100, Color.Gold), lineWidth + 3f))
                    {
                        glowPen.DashStyle = DashStyle.Solid; // Solide pour l'effet lumineux
                        g.DrawLine(glowPen, parentPos.X, parentPos.Y, childPos.X, childPos.Y);
                    }
                }
            }
        }

        /// <summary>
        /// Dessine un indicateur pour un nœud réduit (cluster non développé).
        /// </summary>
        /// <param name="g">Le contexte graphique pour le dessin.</param>
        /// <param name="position">La position du nœud sur le panel.</param>
        public void DrawCollapsedNodeIndicator(Graphics g, Point position)
        {
            int indicatorSize = 5; // Taille de l'indicateur
            Rectangle rect = new Rectangle(position.X - indicatorSize, position.Y - indicatorSize, indicatorSize * 2, indicatorSize * 2);
            g.FillEllipse(Brushes.Gray, rect); // Dessiner un petit cercle gris
            g.DrawEllipse(Pens.Black, rect);   // Contour du cercle
        }

        /// <summary>
        /// Obtient la taille actuelle du panelArbreDeVie.
        /// </summary>
        /// <returns>La taille du panelArbreDeVie.</returns>
        public Size GetPanelSize()
        {
            return panelArbreDeVie.Size;
        }
    }

    /// <summary>
    /// La classe DoubleBufferedPanel est une extension de Panel avec le double-buffering activé pour éviter les scintillements lors du dessin.
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe DoubleBufferedPanel avec le double-buffering activé.
        /// </summary>
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
        }
    }
}