🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Ein selbst gehosteter Git-Server mit einer GitHub-aehnlichen Weboberflaeche, erstellt mit ASP.NET Core und Blazor Server. Durchsuchen Sie Repositories, verwalten Sie Issues, Pull Requests, Wikis, Projekte und mehr -- alles von Ihrem eigenen Rechner oder Server.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)
---

## Inhaltsverzeichnis

- [Funktionen](#funktionen)
- [Technologie-Stack](#technologie-stack)
- [Schnellstart](#schnellstart)
  - [Docker (Empfohlen)](#docker-empfohlen)
  - [Lokal ausfuehren](#lokal-ausfuehren)
  - [Umgebungsvariablen](#umgebungsvariablen)
- [Nutzung](#nutzung)
  - [Anmelden](#1-anmelden)
  - [Ein Repository erstellen](#2-ein-repository-erstellen)
  - [Klonen und Pushen](#3-klonen-und-pushen)
  - [Aus einer IDE klonen](#4-aus-einer-ide-klonen)
  - [Web-Editor](#5-den-web-editor-verwenden)
  - [Container Registry](#6-container-registry)
  - [Paket-Registry](#7-paket-registry)
  - [Pages (Statische Webseiten)](#8-pages-statisches-webseiten-hosting)
  - [Push-Benachrichtigungen](#9-push-benachrichtigungen)
  - [SSH-Schluessel-Authentifizierung](#10-ssh-schluessel-authentifizierung)
  - [LDAP / Active Directory](#11-ldap--active-directory-authentifizierung)
  - [Repository-Geheimnisse](#12-repository-geheimnisse)
  - [OAuth / SSO-Anmeldung](#13-oauth--sso-anmeldung)
  - [Repository importieren](#14-repository-importieren)
  - [Forking & Upstream-Synchronisation](#15-forking--upstream-synchronisation)
  - [CI/CD Auto-Release](#16-cicd-auto-release)
  - [RSS/Atom-Feeds](#17-rssatom-feeds)
- [Datenbank-Konfiguration](#datenbank-konfiguration)
  - [PostgreSQL verwenden](#postgresql-verwenden)
  - [Umschalten ueber das Admin-Dashboard](#umschalten-ueber-das-admin-dashboard)
  - [Eine Datenbank waehlen](#eine-datenbank-waehlen)
- [Auf einem NAS bereitstellen](#auf-einem-nas-bereitstellen)
- [Konfiguration](#konfiguration)
- [Projektstruktur](#projektstruktur)
- [Tests ausfuehren](#tests-ausfuehren)
- [Lizenz](#lizenz)

---

## Funktionen

### Code & Repositories
- **Repository-Verwaltung** -- Erstellen, durchsuchen und loeschen Sie Git-Repositories mit einem vollstaendigen Code-Browser, Datei-Editor, Commit-Verlauf, Branches und Tags
- **Repository-Import/Migration** -- Importieren Sie Repositories von GitHub, GitLab, Bitbucket oder einer beliebigen Git-URL mit optionalem Import von Issues und PRs. Hintergrundverarbeitung mit Fortschrittsverfolgung
- **Repository-Archivierung** -- Markieren Sie Repositories als schreibgeschuetzt mit visuellen Badges; Pushes werden fuer archivierte Repos blockiert
- **Git Smart HTTP** -- Klonen, Fetchen und Pushen ueber HTTP mit Basic Auth
- **Integrierter SSH-Server** -- Nativer SSH-Server fuer Git-Operationen -- kein externes OpenSSH erforderlich. Unterstuetzt ECDH-Schluesselaustausch, AES-CTR-Verschluesselung und Public-Key-Authentifizierung (RSA, ECDSA, Ed25519)
- **SSH-Schluessel-Authentifizierung** -- Fuegen Sie SSH-Public-Keys zu Ihrem Konto hinzu und authentifizieren Sie Git-Operationen ueber SSH mit automatisch verwalteten `authorized_keys` (oder dem integrierten SSH-Server)
- **Forks & Upstream-Synchronisation** -- Forken Sie Repositories, synchronisieren Sie Forks mit dem Upstream per Mausklick und sehen Sie Fork-Beziehungen in der Oberflaeche
- **Git LFS** -- Large File Storage-Unterstuetzung fuer die Verfolgung binaerer Dateien
- **Repository-Spiegelung** -- Spiegeln Sie Repositories zu/von externen Git-Remotes
- **Vergleichsansicht** -- Vergleichen Sie Branches mit Voraus-/Rueckstand-Commit-Zaehlung und vollstaendigem Diff-Rendering
- **Sprachstatistiken** -- GitHub-aehnliche Sprachverteilungsleiste auf jeder Repository-Seite
- **Branch-Schutz** -- Konfigurierbare Regeln fuer erforderliche Reviews, Status-Checks, Verhinderung von Force-Push und CODEOWNERS-Genehmigungsdurchsetzung
- **Tag-Schutz** -- Schuetzen Sie Tags vor Loeschung, erzwungenen Aktualisierungen und unbefugter Erstellung mit Glob-Pattern-Matching und benutzerspezifischen Erlaubnislisten
- **Commit-Signatur-Verifizierung** -- GPG-Signaturverifizierung fuer Commits und annotierte Tags mit "Verified" / "Signed"-Badges in der Oberflaeche
- **Repository-Labels** -- Verwalten Sie Labels mit benutzerdefinierten Farben pro Repository; Labels werden beim Erstellen von Repos aus Templates automatisch kopiert
- **AGit Flow** -- Push-to-Review-Workflow: `git push origin HEAD:refs/for/main` erstellt einen Pull Request ohne Forking oder Erstellen von Remote-Branches. Aktualisiert vorhandene offene PRs bei nachfolgenden Pushes
- **Erkunden** -- Durchsuchen Sie alle zugaenglichen Repositories mit Suche, Sortierung und Themenfilterung
- **Suche** -- Volltextsuche ueber Repositories, Issues, PRs und Code

### Zusammenarbeit
- **Issues & Pull Requests** -- Erstellen, kommentieren, schliessen/wiedereroeffnen Sie Issues und PRs mit Labels, mehreren Zugewiesenen, Faelligkeitsdaten und Reviews. Mergen Sie PRs mit Merge-Commit-, Squash- oder Rebase-Strategien. Webbasierte Merge-Konfliktloesung mit Side-by-Side-Diff-Ansicht
- **Issue-Abhaengigkeiten** -- Definieren Sie "blockiert durch"- und "blockiert"-Beziehungen zwischen Issues mit zirkulaerer Abhaengigkeitserkennung
- **Issue-Anheften & Sperren** -- Heften Sie wichtige Issues oben an die Liste an und sperren Sie Konversationen, um weitere Kommentare zu verhindern
- **Kommentare bearbeiten & loeschen** -- Bearbeiten oder loeschen Sie Ihre eigenen Kommentare zu Issues und Pull Requests mit "(bearbeitet)"-Anzeige
- **Merge-Konfliktloesung** -- Loesen Sie Merge-Konflikte direkt im Browser mit einem visuellen Editor, der Base/Ours/Theirs-Ansichten, Schnell-Akzeptieren-Buttons und Konfliktmarkierungsvalidierung zeigt
- **Diskussionen** -- GitHub Discussions-aehnliche Thread-Konversationen pro Repository mit Kategorien (Allgemein, Fragen & Antworten, Ankuendigungen, Ideen, Zeigen & Erzaehlen, Umfragen), Anheften/Sperren, als Antwort markieren und Abstimmung
- **Code-Review-Vorschlaege** -- Der Modus "Aenderungen vorschlagen" in Inline-Reviews fuer PRs ermoeglicht es Reviewern, Code-Ersetzungen direkt im Diff vorzuschlagen
- **Reaktions-Emoji** -- Reagieren Sie auf Issues, PRs, Diskussionen und Kommentare mit Daumen hoch/runter, Herz, Lachen, Hurra, Verwirrt, Rakete und Augen
- **CODEOWNERS** -- Automatische Zuweisung von PR-Reviewern basierend auf Dateipfaden mit optionaler Durchsetzung, die CODEOWNERS-Genehmigung vor dem Merge erfordert
- **Repository-Templates** -- Erstellen Sie neue Repositories aus Templates mit automatischem Kopieren von Dateien, Labels, Issue-Templates und Branch-Schutzregeln
- **Entwurfs-Issues & Issue-Templates** -- Erstellen Sie Entwurfs-Issues (in Bearbeitung) und definieren Sie wiederverwendbare Issue-Templates (Fehlerbericht, Feature-Anfrage) pro Repository mit Standard-Labels
- **Wiki** -- Markdown-basierte Wiki-Seiten pro Repository mit Revisionshistorie
- **Projekte** -- Kanban-Boards mit Drag-and-Drop-Karten zur Arbeitsorganisation
- **Snippets** -- Teilen Sie Code-Snippets (wie GitHub Gists) mit Syntaxhervorhebung und mehreren Dateien
- **Organisationen & Teams** -- Erstellen Sie Organisationen mit Mitgliedern und Teams, weisen Sie Team-Berechtigungen fuer Repositories zu
- **Granulare Berechtigungen** -- Fuenfstufiges Berechtigungsmodell (Lesen, Triage, Schreiben, Pflegen, Admin) fuer feinkoernige Zugriffskontrolle auf Repositories
- **Meilensteine** -- Verfolgen Sie den Issue-Fortschritt in Richtung Meilensteine mit Fortschrittsbalken und Faelligkeitsdaten
- **Commit-Kommentare** -- Kommentieren Sie einzelne Commits mit optionalen Datei-/Zeilenverweisen
- **Repository-Themen** -- Versehen Sie Repositories mit Themen zur Entdeckung und Filterung auf der Erkunden-Seite

### CI/CD & DevOps
- **CI/CD-Runner** -- Definieren Sie Workflows in `.github/workflows/*.yml` und fuehren Sie sie in Docker-Containern aus. Automatische Ausloesung bei Push- und Pull-Request-Ereignissen
- **GitHub Actions-Kompatibilitaet** -- Dasselbe Workflow-YAML funktioniert sowohl auf MyPersonalGit als auch auf GitHub Actions. Uebersetzt `uses:`-Actions (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) in aequivalente Shell-Befehle
- **Parallele Jobs mit `needs:`** -- Jobs deklarieren Abhaengigkeiten ueber `needs:` und laufen parallel, wenn sie unabhaengig sind. Abhaengige Jobs warten auf ihre Voraussetzungen und werden automatisch abgebrochen, wenn eine Abhaengigkeit fehlschlaegt
- **Bedingte Schritte (`if:`)** -- Schritte unterstuetzen `if:`-Ausdruecke: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Bereinigungsschritte mit `if: failure()` oder `if: always()` werden auch nach frueheren Fehlern ausgefuehrt
- **Schritt-Ausgaben (`$GITHUB_OUTPUT`)** -- Schritte koennen `key=value` oder `key<<DELIMITER`-Mehrzeilenpaare in `$GITHUB_OUTPUT` schreiben, und nachfolgende Schritte empfangen sie als Umgebungsvariablen, kompatibel mit der Syntax `${{ steps.X.outputs.Y }}`
- **`github`-Kontext** -- `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` und `CI=true` werden automatisch in jeden Job injiziert
- **Matrix-Builds** -- `strategy.matrix` erweitert Jobs ueber mehrere Variablenkombinationen (z.B. OS x Version). Unterstuetzt `fail-fast` und `${{ matrix.X }}`-Substitution in `runs-on`, Schrittbefehlen und Schrittnamen
- **`workflow_dispatch`-Eingaben** -- Manuelle Ausloeser mit typisierten Eingabeparametern (String, Boolean, Choice, Number). Die Oberflaeche zeigt ein Eingabeformular beim manuellen Ausloesen von Workflows mit Eingaben. Werte werden als `INPUT_*`-Umgebungsvariablen injiziert
- **Job-Timeouts (`timeout-minutes`)** -- Setzen Sie `timeout-minutes` auf Jobs, um sie automatisch als fehlgeschlagen zu markieren, wenn sie das Limit ueberschreiten. Standard: 360 Minuten (entspricht GitHub Actions)
- **Job-Level `if:`** -- Ueberspringen Sie ganze Jobs basierend auf Bedingungen. Jobs mit `if: always()` laufen auch wenn Abhaengigkeiten fehlschlagen. Uebersprungene Jobs lassen den Run nicht fehlschlagen
- **Job-Ausgaben** -- Jobs deklarieren `outputs:`, die nachgelagerte `needs:`-Jobs ueber `${{ needs.X.outputs.Y }}` nutzen. Ausgaben werden aus Schritt-Ausgaben nach Abschluss des Jobs aufgeloest
- **`continue-on-error`** -- Markieren Sie einzelne Schritte als fehlertolerant, ohne den Job fehlschlagen zu lassen. Nuetzlich fuer optionale Validierungs- oder Benachrichtigungsschritte
- **`on.push.paths`-Filter** -- Loesen Sie Workflows nur aus, wenn bestimmte Dateien sich aendern. Unterstuetzt Glob-Muster (`src/**`, `*.ts`) und `paths-ignore:` fuer Ausnahmen
- **Workflows erneut ausfuehren** -- Fuehren Sie fehlgeschlagene, erfolgreiche oder abgebrochene Workflow-Runs mit einem Klick ueber die Actions-Oberflaeche erneut aus. Erstellt einen frischen Run mit derselben Konfiguration
- **`working-directory`** -- Setzen Sie `defaults.run.working-directory` auf Workflow-Ebene oder pro Schritt `working-directory:`, um zu steuern, wo Befehle ausgefuehrt werden
- **`defaults.run.shell`** -- Konfigurieren Sie eine benutzerdefinierte Shell pro Workflow oder Schritt (`bash`, `sh`, `python3` usw.)
- **`strategy.max-parallel`** -- Begrenzen Sie die gleichzeitige Ausfuehrung von Matrix-Jobs
- **`on.workflow_run`** -- Verketten Sie Workflows: Loesen Sie Workflow B aus, wenn Workflow A abgeschlossen ist. Filtern Sie nach Workflow-Name und `types: [completed]`
- **Automatische Release-Erstellung** -- `softprops/action-gh-release` erstellt echte Release-Entitaeten mit Tag, Titel, Changelog-Body und Pre-Release-/Draft-Flags. Quellcode-Archive (ZIP und TAR.GZ) werden automatisch als herunterladbare Assets angehaengt
- **Auto-Release-Pipeline** -- Integrierter Workflow, der automatisch Versionen taggt, Changelogs generiert und Docker-Images bei jedem Push auf main zu Docker Hub pusht
- **Commit-Status-Checks** -- Workflows setzen automatisch Pending/Success/Failure-Status auf Commits, sichtbar bei Pull Requests
- **Workflow-Abbruch** -- Brechen Sie laufende oder wartende Workflows ueber die Actions-Oberflaeche ab
- **Nebenlaeufigkeitskontrollen** -- Neue Pushes brechen automatisch wartende Runs desselben Workflows ab
- **Workflow-Umgebungsvariablen** -- Setzen Sie `env:` auf Workflow-, Job- oder Schrittebene in YAML
- **Status-Badges** -- Einbettbare SVG-Badges fuer Workflow- und Commit-Status (`/api/badge/{repo}/workflow`)
- **Artefakt-Downloads** -- Laden Sie Build-Artefakte direkt aus der Actions-Oberflaeche herunter
- **Geheimnisverwaltung** -- Verschluesselte Repository-Geheimnisse (AES-256), die als Umgebungsvariablen in CI/CD-Workflow-Runs injiziert werden
- **Webhooks** -- Loesen Sie externe Dienste bei Repository-Ereignissen aus
- **Prometheus-Metriken** -- Integrierter `/metrics`-Endpunkt zur Ueberwachung

### Paket- & Container-Hosting
- **Container Registry** -- Hosten Sie Docker/OCI-Images mit `docker push` und `docker pull` (OCI Distribution Spec)
- **NuGet Registry** -- Hosten Sie .NET-Pakete mit vollstaendiger NuGet v3 API (Service-Index, Suche, Push, Restore)
- **npm Registry** -- Hosten Sie Node.js-Pakete mit Standard npm publish/install
- **PyPI Registry** -- Hosten Sie Python-Pakete mit PEP 503 Simple API, JSON-Metadaten-API und `twine upload`-Kompatibilitaet
- **Maven Registry** -- Hosten Sie Java/JVM-Pakete mit Standard-Maven-Repository-Layout, `maven-metadata.xml`-Generierung und `mvn deploy`-Unterstuetzung
- **Generische Pakete** -- Laden Sie beliebige binaere Artefakte ueber die REST-API hoch und herunter

### Statische Webseiten
- **Pages** -- Stellen Sie statische Webseiten direkt aus einem Repository-Branch bereit (wie GitHub Pages) unter `/pages/{owner}/{repo}/`

### RSS/Atom-Feeds
- **Repository-Feeds** -- Atom-Feeds fuer Commits, Releases und Tags pro Repository (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Benutzer-Aktivitaets-Feed** -- Pro-Benutzer-Aktivitaets-Feed (`/api/feeds/users/{username}/activity.atom`)
- **Globaler Aktivitaets-Feed** -- Seitenweiter Aktivitaets-Feed (`/api/feeds/global/activity.atom`)

### Benachrichtigungen
- **In-App-Benachrichtigungen** -- Erwahnungen, Kommentare und Repository-Aktivitaeten
- **Push-Benachrichtigungen** -- Ntfy- und Gotify-Integration fuer Echtzeit-Mobil-/Desktop-Benachrichtigungen mit benutzerspezifischem Opt-in

### Authentifizierung
- **OAuth2 / SSO** -- Melden Sie sich mit GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord oder Twitter/X an. Administratoren konfigurieren Client-ID und Secret pro Anbieter im Admin-Dashboard -- nur Anbieter mit ausgefuellten Zugangsdaten werden den Benutzern angezeigt
- **OAuth2-Anbieter** -- Fungieren Sie als Identitaetsanbieter, damit andere Apps "Mit MyPersonalGit anmelden" verwenden koennen. Implementiert Authorization Code Flow mit PKCE, Token-Aktualisierung, Userinfo-Endpunkt und OpenID Connect Discovery (`.well-known/openid-configuration`)
- **LDAP / Active Directory** -- Authentifizieren Sie Benutzer gegen ein LDAP-Verzeichnis oder eine Active Directory-Domaene. Benutzer werden beim ersten Login automatisch angelegt mit synchronisierten Attributen (E-Mail, Anzeigename). Unterstuetzt gruppenbasierte Admin-Befoerderung, SSL/TLS und StartTLS
- **SSPI / Windows-integrierte Authentifizierung** -- Transparentes Single Sign-On fuer Windows-Domaenenbenutzer ueber Negotiate/NTLM. Benutzer in einer Domaene werden automatisch ohne Eingabe von Zugangsdaten authentifiziert. Aktivierbar unter Admin > Einstellungen (nur Windows)
- **Zwei-Faktor-Authentifizierung** -- TOTP-basierte 2FA mit Authenticator-App-Unterstuetzung und Wiederherstellungscodes
- **WebAuthn / Passkeys** -- FIDO2-Hardware-Sicherheitsschluessel und Passkey-Unterstuetzung als zweiter Faktor. Registrieren Sie YubiKeys, Plattform-Authentifikatoren (Face ID, Windows Hello, Touch ID) und andere FIDO2-Geraete. Sign-Count-Verifizierung zur Erkennung geklonter Schluessel
- **Verknuepfte Konten** -- Benutzer koennen mehrere OAuth-Anbieter mit ihrem Konto in den Einstellungen verknuepfen

### Administration
- **Admin-Dashboard** -- Systemeinstellungen (einschliesslich Datenbankanbieter, SSH-Server, LDAP/AD, Footer-Seiten), Benutzerverwaltung, Audit-Logs und Statistiken
- **Anpassbare Footer-Seiten** -- Nutzungsbedingungen, Datenschutzrichtlinie, Dokumentation und Kontaktseiten mit Markdown-Inhalt, bearbeitbar ueber Admin > Einstellungen
- **Benutzerprofile** -- Beitrags-Heatmap, Aktivitaets-Feed und Statistiken pro Benutzer
- **Persoenliche Zugangstokens** -- Token-basierte API-Authentifizierung mit konfigurierbaren Bereichen und optionalen routenspezifischen Einschraenkungen (Glob-Muster wie `/api/packages/**` zur Beschraenkung des Token-Zugriffs auf bestimmte API-Pfade)
- **Sicherung & Wiederherstellung** -- Exportieren und importieren Sie Serverdaten
- **Sicherheits-Scanning** -- Echtes Schwachstellen-Scanning von Abhaengigkeiten, betrieben von der [OSV.dev](https://osv.dev/)-Datenbank. Extrahiert automatisch Abhaengigkeiten aus `.csproj` (NuGet), `package.json` (npm) und `requirements.txt` (PyPI) und prueft jede gegen bekannte CVEs. Berichtet Schweregrad, behobene Versionen und Advisory-Links. Plus manuelle Sicherheitshinweise mit Entwurf-/Veroeffentlichungs-/Schliessen-Workflow
- **Dunkelmodus** -- Volle Unterstuetzung fuer Dunkel-/Hellmodus mit Umschalter im Header
- **Mehrsprachigkeit / i18n** -- Vollstaendige Lokalisierung ueber alle 27 Seiten mit 676 Ressourcenschluesseln. Wird mit 11 Sprachen ausgeliefert: Englisch, Spanisch, Franzoesisch, Deutsch, Japanisch, Koreanisch, Chinesisch (Vereinfacht), Portugiesisch, Russisch, Italienisch und Tuerkisch. Weitere hinzufuegen durch Erstellen von `SharedResource.{locale}.resx`-Dateien

## Technologie-Stack

| Komponente | Technologie |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (interaktives serverseitiges Rendering) |
| Datenbank | SQLite (Standard) oder PostgreSQL ueber Entity Framework Core 10 |
| Git-Engine | LibGit2Sharp |
| Authentifizierung | BCrypt-Passwort-Hashing, sitzungsbasierte Authentifizierung, PAT-Tokens, OAuth2 (8 Anbieter + Anbieter-Modus), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| SSH-Server | Integrierte SSH2-Protokollimplementierung (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Ueberwachung | Prometheus-Metriken |

## Schnellstart

### Voraussetzungen

- [Docker](https://docs.docker.com/get-docker/) (empfohlen)
- Oder [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git fuer die lokale Entwicklung

### Docker (Empfohlen)

Von Docker Hub herunterladen und ausfuehren:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Port 2222 ist optional -- nur erforderlich, wenn Sie den integrierten SSH-Server unter Admin > Einstellungen aktivieren.

Oder mit Docker Compose:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

Die App ist unter **http://localhost:8080** verfuegbar.

> **Standard-Zugangsdaten**: `admin` / `admin`
>
> **Aendern Sie das Standardpasswort sofort** ueber das Admin-Dashboard nach dem ersten Login.

### Lokal ausfuehren

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

Die App startet unter **http://localhost:5146**.

### Umgebungsvariablen

| Variable | Beschreibung | Standard |
|----------|-------------|---------|
| `Database__Provider` | Datenbank-Engine: `sqlite` oder `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Datenbank-Verbindungszeichenfolge | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Verzeichnis, in dem Git-Repos gespeichert werden | `/repos` |
| `Git__RequireAuth` | Authentifizierung fuer Git-HTTP-Operationen erforderlich | `true` |
| `Git__Users__<username>` | Passwort fuer Git HTTP Basic Auth-Benutzer setzen | -- |
| `RESET_ADMIN_PASSWORD` | Notfall-Admin-Passwort-Zuruecksetzung beim Start | -- |
| `Secrets__EncryptionKey` | Benutzerdefinierter Verschluesselungsschluessel fuer Repository-Geheimnisse | Abgeleitet aus dem DB-Verbindungsstring |
| `Ssh__DataDir` | Verzeichnis fuer SSH-Daten (Host-Keys, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Pfad zur generierten authorized_keys-Datei | `<DataDir>/authorized_keys` |

> **Hinweis:** Der Port des integrierten SSH-Servers und LDAP-Einstellungen werden ueber das Admin-Dashboard (Admin > Einstellungen) konfiguriert, nicht ueber Umgebungsvariablen. So koennen Sie diese ohne erneute Bereitstellung aendern.

## Nutzung

### 1. Anmelden

Oeffnen Sie die App und klicken Sie auf **Anmelden**. Bei einer Neuinstallation verwenden Sie die Standard-Zugangsdaten (`admin` / `admin`). Erstellen Sie zusaetzliche Benutzer ueber das **Admin**-Dashboard oder aktivieren Sie die Benutzerregistrierung unter Admin > Einstellungen.

### 2. Ein Repository erstellen

Klicken Sie auf der Startseite auf die gruene **Neu**-Schaltflaeche, geben Sie einen Namen ein und klicken Sie auf **Erstellen**. Dadurch wird ein Bare-Git-Repository auf dem Server erstellt, das Sie klonen, pushen und ueber die Weboberflaeche verwalten koennen.

### 3. Klonen und Pushen

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Wenn Git-HTTP-Authentifizierung aktiviert ist, werden Sie nach den Zugangsdaten gefragt, die ueber die Umgebungsvariablen `Git__Users__<username>` konfiguriert wurden. Diese sind getrennt vom Web-UI-Login.

### 4. Aus einer IDE klonen

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > `http://localhost:8080/git/MyRepo.git` einfuegen

**Visual Studio**: **Git > Repository klonen** > URL einfuegen

**JetBrains**: **Datei > Neu > Projekt aus Versionskontrolle** > URL einfuegen

### 5. Den Web-Editor verwenden

Sie koennen Dateien direkt im Browser bearbeiten:
- Navigieren Sie zu einem Repository und klicken Sie auf eine Datei, dann klicken Sie auf **Bearbeiten**
- Verwenden Sie **Dateien hinzufuegen > Neue Datei erstellen**, um Dateien ohne lokalen Klon hinzuzufuegen
- Verwenden Sie **Dateien hinzufuegen > Dateien/Ordner hochladen**, um von Ihrem Rechner hochzuladen

### 6. Container Registry

Pushen und pullen Sie Docker/OCI-Images direkt auf Ihren Server:

```bash
# Anmelden (verwenden Sie einen Personal Access Token aus Einstellungen > Zugangstokens)
docker login localhost:8080 -u youruser

# Ein Image pushen
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Ein Image pullen
docker pull localhost:8080/myapp:v1
```

> **Hinweis:** Docker erfordert standardmaessig HTTPS. Fuer HTTP fuegen Sie Ihren Server zu Dockers `insecure-registries` in `~/.docker/daemon.json` hinzu:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Paket-Registry

**NuGet (.NET-Pakete):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (Node.js-Pakete):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (Python-Pakete):**
```bash
# Ein Paket installieren
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# Mit twine hochladen
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

**Maven (Java/JVM-Pakete):**
```xml
<!-- In Ihrer pom.xml, Repository hinzufuegen -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- In settings.xml, Zugangsdaten hinzufuegen -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Generisch (beliebige Binaerdatei):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Durchsuchen Sie alle Pakete unter `/packages` in der Weboberflaeche.

### 8. Pages (Statisches Webseiten-Hosting)

Stellen Sie statische Webseiten aus einem Repository-Branch bereit:

1. Gehen Sie zum **Einstellungen**-Tab Ihres Repositorys und aktivieren Sie **Pages**
2. Setzen Sie den Branch (Standard: `gh-pages`)
3. Pushen Sie HTML/CSS/JS in diesen Branch
4. Besuchen Sie `http://localhost:8080/pages/{username}/{repo}/`

### 9. Push-Benachrichtigungen

Konfigurieren Sie Ntfy oder Gotify unter **Admin > Systemeinstellungen**, um Push-Benachrichtigungen auf Ihrem Telefon oder Desktop zu erhalten, wenn Issues, PRs oder Kommentare erstellt werden. Benutzer koennen sich unter **Einstellungen > Benachrichtigungen** ein-/abmelden.

### 10. SSH-Schluessel-Authentifizierung

Verwenden Sie SSH-Schluessel fuer passwortlose Git-Operationen. Es gibt zwei Optionen:

#### Option A: Integrierter SSH-Server (Empfohlen)

Kein externer SSH-Daemon erforderlich -- MyPersonalGit betreibt seinen eigenen SSH-Server:

1. Gehen Sie zu **Admin > Einstellungen** und aktivieren Sie den **Integrierten SSH-Server**
2. Setzen Sie den SSH-Port (Standard: 2222) -- verwenden Sie 22, wenn kein System-SSH laeuft
3. Speichern Sie die Einstellungen und starten Sie den Server neu (Portaenderungen erfordern Neustart)
4. Gehen Sie zu **Einstellungen > SSH-Schluessel** und fuegen Sie Ihren oeffentlichen Schluessel hinzu (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` oder `~/.ssh/id_ecdsa.pub`)
5. Ueber SSH klonen:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Der integrierte SSH-Server unterstuetzt ECDH-SHA2-NISTP256-Schluesselaustausch, AES-128/256-CTR-Verschluesselung, HMAC-SHA2-256 und Public-Key-Authentifizierung mit Ed25519-, RSA- und ECDSA-Schluesseln.

#### Option B: System-OpenSSH

Wenn Sie den SSH-Daemon Ihres Systems bevorzugen:

1. Gehen Sie zu **Einstellungen > SSH-Schluessel** und fuegen Sie Ihren oeffentlichen Schluessel hinzu
2. MyPersonalGit pflegt automatisch eine `authorized_keys`-Datei aus allen registrierten SSH-Schluesseln
3. Konfigurieren Sie das OpenSSH Ihres Servers zur Verwendung der generierten authorized_keys-Datei:
   ```
   # In /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Ueber SSH klonen:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

Der SSH-Auth-Dienst stellt auch eine API unter `/api/ssh/authorized-keys` bereit, die mit der `AuthorizedKeysCommand`-Direktive von OpenSSH verwendet werden kann.

### 11. LDAP / Active Directory-Authentifizierung

Authentifizieren Sie Benutzer gegen das LDAP-Verzeichnis oder die Active Directory-Domaene Ihrer Organisation:

1. Gehen Sie zu **Admin > Einstellungen** und scrollen Sie zu **LDAP / Active Directory-Authentifizierung**
2. Aktivieren Sie LDAP und geben Sie Ihre Serverdetails ein:
   - **Server**: Ihr LDAP-Server-Hostname (z.B. `dc01.corp.local`)
   - **Port**: 389 fuer LDAP, 636 fuer LDAPS
   - **SSL/TLS**: Aktivieren fuer LDAPS, oder StartTLS fuer das Upgrade einer unverschluesselten Verbindung verwenden
3. Konfigurieren Sie ein Dienstkonto fuer die Benutzersuche:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind-Passwort**: Das Passwort des Dienstkontos
4. Setzen Sie die Suchparameter:
   - **Such-Basis-DN**: `OU=Users,DC=corp,DC=local`
   - **Benutzerfilter**: `(sAMAccountName={0})` fuer AD, `(uid={0})` fuer OpenLDAP
5. Ordnen Sie LDAP-Attribute den Benutzerfeldern zu:
   - **Benutzername**: `sAMAccountName` (AD) oder `uid` (OpenLDAP)
   - **E-Mail**: `mail`
   - **Anzeigename**: `displayName`
6. Setzen Sie optional einen **Admin-Gruppen-DN** -- Mitglieder dieser Gruppe werden automatisch zum Admin befoerdert
7. Klicken Sie auf **LDAP-Verbindung testen**, um die Einstellungen zu ueberpruefen
8. Einstellungen speichern

Benutzer koennen sich nun mit ihren Domaenen-Zugangsdaten auf der Anmeldeseite anmelden. Beim ersten Login wird automatisch ein lokales Konto mit aus dem Verzeichnis synchronisierten Attributen erstellt. LDAP-Authentifizierung wird auch fuer Git-HTTP-Operationen (Klonen/Pushen) verwendet.

### 12. Repository-Geheimnisse

Fuegen Sie Repositories verschluesselte Geheimnisse fuer die Verwendung in CI/CD-Workflows hinzu:

1. Gehen Sie zum **Einstellungen**-Tab Ihres Repositorys
2. Scrollen Sie zur **Geheimnisse**-Karte und klicken Sie auf **Geheimnis hinzufuegen**
3. Geben Sie einen Namen ein (z.B. `DEPLOY_TOKEN`) und einen Wert -- der Wert wird mit AES-256 verschluesselt
4. Geheimnisse werden automatisch als Umgebungsvariablen in jeden Workflow-Run injiziert

Referenzieren Sie Geheimnisse in Ihrem Workflow:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO-Anmeldung

Melden Sie sich mit externen Identitaetsanbietern an:

1. Gehen Sie zu **Admin > OAuth / SSO** und konfigurieren Sie die Anbieter, die Sie aktivieren moechten
2. Geben Sie die **Client-ID** und das **Client-Secret** aus der Entwicklerkonsole des Anbieters ein
3. Aktivieren Sie **Aktivieren** -- nur Anbieter mit beiden ausgefuellten Zugangsdaten werden auf der Anmeldeseite angezeigt
4. Die Callback-URL fuer jeden Anbieter wird im Admin-Panel angezeigt (z.B. `https://yourserver/oauth/callback/github`)

Unterstuetzte Anbieter: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Benutzer koennen mehrere Anbieter mit ihrem Konto unter **Einstellungen > Verknuepfte Konten** verknuepfen.

### 14. Repository importieren

Importieren Sie Repositories aus externen Quellen mit vollstaendiger Historie:

1. Klicken Sie auf der Startseite auf **Importieren**
2. Waehlen Sie einen Quellentyp (Git-URL, GitHub, GitLab oder Bitbucket)
3. Geben Sie die Repository-URL und optional ein Auth-Token fuer private Repos ein
4. Fuer GitHub/GitLab/Bitbucket-Importe koennen Sie optional Issues und Pull Requests importieren
5. Verfolgen Sie den Importfortschritt in Echtzeit auf der Import-Seite

### 15. Forking & Upstream-Synchronisation

Forken Sie ein Repository und halten Sie es synchron:

1. Klicken Sie auf die **Fork**-Schaltflaeche auf einer beliebigen Repository-Seite
2. Ein Fork wird unter Ihrem Benutzernamen mit einem Link zum Original erstellt
3. Klicken Sie neben dem "Geforkt von"-Badge auf **Fork synchronisieren**, um die neuesten Aenderungen vom Upstream zu holen

### 16. CI/CD Auto-Release

MyPersonalGit enthaelt eine integrierte CI/CD-Pipeline, die bei jedem Push auf main automatisch taggt, released und Docker-Images pusht. Workflows werden bei Push automatisch ausgeloest -- kein externer CI-Dienst erforderlich.

**So funktioniert es:**
1. Ein Push auf `main` loest automatisch `.github/workflows/release.yml` aus
2. Erhoeht die Patch-Version (`v1.15.1` -> `v1.15.2`), erstellt einen Git-Tag
3. Meldet sich bei Docker Hub an, baut das Image und pusht es als `:latest` und `:vX.Y.Z`

**Einrichtung:**
1. Gehen Sie zu den **Einstellungen > Geheimnisse** Ihres Repos in MyPersonalGit
2. Fuegen Sie ein Geheimnis namens `DOCKERHUB_TOKEN` mit Ihrem Docker Hub-Zugangstokens hinzu
3. Stellen Sie sicher, dass der MyPersonalGit-Container den Docker-Socket gemountet hat (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Pushen Sie auf main -- der Workflow wird automatisch ausgeloest

**GitHub Actions-Kompatibilitaet:**
Dasselbe Workflow-YAML funktioniert auch auf GitHub Actions -- keine Aenderungen noetig. MyPersonalGit uebersetzt `uses:`-Actions zur Laufzeit in aequivalente Shell-Befehle:

| GitHub Action | MyPersonalGit-Uebersetzung |
|---|---|
| `actions/checkout@v4` | Repo bereits nach `/workspace` geklont |
| `actions/setup-dotnet@v4` | Installiert .NET SDK ueber das offizielle Installationsskript |
| `actions/setup-node@v4` | Installiert Node.js ueber NodeSource |
| `actions/setup-python@v5` | Installiert Python ueber apt/apk |
| `actions/setup-java@v4` | Installiert OpenJDK ueber apt/apk |
| `docker/login-action@v3` | `docker login` mit stdin-Passwort |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | Keine Aktion (verwendet Standard-Builder) |
| `softprops/action-gh-release@v2` | Erstellt eine echte Release-Entitaet in der Datenbank |
| `${{ secrets.X }}` | `$X`-Umgebungsvariable |
| `${{ steps.X.outputs.Y }}` | `$Y`-Umgebungsvariable |
| `${{ github.sha }}` | `$GITHUB_SHA`-Umgebungsvariable |

**Parallele Jobs:**
Jobs laufen standardmaessig parallel. Verwenden Sie `needs:`, um Abhaengigkeiten zu deklarieren:
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
Jobs ohne `needs:` starten sofort. Ein Job wird abgebrochen, wenn eine seiner Abhaengigkeiten fehlschlaegt.

**Bedingte Schritte:**
Verwenden Sie `if:`, um zu steuern, wann Schritte ausgefuehrt werden:
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
Unterstuetzte Ausdruecke: `always()`, `success()` (Standard), `failure()`, `cancelled()`, `true`, `false`.

**Schritt-Ausgaben:**
Schritte koennen Werte an nachfolgende Schritte ueber `$GITHUB_OUTPUT` weitergeben:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Matrix-Builds:**
Faechern Sie Jobs ueber mehrere Kombinationen mit `strategy.matrix` auf:
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
Dies erstellt 4 Jobs: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)` usw. Alle laufen parallel.

**Manuelle Ausloeser mit Eingaben (`workflow_dispatch`):**
Definieren Sie typisierte Eingaben, die als Formular in der Oberflaeche beim manuellen Ausloesen angezeigt werden:
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
Eingabewerte werden als `INPUT_<NAME>`-Umgebungsvariablen (grossgeschrieben) injiziert.

**Job-Timeouts:**
Setzen Sie `timeout-minutes` auf Jobs, um sie automatisch als fehlgeschlagen zu markieren, wenn sie zu lange laufen:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Standard-Timeout ist 360 Minuten (6 Stunden), entspricht GitHub Actions.

**Job-Level-Bedingungen:**
Verwenden Sie `if:` bei Jobs, um sie basierend auf Bedingungen zu ueberspringen:
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

**Job-Ausgaben:**
Jobs koennen Werte ueber `outputs:` an nachgelagerte Jobs weitergeben:
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

**Bei Fehler fortfahren:**
Lassen Sie einen Schritt fehlschlagen, ohne den Job zu beenden:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Pfadfilterung:**
Loesen Sie Workflows nur aus, wenn bestimmte Dateien sich aendern:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # or use paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Arbeitsverzeichnis:**
Legen Sie fest, wo Befehle ausgefuehrt werden:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # runs in src/app
      - run: npm test
        working-directory: tests  # overrides default
```

**Workflows erneut ausfuehren:**
Klicken Sie auf die **Erneut ausfuehren**-Schaltflaeche bei jedem abgeschlossenen, fehlgeschlagenen oder abgebrochenen Workflow-Run, um einen frischen Run mit denselben Jobs, Schritten und Konfigurationen zu erstellen.

**Pull-Request-Workflows:**
Workflows mit `on: pull_request` werden automatisch ausgeloest, wenn ein Nicht-Entwurfs-PR erstellt wird, und fuehren Checks gegen den Quell-Branch aus.

**Commit-Status-Checks:**
Workflows setzen automatisch Commit-Statusse (Pending/Success/Failure), damit Sie Build-Ergebnisse bei PRs sehen und erforderliche Checks ueber Branch-Schutz durchsetzen koennen.

**Workflow-Abbruch:**
Klicken Sie auf die **Abbrechen**-Schaltflaeche bei jedem laufenden oder wartenden Workflow in der Actions-Oberflaeche, um ihn sofort zu stoppen.

**Status-Badges:**
Betten Sie Build-Status-Badges in Ihre README oder anderswo ein:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Nach Workflow-Name filtern: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom-Feeds

Abonnieren Sie Repository-Aktivitaeten ueber Standard-Atom-Feeds in jedem RSS-Reader:

```
# Repository-Commits
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Repository-Releases
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Repository-Tags
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Benutzer-Aktivitaet
http://localhost:8080/api/feeds/users/admin/activity.atom

# Globale Aktivitaet (alle Repositories)
http://localhost:8080/api/feeds/global/activity.atom
```

Keine Authentifizierung fuer oeffentliche Repositories erforderlich. Fuegen Sie diese URLs zu jedem Feed-Reader (Feedly, Miniflux, FreshRSS usw.) hinzu, um ueber Aenderungen informiert zu bleiben.

## Datenbank-Konfiguration

MyPersonalGit verwendet standardmaessig **SQLite** -- keine Konfiguration, Einzeldatei-Datenbank, perfekt fuer den persoenlichen Gebrauch und kleine Teams.

Fuer groessere Bereitstellungen (viele gleichzeitige Benutzer, hohe Verfuegbarkeit oder wenn Sie bereits PostgreSQL betreiben) koennen Sie auf **PostgreSQL** umschalten:

### PostgreSQL verwenden

**Docker Compose** (empfohlen fuer PostgreSQL):
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

**Nur Umgebungsvariablen** (wenn Sie bereits einen PostgreSQL-Server haben):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

EF Core-Migrationen laufen beim Start automatisch fuer beide Anbieter. Kein manuelles Schema-Setup erforderlich.

### Umschalten ueber das Admin-Dashboard

Sie koennen den Datenbankanbieter auch direkt ueber die Weboberflaeche umschalten:

1. Gehen Sie zu **Admin > Einstellungen** -- die **Datenbank**-Karte befindet sich oben
2. Waehlen Sie **PostgreSQL** aus dem Anbieter-Dropdown
3. Geben Sie Ihren PostgreSQL-Verbindungsstring ein (z.B. `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Klicken Sie auf **Datenbankeinstellungen speichern**
5. Starten Sie die Anwendung neu, damit die Aenderung wirksam wird

Die Konfiguration wird in `~/.mypersonalgit/database.json` gespeichert (ausserhalb der Datenbank selbst, damit sie vor dem Verbinden gelesen werden kann).

### Eine Datenbank waehlen

| | SQLite | PostgreSQL |
|---|---|---|
| **Einrichtung** | Keine Konfiguration (Standard) | Erfordert einen PostgreSQL-Server |
| **Ideal fuer** | Persoenlichen Gebrauch, kleine Teams, NAS | Teams von 50+, hohe Nebenlaeufigkeit |
| **Sicherung** | `.db`-Datei kopieren | Standard `pg_dump` |
| **Nebenlaeufigkeit** | Einzelner Schreiber (ausreichend fuer die meisten Faelle) | Volle Multi-Writer-Unterstuetzung |
| **Migration** | Nicht zutreffend | Anbieter wechseln + App starten (migriert automatisch) |

## Auf einem NAS bereitstellen

MyPersonalGit funktioniert hervorragend auf einem NAS (QNAP, Synology usw.) ueber Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Der Docker-Socket-Mount ist optional -- nur erforderlich, wenn Sie CI/CD-Workflow-Ausfuehrung wuenschen. Port 2222 wird nur benoetigt, wenn Sie den integrierten SSH-Server aktivieren.

## Konfiguration

Alle Einstellungen koennen in `appsettings.json`, ueber Umgebungsvariablen oder ueber das Admin-Dashboard unter `/admin` konfiguriert werden:

- Datenbankanbieter (SQLite oder PostgreSQL)
- Projektverzeichnis
- Authentifizierungsanforderungen
- Benutzerregistrierungseinstellungen
- Funktionsumschalter (Issues, Wiki, Projekte, Actions)
- Maximale Repository-Groesse und Anzahl pro Benutzer
- SMTP-Einstellungen fuer E-Mail-Benachrichtigungen
- Push-Benachrichtigungseinstellungen (Ntfy/Gotify)
- Integrierter SSH-Server (aktivieren/deaktivieren, Port)
- LDAP/Active Directory-Authentifizierung (Server, Bind-DN, Such-Basis, Benutzerfilter, Attributzuordnung, Admin-Gruppe)
- OAuth/SSO-Anbieterkonfiguration (Client-ID/Secret pro Anbieter)

## Projektstruktur

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor-Seiten (Home, RepoDetails, Issues, PRs, Packages usw.)
  Controllers/       # REST-API-Endpunkte (NuGet, npm, Generic, Registry usw.)
  Data/              # EF Core DbContext, Service-Implementierungen
  Models/            # Domaenenmodelle
  Migrations/        # EF Core-Migrationen
  Services/          # Middleware (Auth, Git-HTTP-Backend, Pages, Registry-Auth)
    SshServer/       # Integrierter SSH-Server (SSH2-Protokoll, ECDH, AES-CTR)
  Program.cs         # App-Start, DI, Middleware-Pipeline
MyPersonalGit.Tests/
  UnitTest1.cs       # xUnit-Tests mit InMemory-Datenbank
```

## Tests ausfuehren

```bash
dotnet test
```

## Lizenz

MIT
