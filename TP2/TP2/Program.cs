using System;
using System.Windows.Forms;

namespace TreeOfLifeApp
{
    static class Program
    {
        /// <summary>
        /// Point d'entr�e principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1()); // Lancer le Form1a
        }
    }
}