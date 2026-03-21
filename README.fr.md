🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Un serveur Git auto-hébergé avec une interface web similaire à GitHub, construit avec ASP.NET Core et Blazor Server. Parcourez vos dépôts, gérez les issues, les pull requests, les wikis, les projets et bien plus encore — le tout depuis votre propre machine ou serveur.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## Table des matières

- [Fonctionnalités](#fonctionnalités)
- [Stack technique](#stack-technique)
- [Démarrage rapide](#démarrage-rapide)
  - [Docker (recommandé)](#docker-recommandé)
  - [Exécution locale](#exécution-locale)
  - [Variables d'environnement](#variables-denvironnement)
- [Utilisation](#utilisation)
  - [Connexion](#1-connexion)
  - [Créer un dépôt](#2-créer-un-dépôt)
  - [Cloner et pousser](#3-cloner-et-pousser)
  - [Cloner depuis un IDE](#4-cloner-depuis-un-ide)
  - [Éditeur web](#5-utiliser-léditeur-web)
  - [Registre de conteneurs](#6-registre-de-conteneurs)
  - [Registre de paquets](#7-registre-de-paquets)
  - [Pages (sites statiques)](#8-pages-hébergement-de-sites-statiques)
  - [Notifications push](#9-notifications-push)
  - [Authentification par clé SSH](#10-authentification-par-clé-ssh)
  - [LDAP / Active Directory](#11-authentification-ldap--active-directory)
  - [Secrets de dépôt](#12-secrets-de-dépôt)
  - [Connexion OAuth / SSO](#13-connexion-oauth--sso)
  - [Importer un dépôt](#14-importer-un-dépôt)
  - [Forks et synchronisation upstream](#15-forks-et-synchronisation-upstream)
  - [CI/CD Auto-Release](#16-cicd-auto-release)
  - [Flux RSS/Atom](#17-flux-rssatom)
- [Configuration de la base de données](#configuration-de-la-base-de-données)
  - [Utiliser PostgreSQL](#utiliser-postgresql)
  - [Changer depuis le tableau de bord Admin](#changer-depuis-le-tableau-de-bord-admin)
  - [Choisir une base de données](#choisir-une-base-de-données)
- [Déployer sur un NAS](#déployer-sur-un-nas)
- [Configuration](#configuration)
- [Structure du projet](#structure-du-projet)
- [Exécuter les tests](#exécuter-les-tests)
- [Licence](#licence)

---

## Fonctionnalités

### Code et dépôts
- **Gestion des dépôts** — Créez, parcourez et supprimez des dépôts Git avec un navigateur de code complet, un éditeur de fichiers, un historique des commits, des branches et des tags
- **Import/Migration de dépôts** — Importez des dépôts depuis GitHub, GitLab, Bitbucket, Gitea/Forgejo/Gogs ou toute URL Git avec import optionnel des issues et PRs. Traitement en arrière-plan avec suivi de progression
- **Archivage de dépôts** — Marquez des dépôts en lecture seule avec des badges visuels ; les push sont bloqués pour les dépôts archivés
- **Git Smart HTTP** — Clonez, récupérez et poussez via HTTP avec Basic Auth
- **Serveur SSH intégré** — Serveur SSH natif pour les opérations Git — aucun OpenSSH externe requis. Supporte l'échange de clés ECDH, le chiffrement AES-CTR et l'authentification par clé publique (RSA, ECDSA, Ed25519)
- **Authentification par clé SSH** — Ajoutez des clés publiques SSH à votre compte et authentifiez les opérations Git via SSH avec gestion automatique du fichier `authorized_keys` (ou le serveur SSH intégré)
- **Forks et synchronisation upstream** — Forkez des dépôts, synchronisez les forks avec l'upstream en un clic, et visualisez les relations de fork dans l'interface
- **Git LFS** — Support du Large File Storage pour le suivi des fichiers binaires
- **Miroir de dépôts** — Miroitez des dépôts vers/depuis des remotes Git externes
- **Vue comparative** — Comparez les branches avec le nombre de commits en avance/en retard et un rendu complet des diffs
- **Statistiques de langages** — Barre de répartition des langages à la GitHub sur chaque page de dépôt
- **Protection de branches** — Règles configurables pour les reviews obligatoires, les checks de statut, la prévention du force-push et l'approbation CODEOWNERS
- **Commits signés obligatoires** — Règle de protection de branche exigeant que tous les commits soient signés GPG avant la fusion
- **Protection de tags** — Protégez les tags contre la suppression, les mises à jour forcées et la création non autorisée avec correspondance par motifs glob et listes d'autorisation par utilisateur
- **Vérification de signature de commits** — Vérification des signatures GPG sur les commits et tags annotés avec badges « Verified » / « Signed » dans l'interface
- **Labels de dépôt** — Gérez les labels avec des couleurs personnalisées par dépôt ; les labels sont automatiquement copiés lors de la création de dépôts à partir de modèles
- **AGit Flow** — Workflow push-to-review : `git push origin HEAD:refs/for/main` crée une pull request sans forker ni créer de branches distantes. Met à jour les PRs ouvertes existantes lors des push suivants
- **Explorer** — Parcourez tous les dépôts accessibles avec recherche, tri et filtrage par sujet
- **Autolink References** — Conversion automatique de `#123` en liens vers les issues, ainsi que des motifs personnalisés configurables (par ex., `JIRA-456` → URLs externes) par dépôt
- **Recherche** — Recherche plein texte dans les dépôts, issues, PRs et le code
- **License Detection** — Détecte automatiquement les fichiers LICENSE et identifie les licences courantes (MIT, Apache-2.0, GPL, BSD, ISC, MPL, Unlicense) avec un badge dans la barre latérale du dépôt

### Collaboration
- **Issues et Pull Requests** — Créez, commentez, fermez/rouvrez des issues et PRs avec labels, assignés multiples, dates d'échéance et reviews. Fusionnez les PRs avec les stratégies merge commit, squash ou rebase. Résolution des conflits de fusion via le web avec vue diff côte à côte
- **Dépendances d'issues** — Définissez des relations « bloqué par » et « bloque » entre les issues avec détection des dépendances circulaires
- **Épinglage et verrouillage d'issues** — Épinglez les issues importantes en haut de la liste et verrouillez les conversations pour empêcher les commentaires supplémentaires
- **Édition et suppression de commentaires** — Modifiez ou supprimez vos propres commentaires sur les issues et pull requests avec indicateur « (modifié) »
- **Résolution des conflits de fusion** — Résolvez les conflits de fusion directement dans le navigateur avec un éditeur visuel affichant les vues base/ours/theirs, des boutons d'acceptation rapide et la validation des marqueurs de conflit
- **Squash Commit Message** — Personnalisez le message de commit lors du squash-merge d'une pull request
- **Discussions** — Conversations en fil à la GitHub Discussions par dépôt avec catégories (Général, Q&A, Annonces, Idées, Show & Tell, Sondages), épinglage/verrouillage, marquer comme réponse et votes positifs
- **Suggestions de revue de code** — Le mode « Suggérer des modifications » dans les reviews inline de PRs permet aux reviewers de proposer des remplacements de code directement dans le diff
- **Image Diff** — Comparaison d'images côte à côte dans les pull requests avec curseur d'opacité pour le diff visuel des images modifiées (PNG, JPG, GIF, SVG, WebP)
- **File Tree dans les PRs** — Barre latérale avec arborescence de fichiers repliable dans la vue diff des pull requests pour naviguer facilement entre les fichiers modifiés
- **Marquer les fichiers comme vus** — Suivi de la progression des revues dans les pull requests avec cases « Vu » par fichier et compteur de progression
- **Coloration syntaxique des diffs** — Coloration syntaxique selon le langage dans les diffs de pull requests et de comparaison via Prism.js
- **Émoji de réaction** — Réagissez aux issues, PRs, discussions et commentaires avec pouce haut/bas, cœur, rire, hourra, perplexe, fusée et yeux
- **Auto-Merge** — Activez la fusion automatique sur les pull requests pour fusionner automatiquement lorsque toutes les vérifications de statut requises sont passées et les revues approuvées
- **Cherry-Pick / Revert via UI** — Sélectionnez n'importe quel commit vers une autre branche ou annulez un commit, directement ou en tant que nouveau pull request, depuis l'interface web
- **Transfer Issues** — Déplacez des issues entre dépôts en préservant le titre, le corps, les commentaires, les labels correspondants et en liant l'original avec une note de transfert
- **Saved Replies** — Enregistrez des réponses prédéfinies et insérez-les rapidement lors de la rédaction de commentaires sur les issues ou pull requests
- **Batch Issue Operations** — Sélectionnez plusieurs issues et fermez-les ou rouvrez-les en masse depuis la liste des issues
- **CODEOWNERS** — Attribution automatique des reviewers de PR en fonction des chemins de fichiers avec application optionnelle exigeant l'approbation CODEOWNERS avant la fusion
- **Modèles de dépôt** — Créez de nouveaux dépôts à partir de modèles avec copie automatique des fichiers, labels, modèles d'issues et règles de protection de branches
- **Brouillons d'issues et modèles d'issues** — Créez des brouillons d'issues (travail en cours) et définissez des modèles d'issues réutilisables (rapport de bug, demande de fonctionnalité) par dépôt avec labels par défaut
- **Release Editing** — Modifiez les titres, descriptions et indicateurs brouillon/pré-release des releases après leur création
- **Wiki** — Pages wiki en Markdown par dépôt avec historique des révisions
- **Projets** — Tableaux Kanban avec cartes glisser-déposer pour organiser le travail
- **Snippets** — Partagez des extraits de code (comme les Gists GitHub) avec coloration syntaxique et fichiers multiples
- **Organisations et équipes** — Créez des organisations avec des membres et des équipes, attribuez des permissions d'équipe aux dépôts
- **Permissions granulaires** — Modèle de permissions à cinq niveaux (Lecture, Triage, Écriture, Maintenance, Admin) pour un contrôle d'accès fin sur les dépôts
- **Jalons** — Suivez la progression des issues vers des jalons avec barres de progression et dates d'échéance
- **Commentaires de commits** — Commentez des commits individuels avec références optionnelles de fichier/ligne
- **Sujets de dépôt** — Taguez les dépôts avec des sujets pour la découverte et le filtrage sur la page Explorer
- **Activity Pulse** — Page de résumé hebdomadaire par dépôt affichant les PRs fusionnées, les issues ouvertes/fermées, les commits, les principaux contributeurs et les branches actives au cours des 7 derniers jours

### CI/CD et DevOps
- **Runner CI/CD** — Définissez des workflows dans `.github/workflows/*.yml` et exécutez-les dans des conteneurs Docker. Déclenchement automatique sur les événements push et pull request
- **Compatibilité GitHub Actions** — Le même YAML de workflow fonctionne sur MyPersonalGit et GitHub Actions. Traduit les actions `uses:` (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) en commandes shell équivalentes
- **Jobs parallèles avec `needs:`** — Les jobs déclarent leurs dépendances via `needs:` et s'exécutent en parallèle lorsqu'ils sont indépendants. Les jobs dépendants attendent leurs prérequis et sont automatiquement annulés si une dépendance échoue
- **Étapes conditionnelles (`if:`)** — Les étapes supportent les expressions `if:` : `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Les étapes de nettoyage avec `if: failure()` ou `if: always()` s'exécutent même après des échecs précédents
- **Sorties d'étapes (`$GITHUB_OUTPUT`)** — Les étapes peuvent écrire des paires `key=value` ou `key<<DELIMITER` multiligne dans `$GITHUB_OUTPUT` et les étapes suivantes les reçoivent comme variables d'environnement, compatible avec la syntaxe `${{ steps.X.outputs.Y }}`
- **Contexte `github`** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` et `CI=true` sont automatiquement injectés dans chaque job
- **Builds matriciels** — `strategy.matrix` développe les jobs sur plusieurs combinaisons de variables (ex. OS x version). Supporte `fail-fast` et la substitution `${{ matrix.X }}` dans `runs-on`, les commandes d'étapes et les noms d'étapes
- **Inputs `workflow_dispatch`** — Déclenchements manuels avec paramètres d'entrée typés (string, boolean, choice, number). L'interface affiche un formulaire lors du déclenchement de workflows avec des inputs. Les valeurs sont injectées comme variables d'environnement `INPUT_*`
- **Timeouts de jobs (`timeout-minutes`)** — Définissez `timeout-minutes` sur les jobs pour les faire échouer automatiquement s'ils dépassent la limite. Par défaut : 360 minutes (identique à GitHub Actions)
- **`if:` au niveau du job** — Ignorez des jobs entiers en fonction de conditions. Les jobs avec `if: always()` s'exécutent même quand les dépendances échouent. Les jobs ignorés ne font pas échouer l'exécution
- **Sorties de jobs** — Les jobs déclarent des `outputs:` que les jobs en aval consomment via `${{ needs.X.outputs.Y }}`. Les sorties sont résolues à partir des sorties d'étapes après la fin du job
- **`continue-on-error`** — Marquez des étapes individuelles comme autorisées à échouer sans faire échouer le job. Utile pour les étapes de validation ou de notification optionnelles
- **Filtre `on.push.paths`** — Ne déclenchez les workflows que lorsque des fichiers spécifiques changent. Supporte les motifs glob (`src/**`, `*.ts`) et `paths-ignore:` pour les exclusions
- **Relancer les workflows** — Relancez les exécutions de workflows échouées, réussies ou annulées en un clic depuis l'interface Actions. Crée une nouvelle exécution avec la même configuration
- **`working-directory`** — Définissez `defaults.run.working-directory` au niveau du workflow ou `working-directory:` par étape pour contrôler où les commandes s'exécutent
- **`defaults.run.shell`** — Configurez un shell personnalisé par workflow ou par étape (`bash`, `sh`, `python3`, etc.)
- **`strategy.max-parallel`** — Limitez l'exécution concurrente des jobs matriciels
- **Reusable Workflows (`workflow_call`)** — Définissez des workflows avec `on: workflow_call` que d'autres workflows peuvent invoquer avec `uses: ./.github/workflows/build.yml`. Prend en charge les entrées, sorties et secrets typés. Les jobs du workflow appelé sont intégrés dans l'appelant
- **Composite Actions** — Définissez des actions multi-étapes dans `.github/actions/{name}/action.yml` avec `runs: using: composite`. Les étapes des actions composites sont développées en ligne lors de l'exécution
- **Environment Deployments** — Configurez des environnements de déploiement (ex., `staging`, `production`) avec des règles de protection : reviewers requis, délais d'attente et restrictions de branches. Les jobs de workflow avec `environment:` nécessitent une approbation avant l'exécution. Historique complet des déploiements avec interface d'approbation/rejet
- **`on.workflow_run`** — Chaînez des workflows : déclenchez le workflow B quand le workflow A se termine. Filtrez par nom de workflow et `types: [completed]`
- **Création automatique de releases** — `softprops/action-gh-release` crée de vraies entités Release avec tag, titre, corps de changelog et drapeaux pre-release/brouillon. Les archives du code source (ZIP et TAR.GZ) sont automatiquement jointes en tant qu'assets téléchargeables
- **Pipeline Auto-Release** — Workflow intégré qui tague automatiquement les versions, génère les changelogs et pousse les images Docker vers Docker Hub à chaque push sur main
- **Checks de statut de commit** — Les workflows définissent automatiquement le statut pending/success/failure sur les commits, visible sur les pull requests
- **Annulation de workflows** — Annulez les workflows en cours d'exécution ou en file d'attente depuis l'interface Actions
- **Contrôles de concurrence** — Les nouveaux push annulent automatiquement les exécutions en file d'attente du même workflow
- **Variables d'environnement de workflow** — Définissez `env:` au niveau du workflow, du job ou de l'étape dans le YAML
- **Badges de statut** — Badges SVG intégrables pour le statut du workflow et des commits (`/api/badge/{repo}/workflow`)
- **Téléchargement d'artefacts** — Téléchargez les artefacts de build directement depuis l'interface Actions
- **Gestion des secrets** — Secrets de dépôt chiffrés (AES-256) injectés comme variables d'environnement dans les exécutions de workflows CI/CD
- **Webhooks** — Déclenchez des services externes sur les événements de dépôt
- **Métriques Prometheus** — Endpoint `/metrics` intégré pour la surveillance

### Hébergement de paquets et conteneurs (20 registries)
- **Registre de conteneurs** — Hébergez des images Docker/OCI avec `docker push` et `docker pull` (OCI Distribution Spec)
- **Registre NuGet** — Hébergez des paquets .NET avec l'API NuGet v3 complète (index de service, recherche, push, restore)
- **Registre npm** — Hébergez des paquets Node.js avec les commandes npm standard publish/install
- **Registre PyPI** — Hébergez des paquets Python avec l'API Simple PEP 503, l'API de métadonnées JSON et la compatibilité `twine upload`
- **Registre Maven** — Hébergez des paquets Java/JVM avec la disposition standard de dépôt Maven, génération de `maven-metadata.xml` et support de `mvn deploy`
- **Alpine Registry** — Hébergez des paquets Alpine Linux `.apk` avec génération d'APKINDEX
- **RPM Registry** — Hébergez des paquets RPM avec métadonnées `repomd.xml` pour `dnf`/`yum`
- **Chef Registry** — Hébergez des cookbooks Chef avec une API compatible Chef Supermarket
- **Paquets génériques** — Téléchargez et téléversez des artefacts binaires arbitraires via l'API REST

### Sites statiques
- **Pages** — Servez des sites web statiques directement depuis une branche de dépôt (comme GitHub Pages) à `/pages/{owner}/{repo}/`

### Flux RSS/Atom
- **Flux de dépôt** — Flux Atom pour les commits, releases et tags par dépôt (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Flux d'activité utilisateur** — Flux d'activité par utilisateur (`/api/feeds/users/{username}/activity.atom`)
- **Flux d'activité global** — Flux d'activité à l'échelle du site (`/api/feeds/global/activity.atom`)

### Notifications
- **Notifications dans l'application** — Mentions, commentaires et activité des dépôts
- **Notifications push** — Intégration Ntfy et Gotify pour des alertes en temps réel sur mobile/bureau avec opt-in par utilisateur

### Authentification
- **OAuth2 / SSO** — Connectez-vous avec GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord ou Twitter/X. Les administrateurs configurent le Client ID et le Secret par fournisseur dans le tableau de bord Admin — seuls les fournisseurs avec les identifiants renseignés sont affichés aux utilisateurs
- **Fournisseur OAuth2** — Agissez en tant que fournisseur d'identité pour que d'autres applications puissent utiliser « Se connecter avec MyPersonalGit ». Implémente le flux Authorization Code avec PKCE, le rafraîchissement de token, l'endpoint userinfo et la découverte OpenID Connect (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Authentifiez les utilisateurs auprès d'un annuaire LDAP ou d'un domaine Active Directory. Les utilisateurs sont auto-provisionnés lors de leur première connexion avec des attributs synchronisés (email, nom d'affichage). Supporte la promotion admin basée sur les groupes, SSL/TLS et StartTLS
- **SSPI / Authentification intégrée Windows** — Single Sign-On transparent pour les utilisateurs du domaine Windows via Negotiate/NTLM. Les utilisateurs d'un domaine sont authentifiés automatiquement sans saisir d'identifiants. Activez dans Admin > Settings (Windows uniquement)
- **Authentification à deux facteurs** — 2FA basée sur TOTP avec support des applications d'authentification et codes de récupération
- **WebAuthn / Passkeys** — Support des clés de sécurité matérielle FIDO2 et des passkeys comme second facteur. Enregistrez des YubiKeys, des authentificateurs de plateforme (Face ID, Windows Hello, Touch ID) et d'autres appareils FIDO2. Vérification du compteur de signatures pour la détection de clés clonées
- **Comptes liés** — Les utilisateurs peuvent lier plusieurs fournisseurs OAuth à leur compte depuis les paramètres

### Administration
- **Tableau de bord Admin** — Paramètres système (incluant le fournisseur de base de données, le serveur SSH, LDAP/AD, les pages de pied de page), gestion des utilisateurs, journaux d'audit et statistiques
- **Pages de pied de page personnalisables** — Pages Conditions d'utilisation, Politique de confidentialité, Documentation et Contact avec contenu Markdown éditable depuis Admin > Settings
- **Profils utilisateurs** — Carte thermique des contributions, flux d'activité et statistiques par utilisateur
- **Personal Access Tokens** — Authentification API basée sur des tokens avec scopes configurables et restrictions optionnelles au niveau des routes (motifs glob comme `/api/packages/**` pour limiter l'accès du token à des chemins API spécifiques)
- **Sauvegarde et restauration** — Exportez et importez les données du serveur
- **Analyse de sécurité** — Analyse réelle des vulnérabilités des dépendances alimentée par la base de données [OSV.dev](https://osv.dev/). Extrait automatiquement les dépendances depuis `.csproj` (NuGet), `package.json` (npm), `requirements.txt` (PyPI), `Cargo.toml` (Rust), `Gemfile` (Ruby), `composer.json` (PHP), `go.mod` (Go), `pom.xml` (Maven/Java) et `pubspec.yaml` (Dart/Flutter), puis vérifie chacune par rapport aux CVEs connues. Rapporte la sévérité, les versions corrigées et les liens vers les avis de sécurité. Plus des avis de sécurité manuels avec workflow brouillon/publication/clôture
- **Secret Scanning** — Analyse automatiquement chaque push à la recherche de fuites d'identifiants (clés AWS, tokens GitHub/GitLab, tokens Slack, clés privées, clés API, JWTs, chaînes de connexion et plus). 20 modèles intégrés avec support regex complet. Analyse complète du dépôt à la demande. Alertes avec workflow résoudre/faux positif. Modèles personnalisés configurables via API
- **Dependabot-Style Auto-Update PRs** — Vérifie automatiquement les dépendances obsolètes et crée des pull requests pour les mettre à jour. Prend en charge les écosystèmes NuGet, npm et PyPI. Planification configurable (quotidienne/hebdomadaire/mensuelle) et limite de PRs ouvertes par dépôt
- **Repository Insights (Traffic)** — Suivez les compteurs de clone/fetch, les vues de pages, les visiteurs uniques, les principaux référents et les chemins de contenu populaires. Graphiques de trafic dans l'onglet Insights avec résumés sur 14 jours. Agrégation quotidienne avec rétention de 90 jours. Les adresses IP sont hachées pour la confidentialité
- **Mode sombre** — Support complet du mode sombre/clair avec un bouton de basculement dans l'en-tête
- **Multi-langue / i18n** — Localisation complète sur les 30 pages avec 930 clés de ressources. Livré avec 11 langues : anglais, espagnol, français, allemand, japonais, coréen, chinois (simplifié), portugais, russe, italien et turc. Sélecteur de langue dans l'en-tête. Ajoutez-en d'autres en créant des fichiers `SharedResource.{locale}.resx`
- **Swagger / OpenAPI** — Documentation interactive de l'API à `/swagger` avec tous les endpoints REST découvrables et testables
- **Open Graph Meta Tags** — Les pages de dépôts, d'issues et de PRs incluent og:title et og:description pour des aperçus de liens enrichis dans Slack, Discord et les réseaux sociaux
- **Mermaid Diagrams** — Rendu de diagrammes Mermaid dans les fichiers Markdown (organigrammes, diagrammes de séquence, diagrammes de Gantt, etc.)
- **Math Rendering** — Expressions mathématiques LaTeX/KaTeX dans le Markdown (syntaxe `$inline$` et `$$display$$`)
- **CSV/TSV Viewer** — Les fichiers CSV et TSV sont affichés sous forme de tableaux formatés et triables au lieu de texte brut
- **Keyboard Shortcuts** — Appuyez sur `?` pour afficher une fenêtre d'aide des raccourcis. `/` met le focus sur la recherche, `g i` va aux Issues, `g p` aux Pull Requests, `g h` à l'Accueil, `g n` aux Notifications
- **Health Check Endpoint** — `/health` retourne du JSON avec l'état de connectivité de la base de données pour la surveillance Docker/Kubernetes
- **Sitemap.xml** — Sitemap XML dynamique à `/sitemap.xml` listant tous les dépôts publics pour l'indexation par les moteurs de recherche
- **Line Linking** — Cliquez sur les numéros de ligne dans le visualiseur de fichiers pour générer des URLs partageables `#L42` avec mise en surbrillance de la ligne au chargement
- **File Download** — Téléchargez des fichiers individuels depuis le visualiseur de fichiers avec les en-têtes Content-Disposition appropriés
- **Jupyter Notebook Rendering** — Les fichiers `.ipynb` sont affichés comme des notebooks formatés avec cellules de code, Markdown, sorties et images en ligne
- **Repository Transfer** — Transférez la propriété du dépôt à un autre utilisateur ou organisation depuis les Paramètres du dépôt
- **Default Branch Configuration** — Changez la branche par défaut par dépôt depuis l'onglet Paramètres
- **Rename Repository** — Renommez un dépôt depuis Settings avec mise à jour automatique de toutes les références (issues, PRs, étoiles, webhooks, secrets, etc.)
- **User-Level Secrets** — Secrets chiffrés partagés entre tous les dépôts d'un utilisateur, gérés depuis Settings > Secrets
- **Organization-Level Secrets** — Secrets chiffrés partagés entre tous les dépôts d'une organisation, gérés depuis l'onglet Secrets de l'organisation
- **Repository Pinning** — Épinglez jusqu'à 6 dépôts favoris sur votre page de profil utilisateur pour un accès rapide
- **Git Hooks Management** — Interface web pour visualiser, modifier et gérer les Git hooks côté serveur (pre-receive, update, post-receive, post-update, pre-push) par dépôt
- **Protected File Patterns** — Règle de protection de branche avec des patrons glob pour exiger une approbation de révision pour les modifications de fichiers spécifiques (par exemple, `*.lock`, `migrations/**`, `.github/workflows/*`)
- **External Issue Tracker** — Configurez les dépôts pour renvoyer vers un suivi d'issues externe (Jira, Linear, etc.) avec des motifs d'URL personnalisés
- **Federation (NodeInfo/WebFinger)** — Découverte NodeInfo 2.0, WebFinger et host-meta pour la découvrabilité inter-instances
- **Distributed CI Runners** — Les runners externes peuvent s'enregistrer via API, interroger les jobs en file d'attente et rapporter les résultats

## Stack technique

| Composant | Technologie |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (rendu interactif côté serveur) |
| Base de données | SQLite (par défaut) ou PostgreSQL via Entity Framework Core 10 |
| Moteur Git | LibGit2Sharp |
| Authentification | Hachage de mots de passe BCrypt, authentification par session, tokens PAT, OAuth2 (8 fournisseurs + mode fournisseur), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| Serveur SSH | Implémentation intégrée du protocole SSH2 (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Surveillance | Métriques Prometheus |

## Démarrage rapide

### Prérequis

- [Docker](https://docs.docker.com/get-docker/) (recommandé)
- Ou [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git pour le développement local

### Docker (recommandé)

Téléchargez depuis Docker Hub et lancez :

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Le port 2222 est optionnel — nécessaire uniquement si vous activez le serveur SSH intégré dans Admin > Settings.

Ou utilisez Docker Compose :

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

L'application sera disponible à **http://localhost:8080**.

> **Identifiants par défaut** : `admin` / `admin`
>
> **Changez le mot de passe par défaut immédiatement** via le tableau de bord Admin après la première connexion.

### Exécution locale

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

L'application démarre à **http://localhost:5146**.

### Variables d'environnement

| Variable | Description | Défaut |
|----------|-------------|--------|
| `Database__Provider` | Moteur de base de données : `sqlite` ou `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Chaîne de connexion à la base de données | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Répertoire de stockage des dépôts Git | `/repos` |
| `Git__RequireAuth` | Exiger l'authentification pour les opérations Git HTTP | `true` |
| `Git__Users__<username>` | Définir le mot de passe pour l'utilisateur Git HTTP Basic Auth | — |
| `RESET_ADMIN_PASSWORD` | Réinitialisation d'urgence du mot de passe admin au démarrage | — |
| `Secrets__EncryptionKey` | Clé de chiffrement personnalisée pour les secrets de dépôt | Dérivée de la chaîne de connexion à la BD |
| `Ssh__DataDir` | Répertoire pour les données SSH (clés d'hôte, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Chemin vers le fichier authorized_keys généré | `<DataDir>/authorized_keys` |

> **Note :** Le port du serveur SSH intégré et les paramètres LDAP sont configurés via le tableau de bord Admin (Admin > Settings), et non par des variables d'environnement. Cela vous permet de les modifier sans redéployer.

## Utilisation

### 1. Connexion

Ouvrez l'application et cliquez sur **Sign In**. Lors d'une installation neuve, utilisez les identifiants par défaut (`admin` / `admin`). Créez des utilisateurs supplémentaires via le tableau de bord **Admin** ou en activant l'inscription des utilisateurs dans Admin > Settings.

### 2. Créer un dépôt

Cliquez sur le bouton vert **New** sur la page d'accueil, entrez un nom et cliquez sur **Create**. Cela crée un dépôt Git bare sur le serveur que vous pouvez cloner, vers lequel pousser et gérer via l'interface web.

### 3. Cloner et pousser

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Si l'authentification Git HTTP est activée, vous serez invité à saisir les identifiants configurés via les variables d'environnement `Git__Users__<username>`. Ceux-ci sont séparés de la connexion à l'interface web.

### 4. Cloner depuis un IDE

**VS Code** : `Ctrl+Shift+P` > **Git: Clone** > collez `http://localhost:8080/git/MyRepo.git`

**Visual Studio** : **Git > Clone Repository** > collez l'URL

**JetBrains** : **File > New > Project from Version Control** > collez l'URL

### 5. Utiliser l'éditeur web

Vous pouvez modifier des fichiers directement dans le navigateur :
- Naviguez vers un dépôt et cliquez sur un fichier, puis cliquez sur **Edit**
- Utilisez **Add files > Create new file** pour ajouter des fichiers sans clone local
- Utilisez **Add files > Upload files/folder** pour télécharger depuis votre machine

### 6. Registre de conteneurs

Poussez et tirez des images Docker/OCI directement vers votre serveur :

```bash
# Connectez-vous (utilisez un Personal Access Token depuis Settings > Access Tokens)
docker login localhost:8080 -u youruser

# Pousser une image
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Tirer une image
docker pull localhost:8080/myapp:v1
```

> **Note :** Docker requiert HTTPS par défaut. Pour HTTP, ajoutez votre serveur à la liste `insecure-registries` de Docker dans `~/.docker/daemon.json` :
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Registre de paquets

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

# Téléverser avec twine
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
<!-- Dans votre pom.xml, ajoutez le dépôt -->
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

**Générique (tout binaire) :**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Parcourez tous les paquets à `/packages` dans l'interface web.

### 8. Pages (hébergement de sites statiques)

Servez des sites web statiques depuis une branche de dépôt :

1. Allez dans l'onglet **Settings** de votre dépôt et activez **Pages**
2. Définissez la branche (par défaut : `gh-pages`)
3. Poussez du HTML/CSS/JS vers cette branche
4. Visitez `http://localhost:8080/pages/{username}/{repo}/`

### 9. Notifications push

Configurez Ntfy ou Gotify dans **Admin > System Settings** pour recevoir des notifications push sur votre téléphone ou bureau lorsque des issues, PRs ou commentaires sont créés. Les utilisateurs peuvent activer/désactiver dans **Settings > Notifications**.

### 10. Authentification par clé SSH

Utilisez des clés SSH pour des opérations Git sans mot de passe. Il existe deux options :

#### Option A : Serveur SSH intégré (recommandé)

Aucun daemon SSH externe requis — MyPersonalGit exécute son propre serveur SSH :

1. Allez dans **Admin > Settings** et activez **Built-in SSH Server**
2. Définissez le port SSH (par défaut : 2222) — utilisez 22 si vous n'exécutez pas de SSH système
3. Enregistrez les paramètres et redémarrez le serveur (les changements de port nécessitent un redémarrage)
4. Allez dans **Settings > SSH Keys** et ajoutez votre clé publique (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` ou `~/.ssh/id_ecdsa.pub`)
5. Clonez via SSH :
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Le serveur SSH intégré supporte l'échange de clés ECDH-SHA2-NISTP256, le chiffrement AES-128/256-CTR, HMAC-SHA2-256 et l'authentification par clé publique avec les clés Ed25519, RSA et ECDSA.

#### Option B : OpenSSH système

Si vous préférez utiliser le daemon SSH de votre système :

1. Allez dans **Settings > SSH Keys** et ajoutez votre clé publique
2. MyPersonalGit maintient automatiquement un fichier `authorized_keys` à partir de toutes les clés SSH enregistrées
3. Configurez le OpenSSH de votre serveur pour utiliser le fichier authorized_keys généré :
   ```
   # Dans /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Clonez via SSH :
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

Le service d'authentification SSH expose également une API à `/api/ssh/authorized-keys` pour utilisation avec la directive `AuthorizedKeysCommand` d'OpenSSH.

### 11. Authentification LDAP / Active Directory

Authentifiez les utilisateurs auprès de l'annuaire LDAP ou du domaine Active Directory de votre organisation :

1. Allez dans **Admin > Settings** et faites défiler jusqu'à **LDAP / Active Directory Authentication**
2. Activez LDAP et renseignez les détails de votre serveur :
   - **Server** : Le nom d'hôte de votre serveur LDAP (ex. `dc01.corp.local`)
   - **Port** : 389 pour LDAP, 636 pour LDAPS
   - **SSL/TLS** : Activez pour LDAPS, ou utilisez StartTLS pour mettre à niveau une connexion en clair
3. Configurez un compte de service pour la recherche d'utilisateurs :
   - **Bind DN** : `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password** : Le mot de passe du compte de service
4. Définissez les paramètres de recherche :
   - **Search Base DN** : `OU=Users,DC=corp,DC=local`
   - **User Filter** : `(sAMAccountName={0})` pour AD, `(uid={0})` pour OpenLDAP
5. Mappez les attributs LDAP aux champs utilisateur :
   - **Username** : `sAMAccountName` (AD) ou `uid` (OpenLDAP)
   - **Email** : `mail`
   - **Display Name** : `displayName`
6. Définissez optionnellement un **Admin Group DN** — les membres de ce groupe sont automatiquement promus administrateurs
7. Cliquez sur **Test LDAP Connection** pour vérifier les paramètres
8. Enregistrez les paramètres

Les utilisateurs peuvent maintenant se connecter avec leurs identifiants de domaine sur la page de connexion. Lors de la première connexion, un compte local est automatiquement créé avec les attributs synchronisés depuis l'annuaire. L'authentification LDAP est également utilisée pour les opérations Git HTTP (clone/push).

### 12. Secrets de dépôt

Ajoutez des secrets chiffrés aux dépôts pour les utiliser dans les workflows CI/CD :

1. Allez dans l'onglet **Settings** de votre dépôt
2. Faites défiler jusqu'à la carte **Secrets** et cliquez sur **Add secret**
3. Entrez un nom (ex. `DEPLOY_TOKEN`) et une valeur — la valeur est chiffrée avec AES-256
4. Les secrets sont automatiquement injectés comme variables d'environnement dans chaque exécution de workflow

Référencez les secrets dans votre workflow :
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. Connexion OAuth / SSO

Connectez-vous avec des fournisseurs d'identité externes :

1. Allez dans **Admin > OAuth / SSO** et configurez les fournisseurs que vous souhaitez activer
2. Entrez le **Client ID** et le **Client Secret** depuis la console développeur du fournisseur
3. Cochez **Enable** — seuls les fournisseurs avec les deux identifiants renseignés apparaîtront sur la page de connexion
4. L'URL de callback pour chaque fournisseur est affichée dans le panneau admin (ex. `https://yourserver/oauth/callback/github`)

Fournisseurs supportés : GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Les utilisateurs peuvent lier plusieurs fournisseurs à leur compte dans **Settings > Linked Accounts**.

### 14. Importer un dépôt

Importez des dépôts depuis des sources externes avec l'historique complet :

1. Cliquez sur **Import** sur la page d'accueil
2. Sélectionnez un type de source (URL Git, GitHub, GitLab ou Bitbucket)
3. Entrez l'URL du dépôt et optionnellement un token d'authentification pour les dépôts privés
4. Pour les imports GitHub/GitLab/Bitbucket, importez optionnellement les issues et pull requests
5. Suivez la progression de l'import en temps réel sur la page Import

### 15. Forks et synchronisation upstream

Forkez un dépôt et gardez-le synchronisé :

1. Cliquez sur le bouton **Fork** sur n'importe quelle page de dépôt
2. Un fork est créé sous votre nom d'utilisateur avec un lien vers l'original
3. Cliquez sur **Sync fork** à côté du badge « forked from » pour tirer les derniers changements depuis l'upstream

### 16. CI/CD Auto-Release

MyPersonalGit inclut un pipeline CI/CD intégré qui tague, publie et pousse automatiquement des images Docker à chaque push sur main. Les workflows se déclenchent automatiquement sur push — aucun service CI externe nécessaire.

**Comment ça fonctionne :**
1. Un push sur `main` déclenche automatiquement `.github/workflows/release.yml`
2. Incrémente la version patch (`v1.15.1` -> `v1.15.2`), crée un tag git
3. Se connecte à Docker Hub, construit l'image et la pousse en tant que `:latest` et `:vX.Y.Z`

**Configuration :**
1. Allez dans **Settings > Secrets** de votre dépôt dans MyPersonalGit
2. Ajoutez un secret nommé `DOCKERHUB_TOKEN` avec votre token d'accès Docker Hub
3. Assurez-vous que le conteneur MyPersonalGit a le socket Docker monté (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Poussez sur main — le workflow se déclenche automatiquement

**Compatibilité GitHub Actions :**
Le même YAML de workflow fonctionne également sur GitHub Actions — aucune modification nécessaire. MyPersonalGit traduit les actions `uses:` en commandes shell équivalentes à l'exécution :

| GitHub Action | Traduction MyPersonalGit |
|---|---|
| `actions/checkout@v4` | Le dépôt est déjà cloné dans `/workspace` |
| `actions/setup-dotnet@v4` | Installe le SDK .NET via le script d'installation officiel |
| `actions/setup-node@v4` | Installe Node.js via NodeSource |
| `actions/setup-python@v5` | Installe Python via apt/apk |
| `actions/setup-java@v4` | Installe OpenJDK via apt/apk |
| `docker/login-action@v3` | `docker login` avec mot de passe via stdin |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op (utilise le builder par défaut) |
| `softprops/action-gh-release@v2` | Crée une vraie entité Release dans la base de données |
| `${{ secrets.X }}` | Variable d'environnement `$X` |
| `${{ steps.X.outputs.Y }}` | Variable d'environnement `$Y` |
| `${{ github.sha }}` | Variable d'environnement `$GITHUB_SHA` |

**Jobs parallèles :**
Les jobs s'exécutent en parallèle par défaut. Utilisez `needs:` pour déclarer les dépendances :
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
Les jobs sans `needs:` démarrent immédiatement. Un job est annulé si l'une de ses dépendances échoue.

**Étapes conditionnelles :**
Utilisez `if:` pour contrôler quand les étapes s'exécutent :
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
Expressions supportées : `always()`, `success()` (par défaut), `failure()`, `cancelled()`, `true`, `false`.

**Sorties d'étapes :**
Les étapes peuvent transmettre des valeurs aux étapes suivantes via `$GITHUB_OUTPUT` :
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Builds matriciels :**
Déployez des jobs sur plusieurs combinaisons avec `strategy.matrix` :
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
Cela crée 4 jobs : `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)`, etc. Tous s'exécutent en parallèle.

**Déclenchements manuels avec inputs (`workflow_dispatch`) :**
Définissez des inputs typés qui s'affichent comme formulaire dans l'interface lors du déclenchement manuel :
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
Les valeurs d'input sont injectées comme variables d'environnement `INPUT_<NAME>` (en majuscules).

**Timeouts de jobs :**
Définissez `timeout-minutes` sur les jobs pour les faire échouer automatiquement s'ils durent trop longtemps :
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Le timeout par défaut est de 360 minutes (6 heures), identique à GitHub Actions.

**Conditions au niveau du job :**
Utilisez `if:` sur les jobs pour les ignorer selon des conditions :
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
Les jobs peuvent transmettre des valeurs aux jobs en aval via `outputs:` :
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
Laissez une étape échouer sans faire échouer le job :
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Filtrage par chemin :**
Ne déclenchez les workflows que lorsque des fichiers spécifiques changent :
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

**Répertoire de travail :**
Définissez où les commandes s'exécutent :
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # s'exécute dans src/app
      - run: npm test
        working-directory: tests  # remplace la valeur par défaut
```

**Relancer les workflows :**
Cliquez sur le bouton **Re-run** sur n'importe quelle exécution de workflow terminée, échouée ou annulée pour créer une nouvelle exécution avec les mêmes jobs, étapes et configuration.

**Workflows de pull request :**
Les workflows avec `on: pull_request` se déclenchent automatiquement lorsqu'une PR non-brouillon est créée, exécutant les checks sur la branche source.

**Checks de statut de commit :**
Les workflows définissent automatiquement les statuts de commit (pending/success/failure) pour que vous puissiez voir les résultats de build sur les PRs et appliquer des checks obligatoires via la protection de branches.

**Annulation de workflows :**
Cliquez sur le bouton **Cancel** sur n'importe quel workflow en cours d'exécution ou en file d'attente dans l'interface Actions pour l'arrêter immédiatement.

**Badges de statut :**
Intégrez des badges de statut de build dans votre README ou ailleurs :
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Filtrez par nom de workflow : `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. Flux RSS/Atom

Abonnez-vous à l'activité des dépôts en utilisant des flux Atom standard dans n'importe quel lecteur RSS :

```
# Commits du dépôt
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Releases du dépôt
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Tags du dépôt
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Activité utilisateur
http://localhost:8080/api/feeds/users/admin/activity.atom

# Activité globale (tous les dépôts)
http://localhost:8080/api/feeds/global/activity.atom
```

Aucune authentification requise pour les dépôts publics. Ajoutez ces URLs à n'importe quel lecteur de flux (Feedly, Miniflux, FreshRSS, etc.) pour rester informé des changements.

## Configuration de la base de données

MyPersonalGit utilise **SQLite** par défaut — zéro configuration, base de données en fichier unique, parfait pour un usage personnel et les petites équipes.

Pour des déploiements plus importants (nombreux utilisateurs simultanés, haute disponibilité, ou si vous utilisez déjà PostgreSQL), vous pouvez passer à **PostgreSQL** :

### Utiliser PostgreSQL

**Docker Compose** (recommandé pour PostgreSQL) :
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

**Variables d'environnement uniquement** (si vous avez déjà un serveur PostgreSQL) :
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

Les migrations EF Core s'exécutent automatiquement au démarrage pour les deux fournisseurs. Aucune configuration manuelle du schéma requise.

### Changer depuis le tableau de bord Admin

Vous pouvez également changer de fournisseur de base de données directement depuis l'interface web :

1. Allez dans **Admin > Settings** — la carte **Database** est en haut
2. Sélectionnez **PostgreSQL** dans le menu déroulant du fournisseur
3. Entrez votre chaîne de connexion PostgreSQL (ex. `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Cliquez sur **Save Database Settings**
5. Redémarrez l'application pour que le changement prenne effet

La configuration est enregistrée dans `~/.mypersonalgit/database.json` (en dehors de la base de données elle-même, pour pouvoir être lue avant la connexion).

### Choisir une base de données

| | SQLite | PostgreSQL |
|---|---|---|
| **Installation** | Zéro configuration (par défaut) | Nécessite un serveur PostgreSQL |
| **Idéal pour** | Usage personnel, petites équipes, NAS | Équipes de 50+, haute concurrence |
| **Sauvegarde** | Copier le fichier `.db` | `pg_dump` standard |
| **Concurrence** | Écrivain unique (suffisant pour la plupart des usages) | Multi-écrivain complet |
| **Migration** | N/A | Changer de fournisseur + lancer l'application (migration automatique) |

## Déployer sur un NAS

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

Le montage du socket Docker est optionnel — nécessaire uniquement si vous souhaitez l'exécution de workflows CI/CD. Le port 2222 n'est nécessaire que si vous activez le serveur SSH intégré.

## Configuration

Tous les paramètres peuvent être configurés dans `appsettings.json`, via des variables d'environnement, ou via le tableau de bord Admin à `/admin` :

- Fournisseur de base de données (SQLite ou PostgreSQL)
- Répertoire racine du projet
- Exigences d'authentification
- Paramètres d'inscription des utilisateurs
- Bascules de fonctionnalités (Issues, Wiki, Projects, Actions)
- Taille maximale de dépôt et nombre par utilisateur
- Paramètres SMTP pour les notifications par email
- Paramètres de notifications push (Ntfy/Gotify)
- Serveur SSH intégré (activer/désactiver, port)
- Authentification LDAP/Active Directory (serveur, Bind DN, base de recherche, filtre utilisateur, mappage d'attributs, groupe admin)
- Configuration des fournisseurs OAuth/SSO (Client ID/Secret par fournisseur)

## Structure du projet

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Pages Blazor (Home, RepoDetails, Issues, PRs, Packages, etc.)
  Controllers/       # Endpoints API REST (NuGet, npm, Generic, Registry, etc.)
  Data/              # DbContext EF Core, implémentations de services
  Models/            # Modèles de domaine
  Migrations/        # Migrations EF Core
  Services/          # Middleware (auth, backend Git HTTP, Pages, auth Registry)
    SshServer/       # Serveur SSH intégré (protocole SSH2, ECDH, AES-CTR)
  Program.cs         # Démarrage de l'application, DI, pipeline de middleware
MyPersonalGit.Tests/
  UnitTest1.cs       # Tests xUnit avec base de données InMemory
```

## Exécuter les tests

```bash
dotnet test
```

## Licence

MIT
