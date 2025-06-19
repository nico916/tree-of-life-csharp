namespace TreeOfLifeApp
{
    partial class Form1
    {
        /// <summary>
        /// Libère les ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            // Supprimer la ligne ci-dessous qui fait référence à 'components'
            // if (disposing && (components != null))
            // {
            //     components.Dispose();
            // }

            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du Concepteur ; ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            // Configuration des propriétés du formulaire
            this.ClientSize = new System.Drawing.Size(900, 700);
            this.Text = "Arbre de Vie";
            this.WindowState = FormWindowState.Maximized;
        }

        #endregion
    }
}