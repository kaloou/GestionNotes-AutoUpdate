using GestionNotes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GestionNotes.Services
{
    class ServiceMiseAJour
    {
        // Définition de l'emplacement du manifeste version.json (ce fichier contient les infos de la dernière version disponible de l'application)
        private readonly string _cheminManifeste = @"\\WIN-V9MDIVV3MVO\GestionNotes-Depot\version.json";

        public VersionInfo VerifierVersion(string versionInstallee)
        {
            try
            {
                // Stockage du contenu du manifeste dans une chaine de caractères
                string contenu = File.ReadAllText(_cheminManifeste);

                // Conversion du texte JSON en un objet de type VersionInfo
                VersionInfo info = JsonSerializer.Deserialize<VersionInfo>(contenu);

                // Vérification de la validité du manifeste (l'objet info doit exister, "version" et "fichier" ne doivent pas être vides dans le fichier version.json)
                if (info == null || string.IsNullOrWhiteSpace(info.version) || string.IsNullOrWhiteSpace(info.fichier))
                {
                    throw new InvalidDataException("La manifeste est invalide !");
                }

                // Vérification du numéro de version disponible
                if (!Version.TryParse(info.version, out Version versionDisponible))
                {
                    throw new InvalidDataException("Le numéro de version est invalide !");
                }

                // Vérification du numéro de version actuel
                if (!Version.TryParse(versionInstallee, out Version versionActuelle))
                {
                    throw new InvalidDataException("Le version installée est invalide !");
                }

                // La nouvelle version doit être supérieure à la version installée, sinon aucune maj n'est dispo
                return versionDisponible > versionActuelle ? info : null;
            } // Gestion des erreurs
            catch (IOException)
            {
                throw new InvalidOperationException("Le dépôt réseau est inaccessible !");
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException("L'accès au dépôt réseau est refusé !");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Le fichier version.json est invalide !");
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
        }

        public string TelechargerMiseAJour(VersionInfo info)
        {
            try
            {
                // Récupération du nom du fichier de la version
                string nomFichier = info.fichier;

                // Vérification du nom du fichier de la version (il doit s'agir d'une archive zip)
                if (!string.Equals(nomFichier, Path.GetFileName(nomFichier), StringComparison.Ordinal) || nomFichier == "." || nomFichier == "..")
                {
                    throw new InvalidDataException("Le nom de l'archive ZIP est invalide !");
                }

                // Récupération du chemin du dossier qui contient le manifeste version.json
                string cheminDossierManifeste = Path.GetDirectoryName(_cheminManifeste);

                // Récupération du chemin du fichier de version (archive zip)
                string cheminArchive = Path.Combine(cheminDossierManifeste, nomFichier);

                // Vérification de la présence de l'archive ZIP dans le dépôt réseau
                if (!File.Exists(cheminArchive))
                {
                    throw new FileNotFoundException("L'archive ZIP est absente du dépôt réseau !", cheminArchive);
                }

                // Création d'un répertoire temporaire local pour conserver l'archive zip téléchargée
                string dossierTemporaire = Path.Combine(Path.GetTempPath(), "GestionNotes", "MisesAJour", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dossierTemporaire);

                // Récupération du chemin de la copie locale
                string cheminLocal = Path.Combine(dossierTemporaire, nomFichier);

                // Récupération de la taille de l'archive source
                long tailleSource = new FileInfo(cheminArchive).Length;

                // Copie de l'archive depuis le dépôt réseau vers le disque local
                File.Copy(cheminArchive, cheminLocal, false);

                // Vérification de l'existence et de la taille de la copie locale
                if (!File.Exists(cheminLocal))
                {
                    throw new IOException("La copie locale de l'archive n'a pas été créée !");
                }

                long tailleCopie = new FileInfo(cheminLocal).Length;

                if (tailleCopie != tailleSource)
                {
                    // Suppression de la copie incomplète
                    File.Delete(cheminLocal);

                    throw new IOException("La copie de l'archive ZIP est incomplète !");
                }

                // Vérification que l'archive est lisible et contient l'application, avant de fermer GestionNotes
                using (ZipArchive archive = ZipFile.OpenRead(cheminLocal))
                {
                    if (archive.GetEntry("GestionNotes.exe") == null)
                    {
                        throw new InvalidDataException("L'archive ZIP ne contient pas GestionNotes.exe à sa racine !");
                    }
                }

                // Retourne le chemin local qui sera utilisé pour appliquer la mise à jour
                return cheminLocal;
            }
            catch (FileNotFoundException)
            {
                throw new InvalidOperationException("L'archive ZIP est introuvable dans le dépôt réseau !");
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException("L'accès au fichier ou au répertoire de mise à jour est refusé !");
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Impossible de copier correctement l'archive ZIP : " + ex.Message, ex);
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
        }
    }
}
