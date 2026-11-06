using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Linq;
using System.Windows; // Permet, avec <UseWPF>true</UseWPF> ajouté dans Updater.csproj, d'afficher des fenêtres de message depuis Updater


return Executer(args);

static int Executer(string[] args)
{
    try
    {
        // Vérification des arguments reçus
        if (args.Length != 3)
        {
            throw new ArgumentException("Les arguments de mise à jour sont invalides !");
        }

        // Récupération des arguments transmis
        string cheminArchive = args[0];
        string dossierInstallation = args[1];
        int idProcessus = int.Parse(args[2]);

        // Vérification de l'existence de l'archive
        if (!File.Exists(cheminArchive))
        {
            throw new FileNotFoundException("L'archive ZIP est introuvable !");
        }

        // Vérification du répertoire d'installation
        if (!Directory.Exists(dossierInstallation))
        {
            throw new DirectoryNotFoundException("Le dossier d'installation est introuvable !");
        }

        // Attente de la fermeture complète de GestionNotes
        try
        {
            using (Process processus = Process.GetProcessById(idProcessus))
            {
                processus.WaitForExit();
            }
        }
        catch (ArgumentException)
        {
            
        }

        // Création du dossier temporaire de l'extraction
        string dossierExtraction = Path.Combine(
            Path.GetTempPath(),
            "GestionNotes",
            "Extraction",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(dossierExtraction);

        // Extraction de l'archive ZIP
        ZipFile.ExtractToDirectory(cheminArchive, dossierExtraction);

        // Vérification aue l'extarction contient bien des fichiers
        if (!Directory.EnumerateFiles(dossierExtraction, "*", SearchOption.AllDirectories).Any())
        {
            throw new InvalidDataException("L'archive ne contient aucun fichier !");
        }

        // Sauvegarde de l'installation actuelle, pour pouvoir la restaurer si le remplacement échoue
        string dossierSauvegarde = dossierExtraction + "-sauvegarde";
        CopierFichiers(dossierInstallation, dossierSauvegarde);

        // Remplacement des fichiers de l'ancienne version
        try
        {
            CopierFichiers(dossierExtraction, dossierInstallation);
        }
        catch (Exception)
        {
            try
            {
                CopierFichiers(dossierSauvegarde, dossierInstallation);
            }
            catch (Exception)
            {
                // Le fichier qui bloque n'a pas été remplacé : ceux qui le précèdent sont déjà restaurés
            }

            throw;
        }

        // Suppression des dossiers d'extraction et de sauvegarde
        Directory.Delete(dossierExtraction, true);
        Directory.Delete(dossierSauvegarde, true);

        // Suppression de l'archive temporaire
        File.Delete(cheminArchive);

        // Redémarrage de GestionNotes
        string cheminApplication = Path.Combine(dossierInstallation, "GestionNotes.exe");

        // Vérification de la présence de GestionNotes.exe
        if (!File.Exists(cheminApplication))
        {
            throw new FileNotFoundException("GestionNotes.exe est introuvable !");
        }

        Demarrer(cheminApplication, dossierInstallation);

        return 0;
    }
    catch (Exception ex)
    {
        // Affichage de l'erreur
        MessageBox.Show("Une erreur est survenue durant la mise à jour !\n" + ex.Message,
            "Erreur de mise à jour",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // Relance de la version présente dans le dossier d'installation pour que l'application reste utilisable
        if (args.Length >= 2 && File.Exists(Path.Combine(args[1], "GestionNotes.exe")))
        {
            Demarrer(Path.Combine(args[1], "GestionNotes.exe"), args[1]);
        }

        return 1;
    }

    static void Demarrer(string cheminApplication, string dossierInstallation)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = cheminApplication,
            WorkingDirectory = dossierInstallation,
            UseShellExecute = true
        });
    }

    static void CopierFichiers(string source, string destination)
    {
        // Création des sous-dossiers
        foreach (string dossier in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            string cheminRelatif = Path.GetRelativePath(source, dossier);
            string nouveauDossier = Path.Combine(destination, cheminRelatif);

            Directory.CreateDirectory(nouveauDossier);
        }

        // Copie des fichiers
        foreach (string fichier in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string cheminRelatif = Path.GetRelativePath(source, fichier);

            // Permet de ne pas remplacer le programme auxilliaire en cours (Updater.exe, Updater.dll, etc.)
            if (Path.GetFileName(cheminRelatif).StartsWith("Updater.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string destinationFichier = Path.Combine(destination, cheminRelatif);
            string dossierParent = Path.GetDirectoryName(destinationFichier);

            if (dossierParent != null)
            {
                Directory.CreateDirectory(dossierParent);
            }

            File.Copy(fichier, destinationFichier, true);
        }
    }
}
