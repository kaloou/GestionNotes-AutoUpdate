using GestionNotes.Services;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace GestionNotes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // La méthode OnStartup est appelée au démarrage
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Création d'une instance du service
                ServiceMiseAJour service = new ServiceMiseAJour();

                // Récupération de la version de l'assembly actuellement exécutée
                // ATTENTION : nécessite de l'avoir préalablement définie dans les fichiers .csproj des différenetes versions
                // REM : ce fichier est accessible via un clic droit sur le projet GestionNotes, puis "Modifier le fichier projet"
                string versionInstallee = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

                // Vérification de la présence ou non d'une version suppérieur à la version 1.0
                var nouvelleVerion = service.VerifierVersion(versionInstallee);

                // Affichage du message si une maj est dispo
                if (nouvelleVerion != null)
                {
                    MessageBoxResult resultat = MessageBox.Show($"Une nouvelle version est disponible : {nouvelleVerion.version}\n" +
                        $"Archive : {nouvelleVerion.fichier}\n" +
                        $"Souhaitez-vous télécharger cette mise à jour ?",
                        "Mise à jour disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultat == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // Téléchargement de l'archive zip
                            string cheminLocal = service.TelechargerMiseAJour(nouvelleVerion);

                            // Récupération du répertoire d'installation de GestionNotes
                            string dossierInstallation = AppDomain.CurrentDomain.BaseDirectory;

                            // Récupération du chemin de Updater.exe
                            string cheminUpdater = Path.Combine(dossierInstallation, "Updater.exe");

                            // Vérification de la présence du programme auxiliaire (Updater.exe)
                            if (!File.Exists(cheminUpdater))
                            {
                                throw new InvalidOperationException("Le programme Updater.exe est introuvable !");
                            }

                            // Préparation des arguments nécessaires
                            string arguments = $"\"{cheminLocal}\" " +
                                $"\"{dossierInstallation}\" " +
                                $"{System.Diagnostics.Process.GetCurrentProcess().Id}";

                            // Préparation du lancement de Updater
                            ProcessStartInfo demarrage = new ProcessStartInfo
                            {
                                FileName = cheminUpdater,
                                Arguments = arguments,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            //Message de succès
                            MessageBox.Show("La mise à jour a été téléchargée avec succès !" +
                               "\nEmplacment : " + cheminLocal,
                               "Téléchargement terminé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                            // Lancement du programme auxilliaire
                            Process.Start(demarrage);

                            // Fermeture de GestionNotes
                            Application.Current.Shutdown();
                        }
                        catch (InvalidOperationException ex)
                        {
                            MessageBox.Show(ex.Message,
                                "\nErreur de téléchargement",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                }

            } // L'application continue même si aucune maj n'est trouvée
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message +
                    "\nGestionNotes va continuer normalement...",
                    "Vérification des versions",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

}
