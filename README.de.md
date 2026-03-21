🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Ein selbst gehosteter Git-Server mit einer GitHub-ähnlichen Weboberfläche, gebaut mit ASP.NET Core und Blazor Server. Durchsuchen Sie Repositories, verwalten Sie Issues, Pull Requests, Wikis, Projekte und mehr — alles auf Ihrem eigenen Rechner oder Server.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## Inhaltsverzeichnis

- [Funktionen](#funktionen)
- [Technologie-Stack](#technologie-stack)
- [Schnellstart](#schnellstart)
  - [Docker (Empfohlen)](#docker-empfohlen)
  - [Lokal ausführen](#lokal-ausführen)
  - [Umgebungsvariablen](#umgebungsvariablen)
- [Verwendung](#verwendung)
  - [Anmelden](#1-anmelden)
  - [Ein Repository erstellen](#2-ein-repository-erstellen)
  - [Klonen und Pushen](#3-klonen-und-pushen)
  - [Aus einer IDE klonen](#4-aus-einer-ide-klonen)
  - [Web-Editor](#5-den-web-editor-verwenden)
  - [Container Registry](#6-container-registry)
  - [Package Registry](#7-package-registry)
  - [Pages (Statische Websites)](#8-pages-statisches-website-hosting)
  - [Push-Benachrichtigungen](#9-push-benachrichtigungen)
  - [SSH-Schlüssel-Authentifizierung](#10-ssh-schlüssel-authentifizierung)
  - [LDAP / Active Directory](#11-ldap--active-directory-authentifizierung)
  - [Repository Secrets](#12-repository-secrets)
  - [OAuth / SSO-Anmeldung](#13-oauth--sso-anmeldung)
  - [Repository importieren](#14-repository-importieren)
  - [Forking & Upstream-Synchronisation](#15-forking--upstream-synchronisation)
  - [CI/CD Auto-Release](#16-cicd-auto-release)
  - [RSS/Atom-Feeds](#17-rssatom-feeds)
- [Datenbankkonfiguration](#datenbankkonfiguration)
  - [PostgreSQL verwenden](#postgresql-verwenden)
  - [Umschalten über das Admin-Dashboard](#umschalten-über-das-admin-dashboard)
  - [Datenbank auswählen](#datenbank-auswählen)
- [Deployment auf einem NAS](#deployment-auf-einem-nas)
- [Konfiguration](#konfiguration)
- [Projektstruktur](#projektstruktur)
- [Tests ausführen](#tests-ausführen)
- [Lizenz](#lizenz)

---

## Funktionen

### Code & Repositories
- **Repository-Verwaltung** — Erstellen, durchsuchen und löschen Sie Git-Repositories mit einem vollständigen Code-Browser, Datei-Editor, Commit-Verlauf, Branches und Tags
- **Repository-Import/Migration** — Importieren Sie Repositories von GitHub, GitLab, Bitbucket, Gitea/Forgejo/Gogs oder einer beliebigen Git-URL mit optionalem Issue- und PR-Import. Hintergrundverarbeitung mit Fortschrittsanzeige
- **Repository-Archivierung** — Markieren Sie Repositories als schreibgeschützt mit visuellen Badges; Pushes werden bei archivierten Repos blockiert
- **Git Smart HTTP** — Klonen, Fetchen und Pushen über HTTP mit Basic Auth
- **Integrierter SSH-Server** — Nativer SSH-Server für Git-Operationen — kein externes OpenSSH erforderlich. Unterstützt ECDH-Schlüsselaustausch, AES-CTR-Verschlüsselung und Public-Key-Authentifizierung (RSA, ECDSA, Ed25519)
- **SSH-Schlüssel-Authentifizierung** — Fügen Sie SSH-Public-Keys zu Ihrem Konto hinzu und authentifizieren Sie Git-Operationen über SSH mit automatisch verwalteter `authorized_keys` (oder dem integrierten SSH-Server)
- **Forks & Upstream-Synchronisation** — Forken Sie Repositories, synchronisieren Sie Forks mit einem Klick und sehen Sie Fork-Beziehungen in der Oberfläche
- **Git LFS** — Large File Storage-Unterstützung für die Verwaltung binärer Dateien
- **Repository-Spiegelung** — Spiegeln Sie Repositories zu/von externen Git-Remotes
- **Vergleichsansicht** — Vergleichen Sie Branches mit Ahead/Behind-Commit-Zählern und vollständigem Diff-Rendering
- **Sprachstatistiken** — GitHub-ähnliche Sprachaufschlüsselung auf jeder Repository-Seite
- **Branch Protection** — Konfigurierbare Regeln für erforderliche Reviews, Status-Checks, Schutz vor Force-Push und CODEOWNERS-Genehmigungsdurchsetzung
- **Signierte Commits erforderlich** — Branch-Protection-Regel, die verlangt, dass alle Commits GPG-signiert sind, bevor sie gemergt werden
- **Tag Protection** — Schützen Sie Tags vor Löschung, erzwungenen Updates und unbefugter Erstellung mit Glob-Pattern-Matching und benutzerspezifischen Freigabelisten
- **Commit-Signatur-Verifizierung** — GPG-Signaturverifizierung bei Commits und annotierten Tags mit "Verified"/"Signed"-Badges in der Oberfläche
- **Repository Labels** — Verwalten Sie Labels mit benutzerdefinierten Farben pro Repository; Labels werden automatisch kopiert, wenn Repos aus Vorlagen erstellt werden
- **AGit Flow** — Push-to-Review-Workflow: `git push origin HEAD:refs/for/main` erstellt einen Pull Request, ohne zu forken oder Remote-Branches zu erstellen. Aktualisiert bestehende offene PRs bei nachfolgenden Pushes
- **Entdecken** — Durchsuchen Sie alle zugänglichen Repositories mit Suche, Sortierung und Topic-Filterung
- **Star from Explore** — Repositories direkt von der Entdecken-Seite mit Stern markieren und entmarkieren, ohne jedes Repo öffnen zu müssen
- **Autolink References** — Automatische Umwandlung von `#123` in Issue-Links sowie konfigurierbare benutzerdefinierte Muster (z. B. `JIRA-456` → externe URLs) pro Repository
- **Suche** — Volltextsuche über Repositories, Issues, PRs und Code
- **License Detection** — Erkennt automatisch LICENSE-Dateien und identifiziert gängige Lizenzen (MIT, Apache-2.0, GPL, BSD, ISC, MPL, Unlicense) mit einem Badge in der Repository-Seitenleiste

### Zusammenarbeit
- **Issues & Pull Requests** — Erstellen, kommentieren, schließen/wiedereröffnen Sie Issues und PRs mit Labels, mehreren Zugewiesenen, Fälligkeitsdaten und Reviews. Mergen Sie PRs mit Merge-Commit-, Squash- oder Rebase-Strategien. Webbasierte Merge-Konfliktlösung mit Side-by-Side-Diff-Ansicht
- **Issue-Abhängigkeiten** — Definieren Sie "blockiert durch"- und "blockiert"-Beziehungen zwischen Issues mit Erkennung zirkulärer Abhängigkeiten
- **Issue-Pinning & -Sperrung** — Pinnen Sie wichtige Issues an den Anfang der Liste und sperren Sie Konversationen, um weitere Kommentare zu verhindern
- **Kommentare bearbeiten & löschen** — Bearbeiten oder löschen Sie Ihre eigenen Kommentare bei Issues und Pull Requests mit "(bearbeitet)"-Anzeige
- **@Mention Notifications** — @erwähnen Sie Benutzer in Kommentaren, um ihnen eine direkte Benachrichtigung zu senden
- **Merge-Konfliktlösung** — Lösen Sie Merge-Konflikte direkt im Browser mit einem visuellen Editor, der Base/Ours/Theirs-Ansichten, Schnellauswahl-Buttons und Konfliktmarker-Validierung zeigt
- **Squash Commit Message** — Passen Sie die Commit-Nachricht beim Squash-Merge eines Pull Requests an
- **Branch Delete After Merge** — Option zum automatischen Löschen des Quellbranches nach dem Zusammenführen eines Pull Requests, standardmäßig aktiviert
- **Diskussionen** — GitHub-Discussions-ähnliche Thread-Konversationen pro Repository mit Kategorien (Allgemein, Fragen & Antworten, Ankündigungen, Ideen, Zeig & Erzähl, Umfragen), Pinnen/Sperren, als Antwort markieren und Upvoting
- **Code-Review-Vorschläge** — Der "Änderungen vorschlagen"-Modus in PR-Inline-Reviews ermöglicht es Reviewern, Code-Ersetzungen direkt im Diff vorzuschlagen
- **Image Diff** — Seite-an-Seite-Bildvergleich in Pull Requests mit Opazitätsregler für visuelles Diffing geänderter Bilder (PNG, JPG, GIF, SVG, WebP)
- **File Tree in PRs** — Einklappbare Dateibaum-Seitenleiste in der Pull-Request-Diff-Ansicht zur einfachen Navigation zwischen geänderten Dateien
- **Dateien als gesehen markieren** — Fortschrittsanzeige für Reviews in Pull Requests mit "Gesehen"-Checkboxen pro Datei und einem Fortschrittszähler
- **Diff-Syntaxhervorhebung** — Sprachbewusste Syntaxfärbung in Pull-Request- und Vergleichs-Diffs über Prism.js
- **Reaktions-Emoji** — Reagieren Sie auf Issues, PRs, Diskussionen und Kommentare mit Daumen hoch/runter, Herz, Lachen, Hurra, Verwirrt, Rakete und Augen
- **Auto-Merge** — Aktivieren Sie Auto-Merge bei Pull Requests, um automatisch zusammenzuführen, wenn alle erforderlichen Status-Checks bestanden und Reviews genehmigt sind
- **CI Status on PR List** — Die Pull-Request-Liste zeigt grüne/rote/gelbe CI-Statusicons neben jedem PR-Titel
- **Cherry-Pick / Revert via UI** — Wählen Sie beliebige Commits für einen anderen Branch aus oder machen Sie einen Commit rückgängig, entweder direkt oder als neuer Pull Request, über die Web-Oberfläche
- **Transfer Issues** — Verschieben Sie Issues zwischen Repositories unter Beibehaltung von Titel, Text, Kommentaren, passenden Labels und Verlinkung des Originals mit einem Transferhinweis
- **Saved Replies** — Speichern Sie vorgefertigte Antworten und fügen Sie diese schnell beim Kommentieren von Issues oder Pull Requests ein
- **Batch Issue Operations** — Wählen Sie mehrere Issues aus und schließen oder öffnen Sie diese gesammelt über die Issue-Liste
- **CODEOWNERS** — Automatische Zuweisung von PR-Reviewern basierend auf Dateipfaden mit optionaler Durchsetzung, die CODEOWNERS-Genehmigung vor dem Merge erfordert
- **Repository-Vorlagen** — Erstellen Sie neue Repositories aus Vorlagen mit automatischem Kopieren von Dateien, Labels, Issue-Vorlagen und Branch-Protection-Regeln
- **Entwurfs-Issues & Issue-Vorlagen** — Erstellen Sie Entwurfs-Issues (Work-in-Progress) und definieren Sie wiederverwendbare Issue-Vorlagen (Bug-Report, Feature-Request) pro Repository mit Standard-Labels
- **PR Template** — Pull-Request-Beschreibungen automatisch aus `.github/PULL_REQUEST_TEMPLATE.md` vorausfüllen
- **Release Editing** — Bearbeiten Sie Release-Titel, Beschreibungen und Entwurf-/Vorabversion-Flags nach der Erstellung
- **Wiki** — Markdown-basierte Wiki-Seiten pro Repository mit Revisionshistorie
- **Projekte** — Kanban-Boards mit Drag-and-Drop-Karten zur Arbeitsorganisation
- **Snippets** — Teilen Sie Code-Snippets (wie GitHub Gists) mit Syntaxhervorhebung und mehreren Dateien
- **Organisationen & Teams** — Erstellen Sie Organisationen mit Mitgliedern und Teams, weisen Sie Team-Berechtigungen für Repositories zu
- **Granulare Berechtigungen** — Fünfstufiges Berechtigungsmodell (Lesen, Triage, Schreiben, Pflegen, Admin) für feinkörnige Zugriffskontrolle auf Repositories
- **Meilensteine** — Verfolgen Sie den Issue-Fortschritt in Richtung Meilensteine mit Fortschrittsbalken und Fälligkeitsdaten
- **Commit-Kommentare** — Kommentieren Sie einzelne Commits mit optionalen Datei-/Zeilenreferenzen
- **Repository Topics** — Taggen Sie Repositories mit Topics zur Entdeckung und Filterung auf der Entdecken-Seite
- **Activity Pulse** — Wöchentliche Zusammenfassungsseite pro Repository mit gemergten PRs, geöffneten/geschlossenen Issues, Commits, Top-Beitragenden und aktiven Branches der letzten 7 Tage

### CI/CD & DevOps
- **CI/CD Runner** — Definieren Sie Workflows in `.github/workflows/*.yml` und führen Sie sie in Docker-Containern aus. Automatische Auslösung bei Push- und Pull-Request-Events
- **GitHub Actions-Kompatibilität** — Dasselbe Workflow-YAML funktioniert sowohl auf MyPersonalGit als auch auf GitHub Actions. Übersetzt `uses:`-Actions (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) in äquivalente Shell-Befehle
- **Parallele Jobs mit `needs:`** — Jobs deklarieren Abhängigkeiten über `needs:` und laufen parallel, wenn sie unabhängig sind. Abhängige Jobs warten auf ihre Voraussetzungen und werden automatisch abgebrochen, wenn eine Abhängigkeit fehlschlägt
- **Bedingte Schritte (`if:`)** — Schritte unterstützen `if:`-Ausdrücke: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Aufräumschritte mit `if: failure()` oder `if: always()` laufen auch nach früheren Fehlern
- **Schritt-Ausgaben (`$GITHUB_OUTPUT`)** — Schritte können `key=value`- oder `key<<DELIMITER`-Multiline-Paare in `$GITHUB_OUTPUT` schreiben, und nachfolgende Schritte erhalten sie als Umgebungsvariablen, kompatibel mit `${{ steps.X.outputs.Y }}`-Syntax
- **`github`-Kontext** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` und `CI=true` werden automatisch in jeden Job injiziert
- **Matrix-Builds** — `strategy.matrix` expandiert Jobs über mehrere Variablenkombinationen (z.B. OS x Version). Unterstützt `fail-fast` und `${{ matrix.X }}`-Substitution in `runs-on`, Schrittbefehlen und Schrittnamen
- **`workflow_dispatch`-Eingaben** — Manuelle Auslöser mit typisierten Eingabeparametern (String, Boolean, Choice, Number). Die Oberfläche zeigt ein Eingabeformular beim manuellen Auslösen von Workflows mit Eingaben. Werte werden als `INPUT_*`-Umgebungsvariablen injiziert
- **Job-Timeouts (`timeout-minutes`)** — Setzen Sie `timeout-minutes` auf Jobs, um sie automatisch fehlschlagen zu lassen, wenn sie das Limit überschreiten. Standard: 360 Minuten (entspricht GitHub Actions)
- **Job-Level `if:`** — Überspringen Sie ganze Jobs basierend auf Bedingungen. Jobs mit `if: always()` laufen auch, wenn Abhängigkeiten fehlschlagen. Übersprungene Jobs lassen den Run nicht fehlschlagen
- **Job-Ausgaben** — Jobs deklarieren `outputs:`, die nachgelagerte `needs:`-Jobs über `${{ needs.X.outputs.Y }}` konsumieren. Ausgaben werden aus Schritt-Ausgaben aufgelöst, nachdem der Job abgeschlossen ist
- **`continue-on-error`** — Markieren Sie einzelne Schritte als fehlschlagen-erlaubt, ohne den Job fehlschlagen zu lassen. Nützlich für optionale Validierungs- oder Benachrichtigungsschritte
- **`on.push.paths`-Filter** — Lösen Sie Workflows nur aus, wenn bestimmte Dateien sich ändern. Unterstützt Glob-Patterns (`src/**`, `*.ts`) und `paths-ignore:` für Ausschlüsse
- **Workflows erneut ausführen** — Führen Sie fehlgeschlagene, erfolgreiche oder abgebrochene Workflow-Runs mit einem Klick über die Actions-Oberfläche erneut aus. Erstellt einen neuen Run mit derselben Konfiguration
- **`working-directory`** — Setzen Sie `defaults.run.working-directory` auf Workflow-Ebene oder pro Schritt `working-directory:`, um zu steuern, wo Befehle ausgeführt werden
- **`defaults.run.shell`** — Konfigurieren Sie eine benutzerdefinierte Shell pro Workflow oder pro Schritt (`bash`, `sh`, `python3`, etc.)
- **`strategy.max-parallel`** — Begrenzen Sie die gleichzeitige Ausführung von Matrix-Jobs
- **Reusable Workflows (`workflow_call`)** — Definieren Sie Workflows mit `on: workflow_call`, die andere Workflows mit `uses: ./.github/workflows/build.yml` aufrufen können. Unterstützt typisierte Eingaben, Ausgaben und Secrets. Jobs des aufgerufenen Workflows werden im Aufrufer eingebettet
- **Composite Actions** — Definieren Sie mehrstufige Aktionen in `.github/actions/{name}/action.yml` mit `runs: using: composite`. Schritte aus Composite Actions werden bei der Ausführung inline erweitert
- **Environment Deployments** — Konfigurieren Sie Deployment-Umgebungen (z.B. `staging`, `production`) mit Schutzregeln: erforderliche Reviewer, Wartezeiten und Branch-Einschränkungen. Workflow-Jobs mit `environment:` erfordern eine Genehmigung vor der Ausführung. Vollständiger Deployment-Verlauf mit Genehmigungs-/Ablehnungs-UI
- **`on.workflow_run`** — Verketten Sie Workflows: Lösen Sie Workflow B aus, wenn Workflow A abgeschlossen ist. Filtern Sie nach Workflow-Name und `types: [completed]`
- **Automatische Release-Erstellung** — `softprops/action-gh-release` erstellt echte Release-Entitäten mit Tag, Titel, Changelog-Text und Pre-Release/Draft-Flags. Quellcode-Archive (ZIP und TAR.GZ) werden automatisch als herunterladbare Assets angehängt
- **Auto-Release-Pipeline** — Integrierter Workflow, der automatisch Versionen taggt, Changelogs generiert und Docker-Images bei jedem Push auf main zu Docker Hub pusht
- **Commit-Status-Checks** — Workflows setzen automatisch Pending/Success/Failure-Status auf Commits, sichtbar bei Pull Requests
- **Workflow-Abbruch** — Brechen Sie laufende oder wartende Workflows über die Actions-Oberfläche ab
- **Nebenläufigkeitskontrollen** — Neue Pushes brechen automatisch wartende Runs desselben Workflows ab
- **Workflow-Umgebungsvariablen** — Setzen Sie `env:` auf Workflow-, Job- oder Schrittebene in YAML
- **Status-Badges** — Einbettbare SVG-Badges für Workflow- und Commit-Status (`/api/badge/{repo}/workflow`)
- **Artefakt-Downloads** — Laden Sie Build-Artefakte direkt aus der Actions-Oberfläche herunter
- **Secrets-Verwaltung** — Verschlüsselte Repository-Secrets (AES-256), die als Umgebungsvariablen in CI/CD-Workflow-Runs injiziert werden
- **Webhooks** — Lösen Sie externe Dienste bei Repository-Ereignissen aus
- **Prometheus-Metriken** — Integrierter `/metrics`-Endpunkt für Monitoring

### Package- & Container-Hosting (20 registries)
- **Container Registry** — Hosten Sie Docker/OCI-Images mit `docker push` und `docker pull` (OCI Distribution Spec)
- **NuGet Registry** — Hosten Sie .NET-Pakete mit vollständiger NuGet v3 API (Service Index, Suche, Push, Restore)
- **npm Registry** — Hosten Sie Node.js-Pakete mit Standard npm publish/install
- **PyPI Registry** — Hosten Sie Python-Pakete mit PEP 503 Simple API, JSON Metadata API und `twine upload`-Kompatibilität
- **Maven Registry** — Hosten Sie Java/JVM-Pakete mit Standard-Maven-Repository-Layout, `maven-metadata.xml`-Generierung und `mvn deploy`-Unterstützung
- **Alpine Registry** — Hosten Sie Alpine Linux `.apk`-Pakete mit APKINDEX-Generierung
- **RPM Registry** — Hosten Sie RPM-Pakete mit `repomd.xml`-Metadaten für `dnf`/`yum`
- **Chef Registry** — Hosten Sie Chef-Cookbooks mit Chef Supermarket-kompatibler API
- **Generische Pakete** — Laden Sie beliebige binäre Artefakte über die REST API hoch und herunter

### Statische Websites
- **Pages** — Stellen Sie statische Websites direkt aus einem Repository-Branch bereit (wie GitHub Pages) unter `/pages/{owner}/{repo}/`

### RSS/Atom-Feeds
- **Repository-Feeds** — Atom-Feeds für Commits, Releases und Tags pro Repository (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Benutzer-Aktivitäts-Feed** — Pro-Benutzer-Aktivitäts-Feed (`/api/feeds/users/{username}/activity.atom`)
- **Globaler Aktivitäts-Feed** — Seitenweiter Aktivitäts-Feed (`/api/feeds/global/activity.atom`)

### Benachrichtigungen
- **In-App-Benachrichtigungen** — Erwähnungen, Kommentare und Repository-Aktivität
- **Push-Benachrichtigungen** — Ntfy- und Gotify-Integration für Echtzeit-Mobil-/Desktop-Benachrichtigungen mit benutzerspezifischer Opt-in-Funktion

### Authentifizierung
- **OAuth2 / SSO** — Anmeldung mit GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord oder Twitter/X. Administratoren konfigurieren Client-ID und Secret pro Anbieter im Admin-Dashboard — nur Anbieter mit ausgefüllten Anmeldeinformationen werden den Benutzern angezeigt
- **OAuth2-Provider** — Fungieren Sie als Identitätsanbieter, damit andere Apps "Mit MyPersonalGit anmelden" verwenden können. Implementiert Authorization Code Flow mit PKCE, Token-Refresh, Userinfo-Endpunkt und OpenID Connect Discovery (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Authentifizieren Sie Benutzer gegen ein LDAP-Verzeichnis oder eine Active Directory-Domäne. Benutzer werden beim ersten Login automatisch mit synchronisierten Attributen (E-Mail, Anzeigename) angelegt. Unterstützt gruppenbasierte Admin-Beförderung, SSL/TLS und StartTLS
- **SSPI / Windows Integrated Auth** — Transparentes Single Sign-On für Windows-Domänenbenutzer über Negotiate/NTLM. Benutzer in einer Domäne werden automatisch ohne Eingabe von Anmeldeinformationen authentifiziert. Aktivierung unter Admin > Einstellungen (nur Windows)
- **Zwei-Faktor-Authentifizierung** — TOTP-basierte 2FA mit Authenticator-App-Unterstützung und Wiederherstellungscodes
- **WebAuthn / Passkeys** — FIDO2-Hardware-Sicherheitsschlüssel und Passkey-Unterstützung als zweiter Faktor. Registrieren Sie YubiKeys, Plattform-Authenticatoren (Face ID, Windows Hello, Touch ID) und andere FIDO2-Geräte. Signaturzähler-Verifizierung zur Erkennung geklonter Schlüssel
- **Verknüpfte Konten** — Benutzer können mehrere OAuth-Anbieter mit ihrem Konto in den Einstellungen verknüpfen

### Administration
- **Admin-Dashboard** — Systemeinstellungen (einschließlich Datenbankanbieter, SSH-Server, LDAP/AD, Footer-Seiten), Benutzerverwaltung, Audit-Logs und Statistiken
- **Anpassbare Footer-Seiten** — Nutzungsbedingungen, Datenschutzrichtlinie, Dokumentation und Kontaktseiten mit Markdown-Inhalten, bearbeitbar unter Admin > Einstellungen
- **Benutzerprofile** — Beitrags-Heatmap, Aktivitäts-Feed und Statistiken pro Benutzer
- **Gravatar Avatars** — Benutzer-Avatare in der gesamten Oberfläche verwenden Gravatar-Identicons basierend auf dem Benutzernamen, mit automatischem Fallback
- **Persönliche Zugriffstoken** — Token-basierte API-Authentifizierung mit konfigurierbaren Geltungsbereichen und optionalen routenspezifischen Einschränkungen (Glob-Patterns wie `/api/packages/**` zur Beschränkung des Token-Zugriffs auf bestimmte API-Pfade)
- **Backup & Wiederherstellung** — Exportieren und importieren Sie Serverdaten
- **Sicherheitsscanning** — Echte Schwachstellenanalyse von Abhängigkeiten, basierend auf der [OSV.dev](https://osv.dev/)-Datenbank. Extrahiert automatisch Abhängigkeiten aus `.csproj` (NuGet), `package.json` (npm), `requirements.txt` (PyPI), `Cargo.toml` (Rust), `Gemfile` (Ruby), `composer.json` (PHP), `go.mod` (Go), `pom.xml` (Maven/Java) und `pubspec.yaml` (Dart/Flutter) und prüft jede gegen bekannte CVEs. Berichtet Schweregrad, behobene Versionen und Advisory-Links. Plus manuelle Sicherheitshinweise mit Entwurf/Veröffentlichung/Schließen-Workflow
- **Secret Scanning** — Scannt jeden Push automatisch auf geleakte Anmeldeinformationen (AWS-Schlüssel, GitHub/GitLab-Tokens, Slack-Tokens, private Schlüssel, API-Schlüssel, JWTs, Verbindungszeichenfolgen und mehr). 20 integrierte Muster mit vollständiger Regex-Unterstützung. Vollständiger Repository-Scan auf Abruf. Warnungen mit Workflow zum Auflösen/Falsch-Positiv. Benutzerdefinierte Muster über API konfigurierbar
- **Dependabot-Style Auto-Update PRs** — Überprüft automatisch veraltete Abhängigkeiten und erstellt Pull Requests zur Aktualisierung. Unterstützt NuGet-, npm- und PyPI-Ökosysteme. Konfigurierbarer Zeitplan (täglich/wöchentlich/monatlich) und Limit für offene PRs pro Repository
- **Repository Insights (Traffic)** — Verfolgen Sie Clone/Fetch-Zähler, Seitenaufrufe, eindeutige Besucher, Top-Referrer und beliebte Inhaltspfade. Verkehrsdiagramme im Insights-Tab mit 14-Tage-Zusammenfassungen. Tägliche Aggregation mit 90-Tage-Aufbewahrung. IP-Adressen werden für den Datenschutz gehasht
- **Dark Mode** — Vollständige Dark/Light-Mode-Unterstützung mit einem Umschalter im Header
- **Mehrsprachigkeit / i18n** — Vollständige Lokalisierung über alle 30 Seiten mit 930 Ressourcenschlüsseln. Wird mit 11 Sprachen ausgeliefert: Englisch, Spanisch, Französisch, Deutsch, Japanisch, Koreanisch, Chinesisch (vereinfacht), Portugiesisch, Russisch, Italienisch und Türkisch. Sprachauswahl in der Kopfzeile. Weitere Sprachen können durch Erstellen von `SharedResource.{locale}.resx`-Dateien hinzugefügt werden
- **Swagger / OpenAPI** — Interaktive API-Dokumentation unter `/swagger` mit allen REST-Endpunkten auffindbar und testbar
- **Open Graph Meta Tags** — Repository-, Issue- und PR-Seiten enthalten og:title und og:description für erweiterte Link-Vorschauen in Slack, Discord und sozialen Medien
- **Mermaid Diagrams** — Mermaid-Diagramm-Rendering in Markdown-Dateien (Flussdiagramme, Sequenzdiagramme, Gantt-Diagramme usw.)
- **Math Rendering** — LaTeX/KaTeX-Mathematikausdrücke in Markdown (`$inline$`- und `$$display$$`-Syntax)
- **CSV/TSV Viewer** — CSV- und TSV-Dateien werden als formatierte, sortierbare Tabellen anstatt als Rohtext dargestellt
- **Keyboard Shortcuts** — Drücken Sie `?` für ein Tastenkürzel-Hilfefenster. `/` fokussiert die Suche, `g i` navigiert zu Issues, `g p` zu Pull Requests, `g h` zur Startseite, `g n` zu Benachrichtigungen
- **Health Check Endpoint** — `/health` gibt JSON mit dem Datenbankverbindungsstatus für Docker/Kubernetes-Monitoring zurück
- **Sitemap.xml** — Dynamische XML-Sitemap unter `/sitemap.xml`, die alle öffentlichen Repositories für die Suchmaschinenindexierung auflistet
- **Line Linking** — Klicken Sie auf Zeilennummern im Dateibetrachter, um teilbare `#L42`-URLs mit Zeilenhervorhebung beim Laden zu generieren
- **File Download** — Laden Sie einzelne Dateien aus dem Dateibetrachter mit korrekten Content-Disposition-Headern herunter
- **Jupyter Notebook Rendering** — `.ipynb`-Dateien werden als formatierte Notebooks mit Code-Zellen, Markdown, Ausgaben und Inline-Bildern dargestellt
- **Repository Transfer** — Übertragen Sie die Repository-Inhaberschaft an einen anderen Benutzer oder eine Organisation über die Repository-Einstellungen
- **Default Branch Configuration** — Ändern Sie den Standard-Branch pro Repository über den Einstellungen-Tab
- **Rename Repository** — Benennen Sie ein Repository über Settings um, mit automatischer Aktualisierung aller Referenzen (Issues, PRs, Sterne, Webhooks, Secrets usw.)
- **User-Level Secrets** — Verschlüsselte Secrets, die für alle Repositories eines Benutzers freigegeben sind, verwaltet unter Settings > Secrets
- **Organization-Level Secrets** — Verschlüsselte Secrets, die für alle Repositories einer Organisation freigegeben sind, verwaltet über den Secrets-Tab der Organisation
- **Repository Pinning** — Heften Sie bis zu 6 Lieblings-Repositories an Ihre Benutzerprofilseite für schnellen Zugriff an
- **Git Hooks Management** — Web-Oberfläche zum Anzeigen, Bearbeiten und Verwalten von serverseitigen Git Hooks (pre-receive, update, post-receive, post-update, pre-push) pro Repository
- **Protected File Patterns** — Branch-Schutzregel mit Glob-Mustern, um eine Review-Genehmigung für Änderungen an bestimmten Dateien zu erfordern (z. B. `*.lock`, `migrations/**`, `.github/workflows/*`)
- **External Issue Tracker** — Konfigurieren Sie Repositories zur Verlinkung mit einem externen Issue-Tracker (Jira, Linear, etc.) mit benutzerdefinierten URL-Mustern
- **Federation (NodeInfo/WebFinger)** — NodeInfo 2.0-Erkennung, WebFinger und host-meta für instanzübergreifende Auffindbarkeit
- **Distributed CI Runners** — Externe Runner können sich über API registrieren, wartende Jobs abfragen und Ergebnisse melden

## Technologie-Stack

| Komponente | Technologie |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (interaktives serverseitiges Rendering) |
| Datenbank | SQLite (Standard) oder PostgreSQL via Entity Framework Core 10 |
| Git Engine | LibGit2Sharp |
| Auth | BCrypt-Passwort-Hashing, sitzungsbasierte Authentifizierung, PAT-Tokens, OAuth2 (8 Anbieter + Provider-Modus), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| SSH-Server | Integrierte SSH2-Protokollimplementierung (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Monitoring | Prometheus-Metriken |

## Schnellstart

### Voraussetzungen

- [Docker](https://docs.docker.com/get-docker/) (empfohlen)
- Oder [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git für lokale Entwicklung

### Docker (Empfohlen)

Von Docker Hub herunterladen und ausführen:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Port 2222 ist optional — wird nur benötigt, wenn Sie den integrierten SSH-Server unter Admin > Einstellungen aktivieren.

Oder verwenden Sie Docker Compose:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

Die Anwendung ist unter **http://localhost:8080** verfügbar.

> **Standard-Anmeldedaten**: `admin` / `admin`
>
> **Ändern Sie das Standardpasswort sofort** über das Admin-Dashboard nach der ersten Anmeldung.

### Lokal ausführen

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

Die Anwendung startet unter **http://localhost:5146**.

### Umgebungsvariablen

| Variable | Beschreibung | Standard |
|----------|-------------|---------|
| `Database__Provider` | Datenbank-Engine: `sqlite` oder `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Datenbank-Verbindungszeichenfolge | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Verzeichnis, in dem Git-Repos gespeichert werden | `/repos` |
| `Git__RequireAuth` | Authentifizierung für Git-HTTP-Operationen erforderlich | `true` |
| `Git__Users__<username>` | Passwort für Git-HTTP-Basic-Auth-Benutzer festlegen | — |
| `RESET_ADMIN_PASSWORD` | Notfall-Admin-Passwort-Reset beim Start | — |
| `Secrets__EncryptionKey` | Benutzerdefinierter Verschlüsselungsschlüssel für Repository-Secrets | Abgeleitet von der DB-Verbindungszeichenfolge |
| `Ssh__DataDir` | Verzeichnis für SSH-Daten (Host-Schlüssel, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Pfad zur generierten authorized_keys-Datei | `<DataDir>/authorized_keys` |

> **Hinweis:** Der integrierte SSH-Server-Port und die LDAP-Einstellungen werden über das Admin-Dashboard (Admin > Einstellungen) konfiguriert, nicht über Umgebungsvariablen. So können Sie diese ohne erneutes Deployment ändern.

## Verwendung

### 1. Anmelden

Öffnen Sie die Anwendung und klicken Sie auf **Anmelden**. Bei einer frischen Installation verwenden Sie die Standard-Anmeldedaten (`admin` / `admin`). Erstellen Sie zusätzliche Benutzer über das **Admin**-Dashboard oder aktivieren Sie die Benutzerregistrierung unter Admin > Einstellungen.

### 2. Ein Repository erstellen

Klicken Sie auf der Startseite auf die grüne Schaltfläche **Neu**, geben Sie einen Namen ein und klicken Sie auf **Erstellen**. Dadurch wird ein Bare-Git-Repository auf dem Server erstellt, das Sie klonen, pushen und über die Weboberfläche verwalten können.

### 3. Klonen und Pushen

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Wenn Git-HTTP-Authentifizierung aktiviert ist, werden Sie nach den über `Git__Users__<username>`-Umgebungsvariablen konfigurierten Anmeldedaten gefragt. Diese sind separat von der Web-UI-Anmeldung.

### 4. Aus einer IDE klonen

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > `http://localhost:8080/git/MyRepo.git` einfügen

**Visual Studio**: **Git > Repository klonen** > URL einfügen

**JetBrains**: **File > New > Project from Version Control** > URL einfügen

### 5. Den Web-Editor verwenden

Sie können Dateien direkt im Browser bearbeiten:
- Navigieren Sie zu einem Repository und klicken Sie auf eine beliebige Datei, dann klicken Sie auf **Bearbeiten**
- Verwenden Sie **Dateien hinzufügen > Neue Datei erstellen**, um Dateien ohne lokalen Klon hinzuzufügen
- Verwenden Sie **Dateien hinzufügen > Dateien/Ordner hochladen**, um von Ihrem Rechner hochzuladen

### 6. Container Registry

Pushen und pullen Sie Docker/OCI-Images direkt auf Ihren Server:

```bash
# Anmelden (verwenden Sie einen persönlichen Zugriffstoken aus Einstellungen > Zugriffstoken)
docker login localhost:8080 -u youruser

# Ein Image pushen
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Ein Image pullen
docker pull localhost:8080/myapp:v1
```

> **Hinweis:** Docker erfordert standardmäßig HTTPS. Für HTTP fügen Sie Ihren Server zu Dockers `insecure-registries` in `~/.docker/daemon.json` hinzu:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Package Registry

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
<!-- In Ihrer pom.xml das Repository hinzufügen -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- In settings.xml Anmeldeinformationen hinzufügen -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Generisch (beliebige Binärdateien):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Durchsuchen Sie alle Pakete unter `/packages` in der Weboberfläche.

### 8. Pages (Statisches Website-Hosting)

Stellen Sie statische Websites aus einem Repository-Branch bereit:

1. Gehen Sie zum **Einstellungen**-Tab Ihres Repositorys und aktivieren Sie **Pages**
2. Legen Sie den Branch fest (Standard: `gh-pages`)
3. Pushen Sie HTML/CSS/JS in diesen Branch
4. Besuchen Sie `http://localhost:8080/pages/{username}/{repo}/`

### 9. Push-Benachrichtigungen

Konfigurieren Sie Ntfy oder Gotify unter **Admin > Systemeinstellungen**, um Push-Benachrichtigungen auf Ihrem Smartphone oder Desktop zu erhalten, wenn Issues, PRs oder Kommentare erstellt werden. Benutzer können unter **Einstellungen > Benachrichtigungen** ein-/ausschalten.

### 10. SSH-Schlüssel-Authentifizierung

Verwenden Sie SSH-Schlüssel für passwortlose Git-Operationen. Es gibt zwei Optionen:

#### Option A: Integrierter SSH-Server (Empfohlen)

Kein externer SSH-Daemon erforderlich — MyPersonalGit betreibt seinen eigenen SSH-Server:

1. Gehen Sie zu **Admin > Einstellungen** und aktivieren Sie den **Integrierten SSH-Server**
2. Legen Sie den SSH-Port fest (Standard: 2222) — verwenden Sie 22, wenn kein System-SSH läuft
3. Speichern Sie die Einstellungen und starten Sie den Server neu (Portänderungen erfordern einen Neustart)
4. Gehen Sie zu **Einstellungen > SSH-Schlüssel** und fügen Sie Ihren öffentlichen Schlüssel hinzu (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` oder `~/.ssh/id_ecdsa.pub`)
5. Klonen über SSH:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Der integrierte SSH-Server unterstützt ECDH-SHA2-NISTP256-Schlüsselaustausch, AES-128/256-CTR-Verschlüsselung, HMAC-SHA2-256 und Public-Key-Authentifizierung mit Ed25519-, RSA- und ECDSA-Schlüsseln.

#### Option B: System-OpenSSH

Wenn Sie den SSH-Daemon Ihres Systems bevorzugen:

1. Gehen Sie zu **Einstellungen > SSH-Schlüssel** und fügen Sie Ihren öffentlichen Schlüssel hinzu
2. MyPersonalGit pflegt automatisch eine `authorized_keys`-Datei aus allen registrierten SSH-Schlüsseln
3. Konfigurieren Sie den OpenSSH Ihres Servers zur Verwendung der generierten authorized_keys-Datei:
   ```
   # In /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Klonen über SSH:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

Der SSH-Auth-Dienst stellt auch eine API unter `/api/ssh/authorized-keys` zur Verwendung mit der `AuthorizedKeysCommand`-Direktive von OpenSSH bereit.

### 11. LDAP / Active Directory-Authentifizierung

Authentifizieren Sie Benutzer gegen das LDAP-Verzeichnis oder die Active Directory-Domäne Ihrer Organisation:

1. Gehen Sie zu **Admin > Einstellungen** und scrollen Sie zu **LDAP / Active Directory-Authentifizierung**
2. Aktivieren Sie LDAP und geben Sie Ihre Serverdetails ein:
   - **Server**: Ihr LDAP-Server-Hostname (z.B. `dc01.corp.local`)
   - **Port**: 389 für LDAP, 636 für LDAPS
   - **SSL/TLS**: Aktivieren für LDAPS, oder verwenden Sie StartTLS zum Upgraden einer unverschlüsselten Verbindung
3. Konfigurieren Sie ein Dienstkonto für die Benutzersuche:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind-Passwort**: Das Passwort des Dienstkontos
4. Legen Sie die Suchparameter fest:
   - **Such-Basis-DN**: `OU=Users,DC=corp,DC=local`
   - **Benutzerfilter**: `(sAMAccountName={0})` für AD, `(uid={0})` für OpenLDAP
5. Ordnen Sie LDAP-Attribute den Benutzerfeldern zu:
   - **Benutzername**: `sAMAccountName` (AD) oder `uid` (OpenLDAP)
   - **E-Mail**: `mail`
   - **Anzeigename**: `displayName`
6. Optional eine **Admin-Gruppen-DN** festlegen — Mitglieder dieser Gruppe werden automatisch zu Administratoren befördert
7. Klicken Sie auf **LDAP-Verbindung testen**, um die Einstellungen zu überprüfen
8. Einstellungen speichern

Benutzer können sich jetzt mit ihren Domänenanmeldeinformationen auf der Anmeldeseite anmelden. Beim ersten Login wird automatisch ein lokales Konto mit synchronisierten Attributen aus dem Verzeichnis erstellt. Die LDAP-Authentifizierung wird auch für Git-HTTP-Operationen (Clone/Push) verwendet.

### 12. Repository Secrets

Fügen Sie verschlüsselte Secrets zu Repositories für die Verwendung in CI/CD-Workflows hinzu:

1. Gehen Sie zum **Einstellungen**-Tab Ihres Repositorys
2. Scrollen Sie zur **Secrets**-Karte und klicken Sie auf **Secret hinzufügen**
3. Geben Sie einen Namen (z.B. `DEPLOY_TOKEN`) und einen Wert ein — der Wert wird mit AES-256 verschlüsselt
4. Secrets werden automatisch als Umgebungsvariablen in jeden Workflow-Run injiziert

Referenzieren Sie Secrets in Ihrem Workflow:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO-Anmeldung

Anmeldung mit externen Identitätsanbietern:

1. Gehen Sie zu **Admin > OAuth / SSO** und konfigurieren Sie die gewünschten Anbieter
2. Geben Sie die **Client-ID** und das **Client Secret** aus der Entwicklerkonsole des Anbieters ein
3. Aktivieren Sie **Aktivieren** — nur Anbieter mit beiden ausgefüllten Anmeldeinformationen werden auf der Anmeldeseite angezeigt
4. Die Callback-URL für jeden Anbieter wird im Admin-Panel angezeigt (z.B. `https://yourserver/oauth/callback/github`)

Unterstützte Anbieter: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Benutzer können mehrere Anbieter mit ihrem Konto unter **Einstellungen > Verknüpfte Konten** verknüpfen.

### 14. Repository importieren

Importieren Sie Repositories aus externen Quellen mit vollständigem Verlauf:

1. Klicken Sie auf der Startseite auf **Importieren**
2. Wählen Sie einen Quelltyp (Git-URL, GitHub, GitLab oder Bitbucket)
3. Geben Sie die Repository-URL und optional ein Auth-Token für private Repos ein
4. Bei GitHub/GitLab/Bitbucket-Importen können Sie optional Issues und Pull Requests importieren
5. Verfolgen Sie den Importfortschritt in Echtzeit auf der Import-Seite

### 15. Forking & Upstream-Synchronisation

Forken Sie ein Repository und halten Sie es synchron:

1. Klicken Sie auf die **Fork**-Schaltfläche auf einer beliebigen Repository-Seite
2. Ein Fork wird unter Ihrem Benutzernamen erstellt, mit einem Link zum Original
3. Klicken Sie neben dem "Geforkt von"-Badge auf **Fork synchronisieren**, um die neuesten Änderungen vom Upstream zu übernehmen

### 16. CI/CD Auto-Release

MyPersonalGit enthält eine integrierte CI/CD-Pipeline, die bei jedem Push auf main automatisch taggt, releast und Docker-Images pusht. Workflows werden automatisch bei Push ausgelöst — kein externer CI-Dienst erforderlich.

**So funktioniert es:**
1. Push auf `main` löst automatisch `.github/workflows/release.yml` aus
2. Erhöht die Patch-Version (`v1.15.1` -> `v1.15.2`), erstellt einen Git-Tag
3. Meldet sich bei Docker Hub an, baut das Image und pusht es als `:latest` und `:vX.Y.Z`

**Einrichtung:**
1. Gehen Sie zu den **Einstellungen > Secrets** Ihres Repos in MyPersonalGit
2. Fügen Sie ein Secret namens `DOCKERHUB_TOKEN` mit Ihrem Docker Hub-Zugriffstoken hinzu
3. Stellen Sie sicher, dass der MyPersonalGit-Container den Docker-Socket gemountet hat (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Pushen Sie auf main — der Workflow wird automatisch ausgelöst

**GitHub Actions-Kompatibilität:**
Dasselbe Workflow-YAML funktioniert auch auf GitHub Actions — keine Änderungen nötig. MyPersonalGit übersetzt `uses:`-Actions zur Laufzeit in äquivalente Shell-Befehle:

| GitHub Action | MyPersonalGit-Übersetzung |
|---|---|
| `actions/checkout@v4` | Repo bereits nach `/workspace` geklont |
| `actions/setup-dotnet@v4` | Installiert .NET SDK über offizielles Installationsskript |
| `actions/setup-node@v4` | Installiert Node.js über NodeSource |
| `actions/setup-python@v5` | Installiert Python über apt/apk |
| `actions/setup-java@v4` | Installiert OpenJDK über apt/apk |
| `docker/login-action@v3` | `docker login` mit stdin-Passwort |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op (verwendet Standard-Builder) |
| `softprops/action-gh-release@v2` | Erstellt eine echte Release-Entität in der Datenbank |
| `${{ secrets.X }}` | `$X`-Umgebungsvariable |
| `${{ steps.X.outputs.Y }}` | `$Y`-Umgebungsvariable |
| `${{ github.sha }}` | `$GITHUB_SHA`-Umgebungsvariable |

**Parallele Jobs:**
Jobs laufen standardmäßig parallel. Verwenden Sie `needs:`, um Abhängigkeiten zu deklarieren:
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
Jobs ohne `needs:` starten sofort. Ein Job wird abgebrochen, wenn eine seiner Abhängigkeiten fehlschlägt.

**Bedingte Schritte:**
Verwenden Sie `if:`, um zu steuern, wann Schritte ausgeführt werden:
```yaml
steps:
  - name: Build
    run: dotnet build

  - name: Bei Fehler benachrichtigen
    if: failure()
    run: curl -X POST https://hooks.example.com/alert

  - name: Aufräumen
    if: always()
    run: rm -rf ./tmp
```
Unterstützte Ausdrücke: `always()`, `success()` (Standard), `failure()`, `cancelled()`, `true`, `false`.

**Schritt-Ausgaben:**
Schritte können Werte an nachfolgende Schritte über `$GITHUB_OUTPUT` weitergeben:
```yaml
steps:
  - name: Version bestimmen
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Version verwenden
    run: echo "Building version $version"
```

**Matrix-Builds:**
Fächern Sie Jobs über mehrere Kombinationen mit `strategy.matrix` auf:
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

**Manuelle Auslöser mit Eingaben (`workflow_dispatch`):**
Definieren Sie typisierte Eingaben, die als Formular in der Oberfläche angezeigt werden, wenn manuell ausgelöst wird:
```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Zielumgebung"
        required: true
        type: choice
        options:
          - staging
          - production
      debug:
        description: "Debug-Modus aktivieren"
        type: boolean
        default: "false"

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying to $INPUT_ENVIRONMENT (debug=$INPUT_DEBUG)"
```
Eingabewerte werden als `INPUT_<NAME>`-Umgebungsvariablen (in Großbuchstaben) injiziert.

**Job-Timeouts:**
Setzen Sie `timeout-minutes` auf Jobs, um sie automatisch fehlschlagen zu lassen, wenn sie zu lange laufen:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Standard-Timeout beträgt 360 Minuten (6 Stunden), entsprechend GitHub Actions.

**Job-Level-Bedingungen:**
Verwenden Sie `if:` auf Jobs, um sie basierend auf Bedingungen zu überspringen:
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
Jobs können Werte an nachgelagerte Jobs über `outputs:` weitergeben:
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
Lassen Sie einen Schritt fehlschlagen, ohne den Job fehlschlagen zu lassen:
```yaml
steps:
  - name: Optionales Linting
    continue-on-error: true
    run: npm run lint

  - name: Build (läuft immer)
    run: npm run build
```

**Pfadfilterung:**
Lösen Sie Workflows nur aus, wenn bestimmte Dateien sich ändern:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # oder verwenden Sie paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Arbeitsverzeichnis:**
Legen Sie fest, wo Befehle ausgeführt werden:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # läuft in src/app
      - run: npm test
        working-directory: tests  # überschreibt Standard
```

**Workflows erneut ausführen:**
Klicken Sie auf die **Erneut ausführen**-Schaltfläche bei jedem abgeschlossenen, fehlgeschlagenen oder abgebrochenen Workflow-Run, um einen neuen Run mit denselben Jobs, Schritten und Konfigurationen zu erstellen.

**Pull-Request-Workflows:**
Workflows mit `on: pull_request` werden automatisch ausgelöst, wenn ein Nicht-Entwurfs-PR erstellt wird, und führen Checks gegen den Quell-Branch aus.

**Commit-Status-Checks:**
Workflows setzen automatisch Commit-Status (Pending/Success/Failure), damit Sie Build-Ergebnisse bei PRs sehen und erforderliche Checks über Branch Protection durchsetzen können.

**Workflow-Abbruch:**
Klicken Sie in der Actions-Oberfläche auf die **Abbrechen**-Schaltfläche bei jedem laufenden oder wartenden Workflow, um ihn sofort zu stoppen.

**Status-Badges:**
Betten Sie Build-Status-Badges in Ihre README oder anderswo ein:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Nach Workflow-Name filtern: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom-Feeds

Abonnieren Sie Repository-Aktivitäten mit Standard-Atom-Feeds in jedem RSS-Reader:

```
# Repository-Commits
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Repository-Releases
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Repository-Tags
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Benutzer-Aktivität
http://localhost:8080/api/feeds/users/admin/activity.atom

# Globale Aktivität (alle Repositories)
http://localhost:8080/api/feeds/global/activity.atom
```

Keine Authentifizierung für öffentliche Repositories erforderlich. Fügen Sie diese URLs zu jedem Feed-Reader (Feedly, Miniflux, FreshRSS usw.) hinzu, um über Änderungen informiert zu bleiben.

## Datenbankkonfiguration

MyPersonalGit verwendet standardmäßig **SQLite** — keine Konfiguration erforderlich, Einzeldatei-Datenbank, perfekt für den persönlichen Gebrauch und kleine Teams.

Für größere Deployments (viele gleichzeitige Benutzer, Hochverfügbarkeit oder wenn Sie bereits PostgreSQL betreiben) können Sie zu **PostgreSQL** wechseln:

### PostgreSQL verwenden

**Docker Compose** (empfohlen für PostgreSQL):
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

EF Core-Migrationen werden beim Start für beide Anbieter automatisch ausgeführt. Kein manuelles Schema-Setup erforderlich.

### Umschalten über das Admin-Dashboard

Sie können den Datenbankanbieter auch direkt über die Weboberfläche wechseln:

1. Gehen Sie zu **Admin > Einstellungen** — die **Datenbank**-Karte ist oben
2. Wählen Sie **PostgreSQL** aus dem Anbieter-Dropdown
3. Geben Sie Ihre PostgreSQL-Verbindungszeichenfolge ein (z.B. `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Klicken Sie auf **Datenbankeinstellungen speichern**
5. Starten Sie die Anwendung neu, damit die Änderung wirksam wird

Die Konfiguration wird in `~/.mypersonalgit/database.json` gespeichert (außerhalb der Datenbank selbst, damit sie vor dem Verbinden gelesen werden kann).

### Datenbank auswählen

| | SQLite | PostgreSQL |
|---|---|---|
| **Einrichtung** | Keine Konfiguration (Standard) | Erfordert einen PostgreSQL-Server |
| **Ideal für** | Persönlichen Gebrauch, kleine Teams, NAS | Teams ab 50+, hohe Nebenläufigkeit |
| **Backup** | `.db`-Datei kopieren | Standard `pg_dump` |
| **Nebenläufigkeit** | Single-Writer (ausreichend für die meisten Fälle) | Voller Multi-Writer-Betrieb |
| **Migration** | N/A | Anbieter wechseln + App starten (auto-migriert) |

## Deployment auf einem NAS

MyPersonalGit funktioniert hervorragend auf einem NAS (QNAP, Synology usw.) über Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Der Docker-Socket-Mount ist optional — wird nur benötigt, wenn Sie CI/CD-Workflow-Ausführung wünschen. Port 2222 wird nur benötigt, wenn Sie den integrierten SSH-Server aktivieren.

## Konfiguration

Alle Einstellungen können in `appsettings.json`, über Umgebungsvariablen oder über das Admin-Dashboard unter `/admin` konfiguriert werden:

- Datenbankanbieter (SQLite oder PostgreSQL)
- Projekt-Stammverzeichnis
- Authentifizierungsanforderungen
- Benutzerregistrierungs-Einstellungen
- Feature-Toggles (Issues, Wiki, Projekte, Actions)
- Maximale Repository-Größe und -Anzahl pro Benutzer
- SMTP-Einstellungen für E-Mail-Benachrichtigungen
- Push-Benachrichtigungs-Einstellungen (Ntfy/Gotify)
- Integrierter SSH-Server (aktivieren/deaktivieren, Port)
- LDAP/Active Directory-Authentifizierung (Server, Bind-DN, Such-Basis, Benutzerfilter, Attribut-Mapping, Admin-Gruppe)
- OAuth/SSO-Anbieterkonfiguration (Client-ID/Secret pro Anbieter)

## Projektstruktur

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor-Seiten (Home, RepoDetails, Issues, PRs, Packages usw.)
  Controllers/       # REST API-Endpunkte (NuGet, npm, Generic, Registry usw.)
  Data/              # EF Core DbContext, Service-Implementierungen
  Models/            # Domänenmodelle
  Migrations/        # EF Core-Migrationen
  Services/          # Middleware (Auth, Git HTTP-Backend, Pages, Registry-Auth)
    SshServer/       # Integrierter SSH-Server (SSH2-Protokoll, ECDH, AES-CTR)
  Program.cs         # App-Start, DI, Middleware-Pipeline
MyPersonalGit.Tests/
  UnitTest1.cs       # xUnit-Tests mit InMemory-Datenbank
```

## Tests ausführen

```bash
dotnet test
```

## Lizenz

MIT
