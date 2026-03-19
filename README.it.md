🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Un server Git self-hosted con un'interfaccia web simile a GitHub, costruito con ASP.NET Core e Blazor Server. Sfoglia repository, gestisci issue, pull request, wiki, progetti e altro ancora — tutto dalla tua macchina o dal tuo server.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## Sommario

- [Funzionalità](#funzionalità)
- [Stack Tecnologico](#stack-tecnologico)
- [Avvio Rapido](#avvio-rapido)
  - [Docker (Consigliato)](#docker-consigliato)
  - [Esecuzione Locale](#esecuzione-locale)
  - [Variabili d'Ambiente](#variabili-dambiente)
- [Utilizzo](#utilizzo)
  - [Accesso](#1-accesso)
  - [Creare un Repository](#2-creare-un-repository)
  - [Clonare e Pushare](#3-clonare-e-pushare)
  - [Clonare da un IDE](#4-clonare-da-un-ide)
  - [Editor Web](#5-utilizzare-leditor-web)
  - [Container Registry](#6-container-registry)
  - [Package Registry](#7-package-registry)
  - [Pages (Siti Statici)](#8-pages-hosting-di-siti-statici)
  - [Notifiche Push](#9-notifiche-push)
  - [Autenticazione con Chiave SSH](#10-autenticazione-con-chiave-ssh)
  - [LDAP / Active Directory](#11-autenticazione-ldap--active-directory)
  - [Repository Secrets](#12-repository-secrets)
  - [Login OAuth / SSO](#13-login-oauth--sso)
  - [Importare un Repository](#14-importare-un-repository)
  - [Forking e Sincronizzazione Upstream](#15-forking-e-sincronizzazione-upstream)
  - [CI/CD Auto-Release](#16-cicd-auto-release)
  - [Feed RSS/Atom](#17-feed-rssatom)
- [Configurazione del Database](#configurazione-del-database)
  - [Utilizzare PostgreSQL](#utilizzare-postgresql)
  - [Cambio dal Pannello Admin](#cambio-dal-pannello-admin)
  - [Scegliere un Database](#scegliere-un-database)
- [Deploy su un NAS](#deploy-su-un-nas)
- [Configurazione](#configurazione)
- [Struttura del Progetto](#struttura-del-progetto)
- [Eseguire i Test](#eseguire-i-test)
- [Licenza](#licenza)

---

## Funzionalità

### Codice e Repository
- **Gestione Repository** — Crea, sfoglia ed elimina repository Git con un browser del codice completo, editor di file, cronologia dei commit, branch e tag
- **Import/Migrazione Repository** — Importa repository da GitHub, GitLab, Bitbucket o qualsiasi URL Git con importazione opzionale di issue e PR. Elaborazione in background con monitoraggio del progresso
- **Archiviazione Repository** — Contrassegna i repository come di sola lettura con badge visivi; i push vengono bloccati per i repository archiviati
- **Git Smart HTTP** — Clona, fetch e push tramite HTTP con Basic Auth
- **Server SSH Integrato** — Server SSH nativo per operazioni Git — nessun OpenSSH esterno richiesto. Supporta scambio chiavi ECDH, crittografia AES-CTR e autenticazione a chiave pubblica (RSA, ECDSA, Ed25519)
- **Autenticazione con Chiave SSH** — Aggiungi chiavi pubbliche SSH al tuo account e autentica le operazioni Git tramite SSH con gestione automatica di `authorized_keys` (o con il server SSH integrato)
- **Fork e Sincronizzazione Upstream** — Forka repository, sincronizza i fork con l'upstream con un clic e visualizza le relazioni dei fork nell'interfaccia
- **Git LFS** — Supporto Large File Storage per il tracciamento di file binari
- **Mirroring dei Repository** — Specchia i repository verso/da remote Git esterni
- **Vista Comparazione** — Confronta branch con conteggio commit ahead/behind e rendering completo delle differenze
- **Statistiche Linguaggio** — Barra di distribuzione linguaggi in stile GitHub su ogni pagina repository
- **Branch Protection** — Regole configurabili per review obbligatorie, status check, prevenzione force-push e applicazione dell'approvazione CODEOWNERS
- **Tag Protection** — Proteggi i tag dalla cancellazione, aggiornamenti forzati e creazione non autorizzata con matching di pattern glob e liste di autorizzazione per utente
- **Verifica Firma dei Commit** — Verifica firma GPG sui commit e tag annotati con badge "Verified"/"Signed" nell'interfaccia
- **Label dei Repository** — Gestisci label con colori personalizzati per repository; le label vengono copiate automaticamente quando si creano repo da template
- **AGit Flow** — Workflow push-to-review: `git push origin HEAD:refs/for/main` crea una pull request senza forkare o creare branch remoti. Aggiorna PR aperte esistenti nei push successivi
- **Esplora** — Sfoglia tutti i repository accessibili con ricerca, ordinamento e filtro per topic
- **Ricerca** — Ricerca full-text su repository, issue, PR e codice

### Collaborazione
- **Issue e Pull Request** — Crea, commenta, chiudi/riapri issue e PR con label, assegnatari multipli, date di scadenza e review. Merge delle PR con strategie merge commit, squash o rebase. Risoluzione dei conflitti di merge basata su web con vista diff side-by-side
- **Dipendenze tra Issue** — Definisci relazioni "bloccato da" e "blocca" tra issue con rilevamento dipendenze circolari
- **Fissaggio e Blocco Issue** — Fissa issue importanti in cima alla lista e blocca le conversazioni per impedire ulteriori commenti
- **Modifica ed Eliminazione Commenti** — Modifica o elimina i tuoi commenti su issue e pull request con indicatore "(modificato)"
- **Risoluzione Conflitti di Merge** — Risolvi i conflitti di merge direttamente nel browser con un editor visuale che mostra viste base/ours/theirs, pulsanti di accettazione rapida e validazione dei marcatori di conflitto
- **Discussioni** — Conversazioni threaded in stile GitHub Discussions per repository con categorie (Generale, Q&A, Annunci, Idee, Mostra e Racconta, Sondaggi), fissa/blocca, segna come risposta e upvoting
- **Suggerimenti di Code Review** — La modalità "Suggerisci modifiche" nelle review inline delle PR permette ai revisori di proporre sostituzioni di codice direttamente nel diff
- **Emoji di Reazione** — Reagisci a issue, PR, discussioni e commenti con pollice su/giù, cuore, risata, evviva, confuso, razzo e occhi
- **CODEOWNERS** — Assegnazione automatica dei revisori PR basata sui percorsi dei file con applicazione opzionale che richiede l'approvazione CODEOWNERS prima del merge
- **Template di Repository** — Crea nuovi repository da template con copia automatica di file, label, template di issue e regole di branch protection
- **Bozze di Issue e Template di Issue** — Crea bozze di issue (work-in-progress) e definisci template di issue riutilizzabili (segnalazione bug, richiesta funzionalità) per repository con label predefinite
- **Wiki** — Pagine wiki basate su Markdown per repository con cronologia delle revisioni
- **Progetti** — Board Kanban con schede drag-and-drop per organizzare il lavoro
- **Snippet** — Condividi snippet di codice (come GitHub Gists) con evidenziazione della sintassi e file multipli
- **Organizzazioni e Team** — Crea organizzazioni con membri e team, assegna permessi team ai repository
- **Permessi Granulari** — Modello di permessi a cinque livelli (Lettura, Triage, Scrittura, Manutenzione, Admin) per un controllo degli accessi a grana fine sui repository
- **Milestone** — Monitora il progresso delle issue verso le milestone con barre di avanzamento e date di scadenza
- **Commenti ai Commit** — Commenta singoli commit con riferimenti opzionali a file/righe
- **Topic dei Repository** — Tagga i repository con topic per la scoperta e il filtraggio nella pagina Esplora

### CI/CD e DevOps
- **CI/CD Runner** — Definisci workflow in `.github/workflows/*.yml` ed eseguili in container Docker. Attivazione automatica su eventi push e pull request
- **Compatibilità GitHub Actions** — Lo stesso YAML del workflow funziona sia su MyPersonalGit che su GitHub Actions. Traduce le action `uses:` (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) in comandi shell equivalenti
- **Job Paralleli con `needs:`** — I job dichiarano dipendenze tramite `needs:` e vengono eseguiti in parallelo quando sono indipendenti. I job dipendenti attendono i prerequisiti e vengono automaticamente annullati se una dipendenza fallisce
- **Step Condizionali (`if:`)** — Gli step supportano espressioni `if:`: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Gli step di pulizia con `if: failure()` o `if: always()` vengono comunque eseguiti dopo errori precedenti
- **Output degli Step (`$GITHUB_OUTPUT`)** — Gli step possono scrivere coppie `key=value` o `key<<DELIMITER` multiriga in `$GITHUB_OUTPUT` e gli step successivi le ricevono come variabili d'ambiente, compatibili con la sintassi `${{ steps.X.outputs.Y }}`
- **Contesto `github`** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` e `CI=true` vengono automaticamente iniettati in ogni job
- **Build a Matrice** — `strategy.matrix` espande i job su combinazioni multiple di variabili (es. OS x versione). Supporta `fail-fast` e sostituzione `${{ matrix.X }}` in `runs-on`, comandi step e nomi step
- **Input `workflow_dispatch`** — Trigger manuali con parametri di input tipizzati (string, boolean, choice, number). L'interfaccia mostra un modulo di input quando si attivano manualmente workflow con input. I valori vengono iniettati come variabili d'ambiente `INPUT_*`
- **Timeout dei Job (`timeout-minutes`)** — Imposta `timeout-minutes` sui job per farli fallire automaticamente se superano il limite. Predefinito: 360 minuti (come GitHub Actions)
- **`if:` a Livello di Job** — Salta interi job basandosi su condizioni. I job con `if: always()` vengono eseguiti anche quando le dipendenze falliscono. I job saltati non fanno fallire l'esecuzione
- **Output dei Job** — I job dichiarano `outputs:` che i job `needs:` a valle consumano tramite `${{ needs.X.outputs.Y }}`. Gli output vengono risolti dagli output degli step dopo il completamento del job
- **`continue-on-error`** — Contrassegna singoli step come autorizzati a fallire senza far fallire il job. Utile per step di validazione o notifica opzionali
- **Filtro `on.push.paths`** — Attiva workflow solo quando cambiano file specifici. Supporta pattern glob (`src/**`, `*.ts`) e `paths-ignore:` per le esclusioni
- **Ri-esecuzione Workflow** — Ri-esegui workflow falliti, riusciti o annullati con un clic dall'interfaccia Actions. Crea una nuova esecuzione con la stessa configurazione
- **`working-directory`** — Imposta `defaults.run.working-directory` a livello di workflow o `working-directory:` per step per controllare dove vengono eseguiti i comandi
- **`defaults.run.shell`** — Configura una shell personalizzata per workflow o per step (`bash`, `sh`, `python3`, ecc.)
- **`strategy.max-parallel`** — Limita l'esecuzione concorrente dei job a matrice
- **`on.workflow_run`** — Concatena workflow: attiva il workflow B quando il workflow A è completato. Filtra per nome workflow e `types: [completed]`
- **Creazione Automatica di Release** — `softprops/action-gh-release` crea entità Release reali con tag, titolo, corpo changelog e flag pre-release/draft. Gli archivi del codice sorgente (ZIP e TAR.GZ) vengono automaticamente allegati come asset scaricabili
- **Pipeline Auto-Release** — Workflow integrato che tagga automaticamente le versioni, genera changelog e invia immagini Docker a Docker Hub ad ogni push su main
- **Commit Status Check** — I workflow impostano automaticamente lo stato pending/success/failure sui commit, visibili sulle pull request
- **Annullamento Workflow** — Annulla workflow in esecuzione o in coda dall'interfaccia Actions
- **Controlli di Concorrenza** — I nuovi push annullano automaticamente le esecuzioni in coda dello stesso workflow
- **Variabili d'Ambiente dei Workflow** — Imposta `env:` a livello di workflow, job o step in YAML
- **Badge di Stato** — Badge SVG incorporabili per lo stato dei workflow e dei commit (`/api/badge/{repo}/workflow`)
- **Download degli Artefatti** — Scarica gli artefatti di build direttamente dall'interfaccia Actions
- **Gestione dei Secrets** — Secret dei repository crittografati (AES-256) iniettati come variabili d'ambiente nelle esecuzioni dei workflow CI/CD
- **Webhooks** — Attiva servizi esterni su eventi del repository
- **Metriche Prometheus** — Endpoint `/metrics` integrato per il monitoraggio

### Hosting di Package e Container
- **Container Registry** — Ospita immagini Docker/OCI con `docker push` e `docker pull` (OCI Distribution Spec)
- **NuGet Registry** — Ospita pacchetti .NET con API NuGet v3 completa (service index, ricerca, push, restore)
- **npm Registry** — Ospita pacchetti Node.js con npm publish/install standard
- **PyPI Registry** — Ospita pacchetti Python con PEP 503 Simple API, JSON Metadata API e compatibilità `twine upload`
- **Maven Registry** — Ospita pacchetti Java/JVM con layout standard del repository Maven, generazione `maven-metadata.xml` e supporto `mvn deploy`
- **Pacchetti Generici** — Carica e scarica artefatti binari arbitrari tramite REST API

### Siti Statici
- **Pages** — Servi siti web statici direttamente da un branch del repository (come GitHub Pages) su `/pages/{owner}/{repo}/`

### Feed RSS/Atom
- **Feed dei Repository** — Feed Atom per commit, release e tag per repository (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Feed Attività Utente** — Feed attività per utente (`/api/feeds/users/{username}/activity.atom`)
- **Feed Attività Globale** — Feed attività dell'intero sito (`/api/feeds/global/activity.atom`)

### Notifiche
- **Notifiche In-App** — Menzioni, commenti e attività del repository
- **Notifiche Push** — Integrazione Ntfy e Gotify per avvisi mobile/desktop in tempo reale con opt-in per utente

### Autenticazione
- **OAuth2 / SSO** — Accedi con GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord o Twitter/X. Gli amministratori configurano Client ID e Secret per provider nel pannello Admin — solo i provider con credenziali compilate vengono mostrati agli utenti
- **Provider OAuth2** — Agisci come provider di identità affinché altre app possano usare "Accedi con MyPersonalGit". Implementa il flusso Authorization Code con PKCE, refresh dei token, endpoint userinfo e OpenID Connect Discovery (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Autentica gli utenti contro una directory LDAP o un dominio Active Directory. Gli utenti vengono auto-provisionati al primo accesso con attributi sincronizzati (email, nome visualizzato). Supporta promozione admin basata su gruppi, SSL/TLS e StartTLS
- **SSPI / Windows Integrated Auth** — Single Sign-On trasparente per utenti di dominio Windows tramite Negotiate/NTLM. Gli utenti in un dominio vengono autenticati automaticamente senza inserire credenziali. Attiva in Admin > Impostazioni (solo Windows)
- **Autenticazione a Due Fattori** — 2FA basato su TOTP con supporto app authenticator e codici di recupero
- **WebAuthn / Passkey** — Supporto chiavi di sicurezza hardware FIDO2 e passkey come secondo fattore. Registra YubiKey, authenticator della piattaforma (Face ID, Windows Hello, Touch ID) e altri dispositivi FIDO2. Verifica del conteggio firme per il rilevamento di chiavi clonate
- **Account Collegati** — Gli utenti possono collegare più provider OAuth al proprio account dalle Impostazioni

### Amministrazione
- **Pannello Admin** — Impostazioni di sistema (inclusi provider di database, server SSH, LDAP/AD, pagine footer), gestione utenti, log di audit e statistiche
- **Pagine Footer Personalizzabili** — Termini di servizio, Informativa sulla privacy, Documentazione e pagine Contatti con contenuto Markdown modificabile da Admin > Impostazioni
- **Profili Utente** — Heatmap dei contributi, feed attività e statistiche per utente
- **Token di Accesso Personale** — Autenticazione API basata su token con ambiti configurabili e restrizioni opzionali a livello di route (pattern glob come `/api/packages/**` per limitare l'accesso del token a percorsi API specifici)
- **Backup e Ripristino** — Esporta e importa dati del server
- **Scansione di Sicurezza** — Scansione reale delle vulnerabilità delle dipendenze basata sul database [OSV.dev](https://osv.dev/). Estrae automaticamente le dipendenze da `.csproj` (NuGet), `package.json` (npm) e `requirements.txt` (PyPI), quindi verifica ciascuna contro CVE noti. Riporta gravità, versioni corrette e link agli advisory. Più advisory di sicurezza manuali con workflow bozza/pubblicazione/chiusura
- **Dark Mode** — Supporto completo dark/light mode con toggle nell'intestazione
- **Multilingua / i18n** — Localizzazione completa su tutte le 27 pagine con 676 chiavi di risorsa. Viene fornito con 11 lingue: inglese, spagnolo, francese, tedesco, giapponese, coreano, cinese (semplificato), portoghese, russo, italiano e turco. Aggiungi altre lingue creando file `SharedResource.{locale}.resx`

## Stack Tecnologico

| Componente | Tecnologia |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (rendering interattivo lato server) |
| Database | SQLite (predefinito) o PostgreSQL tramite Entity Framework Core 10 |
| Git Engine | LibGit2Sharp |
| Auth | Hashing password BCrypt, autenticazione basata su sessione, token PAT, OAuth2 (8 provider + modalità provider), TOTP 2FA, WebAuthn/Passkey, LDAP/AD, SSPI |
| Server SSH | Implementazione protocollo SSH2 integrata (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Monitoraggio | Metriche Prometheus |

## Avvio Rapido

### Prerequisiti

- [Docker](https://docs.docker.com/get-docker/) (consigliato)
- Oppure [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git per lo sviluppo locale

### Docker (Consigliato)

Scarica da Docker Hub ed esegui:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> La porta 2222 è opzionale — necessaria solo se abiliti il server SSH integrato in Admin > Impostazioni.

Oppure usa Docker Compose:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

L'applicazione sarà disponibile su **http://localhost:8080**.

> **Credenziali predefinite**: `admin` / `admin`
>
> **Cambia la password predefinita immediatamente** tramite il pannello Admin dopo il primo accesso.

### Esecuzione Locale

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

L'applicazione si avvia su **http://localhost:5146**.

### Variabili d'Ambiente

| Variabile | Descrizione | Predefinito |
|----------|-------------|---------|
| `Database__Provider` | Motore database: `sqlite` o `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Stringa di connessione al database | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Directory dove vengono salvati i repository Git | `/repos` |
| `Git__RequireAuth` | Richiedi autenticazione per operazioni Git HTTP | `true` |
| `Git__Users__<username>` | Imposta la password per l'utente Git HTTP Basic Auth | — |
| `RESET_ADMIN_PASSWORD` | Reset d'emergenza della password admin all'avvio | — |
| `Secrets__EncryptionKey` | Chiave di crittografia personalizzata per i secret del repository | Derivata dalla stringa di connessione al DB |
| `Ssh__DataDir` | Directory per i dati SSH (chiavi host, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Percorso del file authorized_keys generato | `<DataDir>/authorized_keys` |

> **Nota:** La porta del server SSH integrato e le impostazioni LDAP vengono configurate tramite il pannello Admin (Admin > Impostazioni), non tramite variabili d'ambiente. Questo permette di modificarle senza ridistribuire.

## Utilizzo

### 1. Accesso

Apri l'applicazione e fai clic su **Accedi**. In una nuova installazione, usa le credenziali predefinite (`admin` / `admin`). Crea utenti aggiuntivi tramite il pannello **Admin** o abilitando la registrazione utenti in Admin > Impostazioni.

### 2. Creare un Repository

Fai clic sul pulsante verde **Nuovo** nella pagina iniziale, inserisci un nome e fai clic su **Crea**. Questo crea un repository Git bare sul server che puoi clonare, pushare e gestire tramite l'interfaccia web.

### 3. Clonare e Pushare

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Se l'autenticazione Git HTTP è abilitata, ti verranno richieste le credenziali configurate tramite le variabili d'ambiente `Git__Users__<username>`. Queste sono separate dal login dell'interfaccia web.

### 4. Clonare da un IDE

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > incolla `http://localhost:8080/git/MyRepo.git`

**Visual Studio**: **Git > Clona Repository** > incolla l'URL

**JetBrains**: **File > New > Project from Version Control** > incolla l'URL

### 5. Utilizzare l'Editor Web

Puoi modificare i file direttamente nel browser:
- Naviga in un repository e fai clic su qualsiasi file, poi fai clic su **Modifica**
- Usa **Aggiungi file > Crea nuovo file** per aggiungere file senza un clone locale
- Usa **Aggiungi file > Carica file/cartella** per caricare dal tuo computer

### 6. Container Registry

Pusha e pulla immagini Docker/OCI direttamente sul tuo server:

```bash
# Accedi (usa un Token di Accesso Personale da Impostazioni > Token di Accesso)
docker login localhost:8080 -u youruser

# Pusha un'immagine
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Pulla un'immagine
docker pull localhost:8080/myapp:v1
```

> **Nota:** Docker richiede HTTPS per impostazione predefinita. Per HTTP, aggiungi il tuo server a `insecure-registries` di Docker in `~/.docker/daemon.json`:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Package Registry

**NuGet (pacchetti .NET):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (pacchetti Node.js):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (pacchetti Python):**
```bash
# Installa un pacchetto
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# Carica con twine
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

**Maven (pacchetti Java/JVM):**
```xml
<!-- Nel tuo pom.xml, aggiungi il repository -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- In settings.xml, aggiungi le credenziali -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Generico (qualsiasi binario):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Sfoglia tutti i pacchetti su `/packages` nell'interfaccia web.

### 8. Pages (Hosting di Siti Statici)

Servi siti web statici da un branch del repository:

1. Vai alla scheda **Impostazioni** del tuo repository e abilita **Pages**
2. Imposta il branch (predefinito: `gh-pages`)
3. Pusha HTML/CSS/JS su quel branch
4. Visita `http://localhost:8080/pages/{username}/{repo}/`

### 9. Notifiche Push

Configura Ntfy o Gotify in **Admin > Impostazioni di Sistema** per ricevere notifiche push sul tuo telefono o desktop quando vengono creati issue, PR o commenti. Gli utenti possono attivare/disattivare in **Impostazioni > Notifiche**.

### 10. Autenticazione con Chiave SSH

Usa chiavi SSH per operazioni Git senza password. Ci sono due opzioni:

#### Opzione A: Server SSH Integrato (Consigliato)

Nessun demone SSH esterno richiesto — MyPersonalGit esegue il proprio server SSH:

1. Vai in **Admin > Impostazioni** e abilita il **Server SSH Integrato**
2. Imposta la porta SSH (predefinita: 2222) — usa 22 se non hai SSH di sistema in esecuzione
3. Salva le impostazioni e riavvia il server (le modifiche alla porta richiedono un riavvio)
4. Vai in **Impostazioni > Chiavi SSH** e aggiungi la tua chiave pubblica (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` o `~/.ssh/id_ecdsa.pub`)
5. Clona tramite SSH:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Il server SSH integrato supporta scambio chiavi ECDH-SHA2-NISTP256, crittografia AES-128/256-CTR, HMAC-SHA2-256 e autenticazione a chiave pubblica con chiavi Ed25519, RSA ed ECDSA.

#### Opzione B: OpenSSH di Sistema

Se preferisci usare il demone SSH del tuo sistema:

1. Vai in **Impostazioni > Chiavi SSH** e aggiungi la tua chiave pubblica
2. MyPersonalGit mantiene automaticamente un file `authorized_keys` da tutte le chiavi SSH registrate
3. Configura l'OpenSSH del tuo server per usare il file authorized_keys generato:
   ```
   # In /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Clona tramite SSH:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

Il servizio di autenticazione SSH espone anche un'API su `/api/ssh/authorized-keys` per l'uso con la direttiva `AuthorizedKeysCommand` di OpenSSH.

### 11. Autenticazione LDAP / Active Directory

Autentica gli utenti contro la directory LDAP o il dominio Active Directory della tua organizzazione:

1. Vai in **Admin > Impostazioni** e scorri fino a **Autenticazione LDAP / Active Directory**
2. Abilita LDAP e inserisci i dettagli del tuo server:
   - **Server**: Nome host del tuo server LDAP (es. `dc01.corp.local`)
   - **Porta**: 389 per LDAP, 636 per LDAPS
   - **SSL/TLS**: Abilita per LDAPS, o usa StartTLS per aggiornare una connessione in chiaro
3. Configura un account di servizio per la ricerca degli utenti:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Password Bind**: La password dell'account di servizio
4. Imposta i parametri di ricerca:
   - **DN Base di Ricerca**: `OU=Users,DC=corp,DC=local`
   - **Filtro Utente**: `(sAMAccountName={0})` per AD, `(uid={0})` per OpenLDAP
5. Mappa gli attributi LDAP ai campi utente:
   - **Nome utente**: `sAMAccountName` (AD) o `uid` (OpenLDAP)
   - **Email**: `mail`
   - **Nome Visualizzato**: `displayName`
6. Opzionalmente imposta un **DN Gruppo Admin** — i membri di questo gruppo vengono automaticamente promossi ad admin
7. Fai clic su **Testa Connessione LDAP** per verificare le impostazioni
8. Salva le impostazioni

Gli utenti possono ora accedere con le loro credenziali di dominio nella pagina di login. Al primo accesso, viene creato automaticamente un account locale con attributi sincronizzati dalla directory. L'autenticazione LDAP viene utilizzata anche per le operazioni Git HTTP (clone/push).

### 12. Repository Secrets

Aggiungi secret crittografati ai repository per l'uso nei workflow CI/CD:

1. Vai alla scheda **Impostazioni** del tuo repository
2. Scorri fino alla scheda **Secrets** e fai clic su **Aggiungi secret**
3. Inserisci un nome (es. `DEPLOY_TOKEN`) e un valore — il valore viene crittografato con AES-256
4. I secret vengono automaticamente iniettati come variabili d'ambiente in ogni esecuzione di workflow

Fai riferimento ai secret nel tuo workflow:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. Login OAuth / SSO

Accedi con provider di identità esterni:

1. Vai in **Admin > OAuth / SSO** e configura i provider che vuoi abilitare
2. Inserisci il **Client ID** e il **Client Secret** dalla console sviluppatori del provider
3. Spunta **Abilita** — solo i provider con entrambe le credenziali compilate appariranno nella pagina di login
4. L'URL di callback per ogni provider è mostrato nel pannello admin (es. `https://yourserver/oauth/callback/github`)

Provider supportati: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Gli utenti possono collegare più provider al proprio account in **Impostazioni > Account Collegati**.

### 14. Importare un Repository

Importa repository da fonti esterne con cronologia completa:

1. Fai clic su **Importa** nella pagina iniziale
2. Seleziona un tipo di fonte (URL Git, GitHub, GitLab o Bitbucket)
3. Inserisci l'URL del repository e opzionalmente un token di autenticazione per repo privati
4. Per importazioni da GitHub/GitLab/Bitbucket, importa opzionalmente issue e pull request
5. Monitora il progresso dell'importazione in tempo reale nella pagina Importa

### 15. Forking e Sincronizzazione Upstream

Forka un repository e mantienilo sincronizzato:

1. Fai clic sul pulsante **Fork** su qualsiasi pagina repository
2. Viene creato un fork sotto il tuo nome utente con un link all'originale
3. Fai clic su **Sincronizza fork** accanto al badge "forkato da" per ottenere le ultime modifiche dall'upstream

### 16. CI/CD Auto-Release

MyPersonalGit include una pipeline CI/CD integrata che tagga automaticamente, rilascia e pusha immagini Docker ad ogni push su main. I workflow si attivano automaticamente al push — nessun servizio CI esterno necessario.

**Come funziona:**
1. Il push su `main` attiva automaticamente `.github/workflows/release.yml`
2. Incrementa la versione patch (`v1.15.1` -> `v1.15.2`), crea un tag git
3. Accede a Docker Hub, costruisce l'immagine e la pusha come `:latest` e `:vX.Y.Z`

**Configurazione:**
1. Vai nelle **Impostazioni > Secrets** del tuo repo in MyPersonalGit
2. Aggiungi un secret chiamato `DOCKERHUB_TOKEN` con il tuo token di accesso Docker Hub
3. Assicurati che il container MyPersonalGit abbia il socket Docker montato (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Pusha su main — il workflow si attiva automaticamente

**Compatibilità GitHub Actions:**
Lo stesso YAML del workflow funziona anche su GitHub Actions — nessuna modifica necessaria. MyPersonalGit traduce le action `uses:` in comandi shell equivalenti a runtime:

| GitHub Action | Traduzione MyPersonalGit |
|---|---|
| `actions/checkout@v4` | Repo già clonato in `/workspace` |
| `actions/setup-dotnet@v4` | Installa .NET SDK tramite script di installazione ufficiale |
| `actions/setup-node@v4` | Installa Node.js tramite NodeSource |
| `actions/setup-python@v5` | Installa Python tramite apt/apk |
| `actions/setup-java@v4` | Installa OpenJDK tramite apt/apk |
| `docker/login-action@v3` | `docker login` con password tramite stdin |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op (usa il builder predefinito) |
| `softprops/action-gh-release@v2` | Crea un'entità Release reale nel database |
| `${{ secrets.X }}` | Variabile d'ambiente `$X` |
| `${{ steps.X.outputs.Y }}` | Variabile d'ambiente `$Y` |
| `${{ github.sha }}` | Variabile d'ambiente `$GITHUB_SHA` |

**Job paralleli:**
I job vengono eseguiti in parallelo per impostazione predefinita. Usa `needs:` per dichiarare le dipendenze:
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
I job senza `needs:` partono immediatamente. Un job viene annullato se una delle sue dipendenze fallisce.

**Step condizionali:**
Usa `if:` per controllare quando vengono eseguiti gli step:
```yaml
steps:
  - name: Build
    run: dotnet build

  - name: Notifica in caso di errore
    if: failure()
    run: curl -X POST https://hooks.example.com/alert

  - name: Pulizia
    if: always()
    run: rm -rf ./tmp
```
Espressioni supportate: `always()`, `success()` (predefinito), `failure()`, `cancelled()`, `true`, `false`.

**Output degli step:**
Gli step possono passare valori agli step successivi tramite `$GITHUB_OUTPUT`:
```yaml
steps:
  - name: Determina versione
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Usa versione
    run: echo "Building version $version"
```

**Build a matrice:**
Espandi i job su combinazioni multiple usando `strategy.matrix`:
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
Questo crea 4 job: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)`, ecc. Tutti vengono eseguiti in parallelo.

**Trigger manuali con input (`workflow_dispatch`):**
Definisci input tipizzati che appaiono come modulo nell'interfaccia quando si attiva manualmente:
```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Ambiente di destinazione"
        required: true
        type: choice
        options:
          - staging
          - production
      debug:
        description: "Abilita modalità debug"
        type: boolean
        default: "false"

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying to $INPUT_ENVIRONMENT (debug=$INPUT_DEBUG)"
```
I valori di input vengono iniettati come variabili d'ambiente `INPUT_<NOME>` (maiuscolo).

**Timeout dei job:**
Imposta `timeout-minutes` sui job per farli fallire automaticamente se durano troppo:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Il timeout predefinito è 360 minuti (6 ore), come GitHub Actions.

**Condizionali a livello di job:**
Usa `if:` sui job per saltarli in base a condizioni:
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

**Output dei job:**
I job possono passare valori ai job a valle tramite `outputs:`:
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

**Continua in caso di errore:**
Permetti a uno step di fallire senza far fallire il job:
```yaml
steps:
  - name: Lint opzionale
    continue-on-error: true
    run: npm run lint

  - name: Build (viene sempre eseguito)
    run: npm run build
```

**Filtro per percorso:**
Attiva workflow solo quando cambiano file specifici:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # oppure usa paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Directory di lavoro:**
Imposta dove vengono eseguiti i comandi:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # eseguito in src/app
      - run: npm test
        working-directory: tests  # sovrascrive il predefinito
```

**Ri-esecuzione workflow:**
Fai clic sul pulsante **Ri-esegui** su qualsiasi esecuzione di workflow completata, fallita o annullata per creare una nuova esecuzione con gli stessi job, step e configurazione.

**Workflow di Pull Request:**
I workflow con `on: pull_request` si attivano automaticamente quando viene creata una PR non-bozza, eseguendo i check contro il branch sorgente.

**Commit status check:**
I workflow impostano automaticamente gli stati dei commit (pending/success/failure) in modo da poter vedere i risultati del build sulle PR e imporre check obbligatori tramite branch protection.

**Annullamento workflow:**
Fai clic sul pulsante **Annulla** su qualsiasi workflow in esecuzione o in coda nell'interfaccia Actions per interromperlo immediatamente.

**Badge di stato:**
Incorpora badge dello stato del build nel tuo README o ovunque:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Filtra per nome workflow: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. Feed RSS/Atom

Iscriviti all'attività del repository usando feed Atom standard in qualsiasi lettore RSS:

```
# Commit del repository
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Release del repository
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Tag del repository
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Attività utente
http://localhost:8080/api/feeds/users/admin/activity.atom

# Attività globale (tutti i repository)
http://localhost:8080/api/feeds/global/activity.atom
```

Nessuna autenticazione richiesta per i repository pubblici. Aggiungi questi URL a qualsiasi lettore di feed (Feedly, Miniflux, FreshRSS, ecc.) per rimanere aggiornato sulle modifiche.

## Configurazione del Database

MyPersonalGit usa **SQLite** per impostazione predefinita — nessuna configurazione necessaria, database a file singolo, perfetto per uso personale e piccoli team.

Per deployment più grandi (molti utenti concorrenti, alta disponibilità, o se usi già PostgreSQL), puoi passare a **PostgreSQL**:

### Utilizzare PostgreSQL

**Docker Compose** (consigliato per PostgreSQL):
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

**Solo variabili d'ambiente** (se hai già un server PostgreSQL):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

Le migrazioni EF Core vengono eseguite automaticamente all'avvio per entrambi i provider. Nessuna configurazione manuale dello schema necessaria.

### Cambio dal Pannello Admin

Puoi anche cambiare provider di database direttamente dall'interfaccia web:

1. Vai in **Admin > Impostazioni** — la scheda **Database** è in alto
2. Seleziona **PostgreSQL** dal menu a tendina del provider
3. Inserisci la tua stringa di connessione PostgreSQL (es. `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Fai clic su **Salva Impostazioni Database**
5. Riavvia l'applicazione affinché la modifica abbia effetto

La configurazione viene salvata in `~/.mypersonalgit/database.json` (fuori dal database stesso, in modo da poter essere letta prima della connessione).

### Scegliere un Database

| | SQLite | PostgreSQL |
|---|---|---|
| **Configurazione** | Nessuna configurazione (predefinito) | Richiede un server PostgreSQL |
| **Ideale per** | Uso personale, piccoli team, NAS | Team di 50+, alta concorrenza |
| **Backup** | Copia il file `.db` | `pg_dump` standard |
| **Concorrenza** | Singolo scrittore (adeguato per la maggior parte degli usi) | Multi-scrittore completo |
| **Migrazione** | N/A | Cambia provider + avvia l'app (migra automaticamente) |

## Deploy su un NAS

MyPersonalGit funziona benissimo su un NAS (QNAP, Synology, ecc.) tramite Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Il mount del socket Docker è opzionale — necessario solo se vuoi l'esecuzione dei workflow CI/CD. La porta 2222 è necessaria solo se abiliti il server SSH integrato.

## Configurazione

Tutte le impostazioni possono essere configurate in `appsettings.json`, tramite variabili d'ambiente, o attraverso il pannello Admin su `/admin`:

- Provider di database (SQLite o PostgreSQL)
- Directory root del progetto
- Requisiti di autenticazione
- Impostazioni di registrazione utenti
- Toggle delle funzionalità (Issues, Wiki, Progetti, Actions)
- Dimensione massima del repository e numero per utente
- Impostazioni SMTP per notifiche email
- Impostazioni notifiche push (Ntfy/Gotify)
- Server SSH integrato (abilita/disabilita, porta)
- Autenticazione LDAP/Active Directory (server, bind DN, base di ricerca, filtro utente, mappatura attributi, gruppo admin)
- Configurazione provider OAuth/SSO (Client ID/Secret per provider)

## Struttura del Progetto

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Pagine Blazor (Home, RepoDetails, Issues, PRs, Packages, ecc.)
  Controllers/       # Endpoint REST API (NuGet, npm, Generic, Registry, ecc.)
  Data/              # EF Core DbContext, implementazioni dei servizi
  Models/            # Modelli di dominio
  Migrations/        # Migrazioni EF Core
  Services/          # Middleware (auth, Git HTTP backend, Pages, Registry auth)
    SshServer/       # Server SSH integrato (protocollo SSH2, ECDH, AES-CTR)
  Program.cs         # Avvio dell'app, DI, pipeline middleware
MyPersonalGit.Tests/
  UnitTest1.cs       # Test xUnit con database InMemory
```

## Eseguire i Test

```bash
dotnet test
```

## Licenza

MIT
