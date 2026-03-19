🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Un serveur Git auto-heberge avec une interface web similaire a GitHub, construit avec ASP.NET Core et Blazor Server. Parcourez les depots, gerez les tickets, pull requests, wikis, projets et bien plus — le tout depuis votre propre machine ou serveur.

![Capture d'ecran MyPersonalGit](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)

---

## Table des Matieres

- [Fonctionnalites](#fonctionnalites)
- [Stack Technique](#stack-technique)
- [Demarrage Rapide](#demarrage-rapide)
  - [Docker (Recommande)](#docker-recommande)
  - [Executer Localement](#executer-localement)
  - [Variables d'Environnement](#variables-denvironnement)
- [Utilisation](#utilisation)
  - [Se Connecter](#1-se-connecter)
  - [Creer un Depot](#2-creer-un-depot)
  - [Cloner et Pousser](#3-cloner-et-pousser)
  - [Cloner depuis un IDE](#4-cloner-depuis-un-ide)
  - [Editeur Web](#5-utiliser-lediteur-web)
  - [Registre de Conteneurs](#6-registre-de-conteneurs)
  - [Registre de Paquets](#7-registre-de-paquets)
  - [Pages (Sites Statiques)](#8-pages-hebergement-de-sites-statiques)
  - [Notifications Push](#9-notifications-push)
  - [Authentification par Cle SSH](#10-authentification-par-cle-ssh)
  - [LDAP / Active Directory](#11-authentification-ldap--active-directory)
  - [Secrets du Depot](#12-secrets-du-depot)
  - [Connexion OAuth / SSO](#13-connexion-oauth--sso)
  - [Importer un Depot](#14-importer-un-depot)
  - [Forks et Synchronisation Upstream](#15-forks-et-synchronisation-upstream)
  - [Auto-Release CI/CD](#16-auto-release-cicd)
  - [Flux RSS/Atom](#17-flux-rssatom)
- [Configuration de la Base de Donnees](#configuration-de-la-base-de-donnees)
  - [Utiliser PostgreSQL](#utiliser-postgresql)
  - [Changer depuis le Tableau de Bord Admin](#changer-depuis-le-tableau-de-bord-admin)
  - [Choisir une Base de Donnees](#choisir-une-base-de-donnees)
- [Deployer sur un NAS](#deployer-sur-un-nas)
- [Configuration](#configuration)
- [Structure du Projet](#structure-du-projet)
- [Executer les Tests](#executer-les-tests)
- [Licence](#licence)

---

## Fonctionnalites

### Code et Depots
- **Gestion des Depots** — Creez, parcourez et supprimez des depots Git avec un explorateur de code complet, un editeur de fichiers, un historique de commits, des branches et des tags
- **Importation/Migration de Depots** — Importez des depots depuis GitHub, GitLab, Bitbucket ou toute URL Git avec importation optionnelle de tickets et de PRs. Traitement en arriere-plan avec suivi de progression
- **Archivage de Depots** — Marquez les depots en lecture seule avec des badges visuels ; les pushes sont bloques pour les depots archives
- **Git Smart HTTP** — Clonez, fetchez et poussez via HTTP avec Basic Auth
- **Serveur SSH Integre** — Serveur SSH natif pour les operations Git — aucun OpenSSH externe requis. Prend en charge l'echange de cles ECDH, le chiffrement AES-CTR et l'authentification par cle publique (RSA, ECDSA, Ed25519)
- **Authentification par Cle SSH** — Ajoutez des cles publiques SSH a votre compte et authentifiez les operations Git via SSH avec gestion automatique de `authorized_keys` (ou le serveur SSH integre)
- **Forks et Synchronisation Upstream** — Forkez des depots, synchronisez les forks avec l'upstream en un clic et visualisez les relations de forks dans l'interface
- **Git LFS** — Prise en charge du Large File Storage pour le suivi des fichiers binaires
- **Miroir de Depots** — Miroir de depots vers/depuis des remotes Git externes
- **Vue de Comparaison** — Comparez les branches avec le nombre de commits en avance/en retard et le rendu complet des diffs
- **Statistiques de Langages** — Barre de repartition des langages a la GitHub sur chaque page de depot
- **Protection de Branches** — Regles configurables pour les revues requises, les verifications de statut, la prevention du force-push et l'application de l'approbation CODEOWNERS
- **Protection de Tags** — Protegez les tags contre la suppression, les mises a jour forcees et la creation non autorisee avec correspondance de motifs glob et listes d'autorisation par utilisateur
- **Verification de Signature de Commits** — Verification de signature GPG sur les commits et tags annotes avec badges "Verified" / "Signed" dans l'interface
- **Labels de Depot** — Gerez les labels avec des couleurs personnalisees par depot ; les labels sont automatiquement copies lors de la creation de depots a partir de modeles
- **Flux AGit** — Workflow push-to-review : `git push origin HEAD:refs/for/main` cree un pull request sans forker ni creer de branches distantes. Met a jour les PRs ouverts existants lors des pushes suivants
- **Explorer** — Parcourez tous les depots accessibles avec recherche, tri et filtrage par sujets
- **Recherche** — Recherche en texte integral dans les depots, tickets, PRs et code

### Collaboration
- **Tickets et Pull Requests** — Creez, commentez, fermez/rouvrez des tickets et PRs avec des labels, des assignes multiples, des dates d'echeance et des revues. Fusionnez les PRs avec des strategies de merge commit, squash ou rebase. Resolution de conflits de merge basee sur le web avec vue diff cote a cote
- **Dependances de Tickets** — Definissez des relations "bloque par" et "bloque" entre les tickets avec detection de dependances circulaires
- **Epinglage et Verrouillage de Tickets** — Epinglez les tickets importants en haut de la liste et verrouillez les conversations pour empecher d'autres commentaires
- **Edition et Suppression de Commentaires** — Editez ou supprimez vos propres commentaires sur les tickets et pull requests avec indicateur "(modifie)"
- **Resolution de Conflits de Merge** — Resolvez les conflits de merge directement dans le navigateur avec un editeur visuel montrant les vues base/notre/leur, des boutons d'acceptation rapide et la validation des marqueurs de conflit
- **Discussions** — Conversations en fil a la GitHub Discussions par depot avec categories (General, Questions & Reponses, Annonces, Idees, Montrer & Raconter, Sondages), epingler/verrouiller, marquer comme reponse et votes positifs
- **Suggestions de Revue de Code** — Le mode "Suggerer des modifications" dans les revues en ligne de PR permet aux reviseurs de proposer des remplacements de code directement dans le diff
- **Emojis de Reaction** — Reagissez aux tickets, PRs, discussions et commentaires avec pouce en haut/bas, coeur, rire, hourra, confus, fusee et yeux
- **CODEOWNERS** — Attribution automatique des reviseurs de PR basee sur les chemins de fichiers avec application optionnelle exigeant l'approbation de CODEOWNERS avant la fusion
- **Modeles de Depots** — Creez de nouveaux depots a partir de modeles avec copie automatique des fichiers, labels, modeles de tickets et regles de protection de branches
- **Tickets Brouillons et Modeles de Tickets** — Creez des tickets brouillons (travail en cours) et definissez des modeles de tickets reutilisables (rapport de bug, demande de fonctionnalite) par depot avec labels par defaut
- **Wiki** — Pages wiki basees sur Markdown par depot avec historique des revisions
- **Projets** — Tableaux Kanban avec cartes glisser-deposer pour organiser le travail
- **Snippets** — Partagez des extraits de code (comme GitHub Gists) avec coloration syntaxique et fichiers multiples
- **Organisations et Equipes** — Creez des organisations avec des membres et des equipes, attribuez des permissions d'equipe aux depots
- **Permissions Granulaires** — Modele de permissions a cinq niveaux (Lecture, Triage, Ecriture, Maintenance, Admin) pour un controle d'acces fin sur les depots
- **Jalons** — Suivez la progression des tickets vers les jalons avec barres de progression et dates d'echeance
- **Commentaires de Commits** — Commentez sur des commits individuels avec references optionnelles au fichier/ligne
- **Sujets de Depot** — Taguez les depots avec des sujets pour la decouverte et le filtrage sur la page Explorer

### CI/CD et DevOps
- **Executeur CI/CD** — Definissez des workflows dans `.github/workflows/*.yml` et executez-les dans des conteneurs Docker. Declenchement automatique sur les evenements push et pull request
- **Compatibilite GitHub Actions** — Le meme YAML de workflow fonctionne a la fois sur MyPersonalGit et GitHub Actions. Traduit les actions `uses:` (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) en commandes shell equivalentes
- **Jobs Paralleles avec `needs:`** — Les jobs declarent des dependances via `needs:` et s'executent en parallele lorsqu'ils sont independants. Les jobs dependants attendent leurs prerequis et sont automatiquement annules si une dependance echoue
- **Etapes Conditionnelles (`if:`)** — Les etapes prennent en charge les expressions `if:` : `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Les etapes de nettoyage avec `if: failure()` ou `if: always()` s'executent meme apres des echecs precedents
- **Sorties d'Etapes (`$GITHUB_OUTPUT`)** — Les etapes peuvent ecrire des paires `key=value` ou `key<<DELIMITER` multilignes dans `$GITHUB_OUTPUT` et les etapes suivantes les recoivent comme variables d'environnement, compatible avec la syntaxe `${{ steps.X.outputs.Y }}`
- **Contexte `github`** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` et `CI=true` sont automatiquement injectes dans chaque job
- **Builds Matrice** — `strategy.matrix` etend les jobs a travers plusieurs combinaisons de variables (ex., OS x version). Prend en charge `fail-fast` et la substitution `${{ matrix.X }}` dans `runs-on`, les commandes d'etapes et les noms d'etapes
- **Entrees `workflow_dispatch`** — Declencheurs manuels avec parametres d'entree types (string, boolean, choice, number). L'interface affiche un formulaire de saisie lors du declenchement de workflows avec des entrees. Les valeurs sont injectees comme variables d'environnement `INPUT_*`
- **Timeouts de Jobs (`timeout-minutes`)** — Definissez `timeout-minutes` sur les jobs pour les faire echouer automatiquement s'ils depassent la limite. Par defaut : 360 minutes (identique a GitHub Actions)
- **`if:` au Niveau du Job** — Ignorez des jobs entiers en fonction de conditions. Les jobs avec `if: always()` s'executent meme lorsque les dependances echouent. Les jobs ignores ne font pas echouer l'execution
- **Sorties de Jobs** — Les jobs declarent des `outputs:` que les jobs dependants avec `needs:` consomment via `${{ needs.X.outputs.Y }}`. Les sorties sont resolues a partir des sorties d'etapes une fois le job termine
- **`continue-on-error`** — Marquez des etapes individuelles comme autorisees a echouer sans faire echouer le job. Utile pour les etapes de validation ou de notification optionnelles
- **Filtre `on.push.paths`** — Ne declenchez les workflows que lorsque des fichiers specifiques changent. Prend en charge les motifs glob (`src/**`, `*.ts`) et `paths-ignore:` pour les exclusions
- **Re-executer les Workflows** — Re-executez les executions de workflows echouees, reussies ou annulees en un clic depuis l'interface Actions. Cree une nouvelle execution avec la meme configuration
- **`working-directory`** — Definissez `defaults.run.working-directory` au niveau du workflow ou `working-directory:` par etape pour controler ou les commandes s'executent
- **`defaults.run.shell`** — Configurez un shell personnalise par workflow ou par etape (`bash`, `sh`, `python3`, etc.)
- **`strategy.max-parallel`** — Limitez l'execution concurrente des jobs de matrice
- **`on.workflow_run`** — Chainez les workflows : declenchez le workflow B lorsque le workflow A se termine. Filtrez par nom de workflow et `types: [completed]`
- **Creation Automatique de Releases** — `softprops/action-gh-release` cree de vraies entites Release avec tag, titre, corps de changelog et indicateurs pre-release/brouillon. Les archives de code source (ZIP et TAR.GZ) sont automatiquement jointes comme actifs telechargeables
- **Pipeline d'Auto-Release** — Workflow integre qui auto-tagge les versions, genere les changelogs et publie les images Docker sur Docker Hub a chaque push sur main
- **Verifications de Statut de Commits** — Les workflows definissent automatiquement les statuts en attente/succes/echec sur les commits, visibles sur les pull requests
- **Annulation de Workflows** — Annulez les workflows en cours d'execution ou en file d'attente depuis l'interface Actions
- **Controles de Concurrence** — Les nouveaux pushes annulent automatiquement les executions en file d'attente du meme workflow
- **Variables d'Environnement de Workflow** — Definissez `env:` au niveau du workflow, du job ou de l'etape en YAML
- **Badges de Statut** — Badges SVG integrables pour le statut de workflow et de commit (`/api/badge/{repo}/workflow`)
- **Telechargement d'Artefacts** — Telechargez les artefacts de build directement depuis l'interface Actions
- **Gestion des Secrets** — Secrets de depot chiffres (AES-256) injectes comme variables d'environnement dans les executions de workflows CI/CD
- **Webhooks** — Declenchez des services externes sur les evenements du depot
- **Metriques Prometheus** — Endpoint `/metrics` integre pour la surveillance

### Hebergement de Paquets et Conteneurs
- **Registre de Conteneurs** — Hebergez des images Docker/OCI avec `docker push` et `docker pull` (OCI Distribution Spec)
- **Registre NuGet** — Hebergez des paquets .NET avec l'API NuGet v3 complete (index de services, recherche, push, restauration)
- **Registre npm** — Hebergez des paquets Node.js avec publication/installation npm standard
- **Registre PyPI** — Hebergez des paquets Python avec PEP 503 Simple API, JSON metadata API et compatibilite `twine upload`
- **Registre Maven** — Hebergez des paquets Java/JVM avec la disposition standard du depot Maven, generation de `maven-metadata.xml` et support de `mvn deploy`
- **Paquets Generiques** — Telechargez et televersez des artefacts binaires arbitraires via REST API

### Sites Statiques
- **Pages** — Servez des sites web statiques directement depuis une branche du depot (comme GitHub Pages) a `/pages/{owner}/{repo}/`

### Flux RSS/Atom
- **Flux de Depot** — Flux Atom pour les commits, releases et tags par depot (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Flux d'Activite Utilisateur** — Flux d'activite par utilisateur (`/api/feeds/users/{username}/activity.atom`)
- **Flux d'Activite Global** — Flux d'activite de tout le site (`/api/feeds/global/activity.atom`)

### Notifications
- **Notifications dans l'Application** — Mentions, commentaires et activite du depot
- **Notifications Push** — Integration Ntfy et Gotify pour des alertes en temps reel sur mobile/bureau avec activation par utilisateur

### Authentification
- **OAuth2 / SSO** — Connectez-vous avec GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord ou Twitter/X. Les administrateurs configurent le Client ID et le Secret par fournisseur dans le tableau de bord Admin — seuls les fournisseurs avec des identifiants remplis sont affiches aux utilisateurs
- **Fournisseur OAuth2** — Agissez comme fournisseur d'identite pour que d'autres applications puissent utiliser "Se connecter avec MyPersonalGit". Implemente le flux Authorization Code avec PKCE, le rafraichissement de tokens, l'endpoint userinfo et la decouverte OpenID Connect (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Authentifiez les utilisateurs contre un annuaire LDAP ou un domaine Active Directory. Les utilisateurs sont automatiquement provisiones lors de la premiere connexion avec des attributs synchronises (e-mail, nom d'affichage). Prend en charge la promotion admin basee sur les groupes, SSL/TLS et StartTLS
- **SSPI / Authentification Integree Windows** — Authentification unique transparente pour les utilisateurs de domaine Windows via Negotiate/NTLM. Les utilisateurs sur un domaine sont authentifies automatiquement sans saisir d'identifiants. Activez dans Admin > Settings (Windows uniquement)
- **Authentification a Deux Facteurs** — 2FA basee sur TOTP avec support d'application d'authentification et codes de recuperation
- **WebAuthn / Passkeys** — Support des cles de securite materielle FIDO2 et des passkeys comme second facteur. Enregistrez des YubiKeys, des authentificateurs de plateforme (Face ID, Windows Hello, Touch ID) et d'autres appareils FIDO2. Verification du compteur de signatures pour la detection de cles clonees
- **Comptes Lies** — Les utilisateurs peuvent lier plusieurs fournisseurs OAuth a leur compte depuis les Parametres

### Administration
- **Tableau de Bord Admin** — Parametres systeme (y compris fournisseur de base de donnees, serveur SSH, LDAP/AD, pages de pied de page), gestion des utilisateurs, journaux d'audit et statistiques
- **Pages de Pied de Page Personnalisables** — Conditions d'Utilisation, Politique de Confidentialite, Documentation et pages de Contact avec contenu Markdown modifiable depuis Admin > Settings
- **Profils Utilisateurs** — Carte de chaleur des contributions, flux d'activite et statistiques par utilisateur
- **Tokens d'Acces Personnel** — Authentification API basee sur les tokens avec portees configurables et restrictions optionnelles au niveau des routes (motifs glob comme `/api/packages/**` pour limiter l'acces du token a des chemins API specifiques)
- **Sauvegarde et Restauration** — Exportez et importez les donnees du serveur
- **Analyse de Securite** — Analyse reelle des vulnerabilites de dependances alimentee par la base de donnees [OSV.dev](https://osv.dev/). Extrait automatiquement les dependances de `.csproj` (NuGet), `package.json` (npm) et `requirements.txt` (PyPI), puis verifie chacune contre les CVEs connues. Rapporte la severite, les versions corrigees et les liens vers les avis. Plus des avis de securite manuels avec workflow brouillon/publier/fermer
- **Mode Sombre** — Support complet du mode sombre/clair avec un interrupteur dans l'en-tete
- **Multi-Langue / i18n** — Localisation complete sur les 27 pages avec 676 cles de ressources. Livre avec 11 langues : anglais, espagnol, francais, allemand, japonais, coreen, chinois (simplifie), portugais, russe, italien et turc. Ajoutez-en d'autres en creant des fichiers `SharedResource.{locale}.resx`

## Stack Technique

| Composant | Technologie |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (rendu interactif cote serveur) |
| Base de Donnees | SQLite (par defaut) ou PostgreSQL via Entity Framework Core 10 |
| Moteur Git | LibGit2Sharp |
| Authentification | Hachage de mots de passe BCrypt, authentification par session, tokens PAT, OAuth2 (8 fournisseurs + mode fournisseur), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| Serveur SSH | Implementation integree du protocole SSH2 (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Surveillance | Metriques Prometheus |

## Demarrage Rapide

### Prerequis

- [Docker](https://docs.docker.com/get-docker/) (recommande)
- Ou [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git pour le developpement local

### Docker (Recommande)

Telechargez depuis Docker Hub et executez :

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Le port 2222 est optionnel — necessaire uniquement si vous activez le serveur SSH integre dans Admin > Settings.

Ou utilisez Docker Compose :

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

L'application sera disponible a **http://localhost:8080**.

> **Identifiants par defaut** : `admin` / `admin`
>
> **Changez le mot de passe par defaut immediatement** via le tableau de bord Admin apres la premiere connexion.

### Executer Localement

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

L'application demarre a **http://localhost:5146**.

### Variables d'Environnement

| Variable | Description | Par Defaut |
|----------|-------------|---------|
| `Database__Provider` | Moteur de base de donnees : `sqlite` ou `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Chaine de connexion a la base de donnees | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Repertoire ou les depots Git sont stockes | `/repos` |
| `Git__RequireAuth` | Exiger l'authentification pour les operations Git HTTP | `true` |
| `Git__Users__<username>` | Definir le mot de passe pour l'utilisateur Git HTTP Basic Auth | — |
| `RESET_ADMIN_PASSWORD` | Reinitialisation d'urgence du mot de passe admin au demarrage | — |
| `Secrets__EncryptionKey` | Cle de chiffrement personnalisee pour les secrets du depot | Derivee de la chaine de connexion BD |
| `Ssh__DataDir` | Repertoire pour les donnees SSH (cles d'hote, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Chemin vers le fichier authorized_keys genere | `<DataDir>/authorized_keys` |

> **Remarque :** Le port du serveur SSH integre et les parametres LDAP sont configures via le tableau de bord Admin (Admin > Settings), pas par des variables d'environnement. Cela vous permet de les modifier sans redeployer.

## Utilisation

### 1. Se Connecter

Ouvrez l'application et cliquez sur **Sign In**. Lors d'une nouvelle installation, utilisez les identifiants par defaut (`admin` / `admin`). Creez des utilisateurs supplementaires via le tableau de bord **Admin** ou en activant l'inscription des utilisateurs dans Admin > Settings.

### 2. Creer un Depot

Cliquez sur le bouton vert **New** sur la page d'accueil, entrez un nom et cliquez sur **Create**. Cela cree un depot Git bare sur le serveur que vous pouvez cloner, pousser et gerer via l'interface web.

### 3. Cloner et Pousser

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Si l'authentification Git HTTP est activee, vous serez invite a saisir les identifiants configures via les variables d'environnement `Git__Users__<username>`. Ceux-ci sont distincts de la connexion a l'interface web.

### 4. Cloner depuis un IDE

**VS Code** : `Ctrl+Shift+P` > **Git: Clone** > collez `http://localhost:8080/git/MyRepo.git`

**Visual Studio** : **Git > Clone Repository** > collez l'URL

**JetBrains** : **File > New > Project from Version Control** > collez l'URL

### 5. Utiliser l'Editeur Web

Vous pouvez editer des fichiers directement dans le navigateur :
- Naviguez vers un depot et cliquez sur n'importe quel fichier, puis cliquez sur **Edit**
- Utilisez **Add files > Create new file** pour ajouter des fichiers sans clone local
- Utilisez **Add files > Upload files/folder** pour telecharger depuis votre machine

### 6. Registre de Conteneurs

Poussez et tirez des images Docker/OCI directement vers votre serveur :

```bash
# Se connecter (utilisez un Token d'Acces Personnel depuis Settings > Access Tokens)
docker login localhost:8080 -u youruser

# Pousser une image
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Tirer une image
docker pull localhost:8080/myapp:v1
```

> **Remarque :** Docker requiert HTTPS par defaut. Pour HTTP, ajoutez votre serveur aux `insecure-registries` de Docker dans `~/.docker/daemon.json` :
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Registre de Paquets

**NuGet (paquets .NET) :**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (paquets Node.js) :**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (paquets Python) :**
```bash
# Installer un paquet
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# Telecharger avec twine
pip install twine
cat > ~/.pypirc << 'EOF'
[distutils]
index-servers = mygit

[mygit]
repository = http://localhost:8080/api/packages/pypi/upload/
username = youruser
password = yourPAT
EOF
twine upload --repository mygit dist/*
```

**Maven (paquets Java/JVM) :**
```xml
<!-- Dans votre pom.xml, ajoutez le depot -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- Dans settings.xml, ajoutez les identifiants -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Generique (tout binaire) :**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Parcourez tous les paquets sur `/packages` dans l'interface web.

### 8. Pages (Hebergement de Sites Statiques)

Servez des sites web statiques depuis une branche du depot :

1. Allez dans l'onglet **Settings** de votre depot et activez **Pages**
2. Definissez la branche (par defaut : `gh-pages`)
3. Poussez du HTML/CSS/JS vers cette branche
4. Visitez `http://localhost:8080/pages/{username}/{repo}/`

### 9. Notifications Push

Configurez Ntfy ou Gotify dans **Admin > System Settings** pour recevoir des notifications push sur votre telephone ou bureau lorsque des tickets, PRs ou commentaires sont crees. Les utilisateurs peuvent activer/desactiver dans **Settings > Notifications**.

### 10. Authentification par Cle SSH

Utilisez des cles SSH pour des operations Git sans mot de passe. Il y a deux options :

#### Option A : Serveur SSH Integre (Recommande)

Aucun daemon SSH externe requis — MyPersonalGit execute son propre serveur SSH :

1. Allez dans **Admin > Settings** et activez **Built-in SSH Server**
2. Definissez le port SSH (par defaut : 2222) — utilisez 22 si vous n'executez pas de SSH systeme
3. Sauvegardez les parametres et redemarrez le serveur (les changements de port necessitent un redemarrage)
4. Allez dans **Settings > SSH Keys** et ajoutez votre cle publique (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` ou `~/.ssh/id_ecdsa.pub`)
5. Clonez via SSH :
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Le serveur SSH integre prend en charge l'echange de cles ECDH-SHA2-NISTP256, le chiffrement AES-128/256-CTR, HMAC-SHA2-256 et l'authentification par cle publique avec des cles Ed25519, RSA et ECDSA.

#### Option B : OpenSSH Systeme

Si vous preferez utiliser le daemon SSH de votre systeme :

1. Allez dans **Settings > SSH Keys** et ajoutez votre cle publique
2. MyPersonalGit maintient automatiquement un fichier `authorized_keys` a partir de toutes les cles SSH enregistrees
3. Configurez l'OpenSSH de votre serveur pour utiliser le fichier authorized_keys genere :
   ```
   # Dans /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Clonez via SSH :
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

Le service d'authentification SSH expose egalement une API a `/api/ssh/authorized-keys` pour une utilisation avec la directive `AuthorizedKeysCommand` d'OpenSSH.

### 11. Authentification LDAP / Active Directory

Authentifiez les utilisateurs contre l'annuaire LDAP ou le domaine Active Directory de votre organisation :

1. Allez dans **Admin > Settings** et faites defiler jusqu'a **LDAP / Active Directory Authentication**
2. Activez LDAP et remplissez les details de votre serveur :
   - **Server** : Le nom d'hote de votre serveur LDAP (ex., `dc01.corp.local`)
   - **Port** : 389 pour LDAP, 636 pour LDAPS
   - **SSL/TLS** : Activez pour LDAPS, ou utilisez StartTLS pour mettre a niveau une connexion non chiffree
3. Configurez un compte de service pour rechercher les utilisateurs :
   - **Bind DN** : `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password** : Le mot de passe du compte de service
4. Definissez les parametres de recherche :
   - **Search Base DN** : `OU=Users,DC=corp,DC=local`
   - **User Filter** : `(sAMAccountName={0})` pour AD, `(uid={0})` pour OpenLDAP
5. Mappez les attributs LDAP aux champs utilisateur :
   - **Username** : `sAMAccountName` (AD) ou `uid` (OpenLDAP)
   - **Email** : `mail`
   - **Display Name** : `displayName`
6. Optionnellement, definissez un **Admin Group DN** — les membres de ce groupe sont automatiquement promus administrateurs
7. Cliquez sur **Test LDAP Connection** pour verifier les parametres
8. Sauvegardez les parametres

Les utilisateurs peuvent maintenant se connecter avec leurs identifiants de domaine sur la page de connexion. Lors de la premiere connexion, un compte local est automatiquement cree avec des attributs synchronises depuis l'annuaire. L'authentification LDAP est egalement utilisee pour les operations Git HTTP (clone/push).

### 12. Secrets du Depot

Ajoutez des secrets chiffres aux depots pour les utiliser dans les workflows CI/CD :

1. Allez dans l'onglet **Settings** de votre depot
2. Faites defiler jusqu'a la carte **Secrets** et cliquez sur **Add secret**
3. Entrez un nom (ex., `DEPLOY_TOKEN`) et une valeur — la valeur est chiffree avec AES-256
4. Les secrets sont automatiquement injectes comme variables d'environnement dans chaque execution de workflow

Referencez les secrets dans votre workflow :
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. Connexion OAuth / SSO

Connectez-vous avec des fournisseurs d'identite externes :

1. Allez dans **Admin > OAuth / SSO** et configurez les fournisseurs que vous souhaitez activer
2. Entrez le **Client ID** et le **Client Secret** de la console developpeur du fournisseur
3. Cochez **Enable** — seuls les fournisseurs avec les deux identifiants remplis apparaitront sur la page de connexion
4. L'URL de callback pour chaque fournisseur est affichee dans le panneau d'administration (ex., `https://yourserver/oauth/callback/github`)

Fournisseurs pris en charge : GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Les utilisateurs peuvent lier plusieurs fournisseurs a leur compte dans **Settings > Linked Accounts**.

### 14. Importer un Depot

Importez des depots depuis des sources externes avec l'historique complet :

1. Cliquez sur **Import** sur la page d'accueil
2. Selectionnez un type de source (URL Git, GitHub, GitLab ou Bitbucket)
3. Entrez l'URL du depot et optionnellement un token d'authentification pour les depots prives
4. Pour les importations GitHub/GitLab/Bitbucket, importez optionnellement les tickets et pull requests
5. Suivez la progression de l'importation en temps reel sur la page d'importation

### 15. Forks et Synchronisation Upstream

Forkez un depot et gardez-le synchronise :

1. Cliquez sur le bouton **Fork** sur n'importe quelle page de depot
2. Un fork est cree sous votre nom d'utilisateur avec un lien vers l'original
3. Cliquez sur **Sync fork** a cote du badge "forked from" pour recuperer les derniers changements de l'upstream

### 16. Auto-Release CI/CD

MyPersonalGit inclut un pipeline CI/CD integre qui auto-tagge, cree des releases et publie des images Docker a chaque push sur main. Les workflows se declenchent automatiquement sur push — aucun service CI externe necessaire.

**Comment ca fonctionne :**
1. Un push sur `main` declenche automatiquement `.github/workflows/release.yml`
2. Incremente la version de correctif (`v1.15.1` -> `v1.15.2`), cree un tag git
3. Se connecte a Docker Hub, construit l'image et la publie en tant que `:latest` et `:vX.Y.Z`

**Configuration :**
1. Allez dans **Settings > Secrets** de votre depot dans MyPersonalGit
2. Ajoutez un secret nomme `DOCKERHUB_TOKEN` avec votre token d'acces Docker Hub
3. Assurez-vous que le conteneur MyPersonalGit a le socket Docker monte (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Poussez sur main — le workflow se declenche automatiquement

**Compatibilite GitHub Actions :**
Le meme YAML de workflow fonctionne egalement sur GitHub Actions — aucune modification necessaire. MyPersonalGit traduit les actions `uses:` en commandes shell equivalentes a l'execution :

| Action GitHub | Traduction MyPersonalGit |
|---|---|
| `actions/checkout@v4` | Le depot est deja clone dans `/workspace` |
| `actions/setup-dotnet@v4` | Installe le .NET SDK via le script d'installation officiel |
| `actions/setup-node@v4` | Installe Node.js via NodeSource |
| `actions/setup-python@v5` | Installe Python via apt/apk |
| `actions/setup-java@v4` | Installe OpenJDK via apt/apk |
| `docker/login-action@v3` | `docker login` avec mot de passe via stdin |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op (utilise le builder par defaut) |
| `softprops/action-gh-release@v2` | Cree une vraie entite Release dans la base de donnees |
| `${{ secrets.X }}` | Variable d'environnement `$X` |
| `${{ steps.X.outputs.Y }}` | Variable d'environnement `$Y` |
| `${{ github.sha }}` | Variable d'environnement `$GITHUB_SHA` |

**Jobs paralleles :**
Les jobs s'executent en parallele par defaut. Utilisez `needs:` pour declarer des dependances :
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet build

  test:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test

  deploy:
    needs: [build, test]
    runs-on: ubuntu-latest
    steps:
      - run: echo "deploying..."
```
Les jobs sans `needs:` demarrent immediatement. Un job est annule si l'une de ses dependances echoue.

**Etapes conditionnelles :**
Utilisez `if:` pour controler quand les etapes s'executent :
```yaml
steps:
  - name: Build
    run: dotnet build

  - name: Notify on failure
    if: failure()
    run: curl -X POST https://hooks.example.com/alert

  - name: Cleanup
    if: always()
    run: rm -rf ./tmp
```
Expressions prises en charge : `always()`, `success()` (par defaut), `failure()`, `cancelled()`, `true`, `false`.

**Sorties d'etapes :**
Les etapes peuvent passer des valeurs aux etapes suivantes via `$GITHUB_OUTPUT` :
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Builds matrice :**
Etendez les jobs a travers plusieurs combinaisons en utilisant `strategy.matrix` :
```yaml
jobs:
  test:
    strategy:
      fail-fast: true
      matrix:
        os: [ubuntu-latest, node-20]
        version: ["1.0", "2.0"]
    runs-on: ${{ matrix.os }}
    steps:
      - run: echo "Testing on ${{ matrix.os }} with version ${{ matrix.version }}"
```
Cela cree 4 jobs : `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)`, etc. Tous s'executent en parallele.

**Declencheurs manuels avec entrees (`workflow_dispatch`) :**
Definissez des entrees typees qui s'affichent comme un formulaire dans l'interface lors du declenchement manuel :
```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Target environment"
        required: true
        type: choice
        options:
          - staging
          - production
      debug:
        description: "Enable debug mode"
        type: boolean
        default: "false"

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying to $INPUT_ENVIRONMENT (debug=$INPUT_DEBUG)"
```
Les valeurs d'entree sont injectees comme variables d'environnement `INPUT_<NAME>` (en majuscules).

**Timeouts de jobs :**
Definissez `timeout-minutes` sur les jobs pour les faire echouer automatiquement s'ils s'executent trop longtemps :
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Le timeout par defaut est de 360 minutes (6 heures), identique a GitHub Actions.

**Conditionnels au niveau du job :**
Utilisez `if:` sur les jobs pour les ignorer en fonction de conditions :
```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test

  deploy:
    needs: test
    if: success()
    runs-on: ubuntu-latest
    steps:
      - run: echo "deploying..."

  notify:
    needs: test
    if: failure()
    runs-on: ubuntu-latest
    steps:
      - run: curl -X POST https://hooks.example.com/alert
```

**Sorties de jobs :**
Les jobs peuvent passer des valeurs aux jobs en aval via `outputs:` :
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.ver.outputs.version }}
    steps:
      - id: ver
        run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying version $version"
```

**Continuer en cas d'erreur :**
Laissez une etape echouer sans faire echouer le job :
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Filtrage de chemins :**
Ne declenchez les workflows que lorsque des fichiers specifiques changent :
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # ou utilisez paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Repertoire de travail :**
Definissez ou les commandes s'executent :
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # s'execute dans src/app
      - run: npm test
        working-directory: tests  # remplace la valeur par defaut
```

**Re-executer les workflows :**
Cliquez sur le bouton **Re-run** sur n'importe quelle execution de workflow terminee, echouee ou annulee pour creer une nouvelle execution avec les memes jobs, etapes et configuration.

**Workflows de pull request :**
Les workflows avec `on: pull_request` se declenchent automatiquement lorsqu'un PR non brouillon est cree, executant les verifications contre la branche source.

**Verifications de statut de commits :**
Les workflows definissent automatiquement les statuts de commits (en attente/succes/echec) pour que vous puissiez voir les resultats de build sur les PRs et appliquer les verifications requises via la protection de branches.

**Annulation de workflows :**
Cliquez sur le bouton **Cancel** sur n'importe quel workflow en cours d'execution ou en file d'attente dans l'interface Actions pour l'arreter immediatement.

**Badges de statut :**
Integrez des badges de statut de build dans votre README ou n'importe ou :
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Filtrez par nom de workflow : `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. Flux RSS/Atom

Abonnez-vous a l'activite du depot en utilisant des flux Atom standard dans n'importe quel lecteur RSS :

```
# Commits du depot
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Releases du depot
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Tags du depot
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Activite utilisateur
http://localhost:8080/api/feeds/users/admin/activity.atom

# Activite globale (tous les depots)
http://localhost:8080/api/feeds/global/activity.atom
```

Aucune authentification requise pour les depots publics. Ajoutez ces URLs a n'importe quel lecteur de flux (Feedly, Miniflux, FreshRSS, etc.) pour rester informe des changements.

## Configuration de la Base de Donnees

MyPersonalGit utilise **SQLite** par defaut — zero configuration, base de donnees a fichier unique, parfait pour un usage personnel et les petites equipes.

Pour des deploiements plus importants (nombreux utilisateurs simultanes, haute disponibilite, ou si vous executez deja PostgreSQL), vous pouvez passer a **PostgreSQL** :

### Utiliser PostgreSQL

**Docker Compose** (recommande pour PostgreSQL) :
```yaml
services:
  mypersonalgit:
    image: fennch/mypersonalgit:latest
    ports:
      - "8080:8080"
      - "2222:2222"
    environment:
      - Database__Provider=postgresql
      - ConnectionStrings__Default=Host=db;Database=mypersonalgit;Username=mypg;Password=secret
    depends_on:
      - db
    volumes:
      - repos:/repos

  db:
    image: postgres:17
    environment:
      - POSTGRES_DB=mypersonalgit
      - POSTGRES_USER=mypg
      - POSTGRES_PASSWORD=secret
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  repos:
  pgdata:
```

**Variables d'environnement uniquement** (si vous avez deja un serveur PostgreSQL) :
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

Les migrations EF Core s'executent automatiquement au demarrage pour les deux fournisseurs. Aucune configuration manuelle du schema n'est requise.

### Changer depuis le Tableau de Bord Admin

Vous pouvez egalement changer de fournisseur de base de donnees directement depuis l'interface web :

1. Allez dans **Admin > Settings** — la carte **Database** est en haut
2. Selectionnez **PostgreSQL** dans le menu deroulant des fournisseurs
3. Entrez votre chaine de connexion PostgreSQL (ex., `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Cliquez sur **Save Database Settings**
5. Redemarrez l'application pour que le changement prenne effet

La configuration est sauvegardee dans `~/.mypersonalgit/database.json` (en dehors de la base de donnees elle-meme, afin qu'elle puisse etre lue avant la connexion).

### Choisir une Base de Donnees

| | SQLite | PostgreSQL |
|---|---|---|
| **Configuration** | Zero configuration (par defaut) | Necessite un serveur PostgreSQL |
| **Ideal pour** | Usage personnel, petites equipes, NAS | Equipes de 50+, haute concurrence |
| **Sauvegarde** | Copier le fichier `.db` | `pg_dump` standard |
| **Concurrence** | Ecrivain unique (suffisant pour la plupart des usages) | Multi-ecrivain complet |
| **Migration** | N/A | Changer de fournisseur + executer l'app (auto-migration) |

## Deployer sur un NAS

MyPersonalGit fonctionne parfaitement sur un NAS (QNAP, Synology, etc.) via Docker :

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Le montage du socket Docker est optionnel — necessaire uniquement si vous souhaitez l'execution de workflows CI/CD. Le port 2222 n'est necessaire que si vous activez le serveur SSH integre.

## Configuration

Tous les parametres peuvent etre configures dans `appsettings.json`, via des variables d'environnement ou via le tableau de bord Admin a `/admin` :

- Fournisseur de base de donnees (SQLite ou PostgreSQL)
- Repertoire racine du projet
- Exigences d'authentification
- Parametres d'inscription des utilisateurs
- Interrupteurs de fonctionnalites (Issues, Wiki, Projects, Actions)
- Taille maximale du depot et nombre par utilisateur
- Parametres SMTP pour les notifications par e-mail
- Parametres de notifications push (Ntfy/Gotify)
- Serveur SSH integre (activer/desactiver, port)
- Authentification LDAP/Active Directory (serveur, bind DN, base de recherche, filtre utilisateur, mappage d'attributs, groupe d'administrateurs)
- Configuration des fournisseurs OAuth/SSO (Client ID/Secret par fournisseur)

## Structure du Projet

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Pages Blazor (Home, RepoDetails, Issues, PRs, Packages, etc.)
  Controllers/       # Endpoints REST API (NuGet, npm, Generic, Registry, etc.)
  Data/              # EF Core DbContext, implementations de services
  Models/            # Modeles de domaine
  Migrations/        # Migrations EF Core
  Services/          # Middleware (authentification, backend Git HTTP, Pages, authentification Registry)
    SshServer/       # Serveur SSH integre (protocole SSH2, ECDH, AES-CTR)
  Program.cs         # Demarrage de l'app, DI, pipeline de middleware
MyPersonalGit.Tests/
  UnitTest1.cs       # Tests xUnit avec base de donnees InMemory
```

## Executer les Tests

```bash
dotnet test
```

## Licence

MIT
