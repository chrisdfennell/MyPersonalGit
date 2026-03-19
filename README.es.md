🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Un servidor Git autoalojado con una interfaz web similar a GitHub, construido con ASP.NET Core y Blazor Server. Navega por repositorios, gestiona incidencias, pull requests, wikis, proyectos y mucho mas — todo desde tu propia maquina o servidor.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## Tabla de Contenidos

- [Funcionalidades](#funcionalidades)
- [Stack Tecnologico](#stack-tecnologico)
- [Inicio Rapido](#inicio-rapido)
  - [Docker (Recomendado)](#docker-recomendado)
  - [Ejecutar Localmente](#ejecutar-localmente)
  - [Variables de Entorno](#variables-de-entorno)
- [Uso](#uso)
  - [Iniciar Sesion](#1-iniciar-sesion)
  - [Crear un Repositorio](#2-crear-un-repositorio)
  - [Clonar y Hacer Push](#3-clonar-y-hacer-push)
  - [Clonar desde un IDE](#4-clonar-desde-un-ide)
  - [Editor Web](#5-usar-el-editor-web)
  - [Registro de Contenedores](#6-registro-de-contenedores)
  - [Registro de Paquetes](#7-registro-de-paquetes)
  - [Pages (Sitios Estaticos)](#8-pages-alojamiento-de-sitios-estaticos)
  - [Notificaciones Push](#9-notificaciones-push)
  - [Autenticacion con Clave SSH](#10-autenticacion-con-clave-ssh)
  - [LDAP / Active Directory](#11-autenticacion-ldap--active-directory)
  - [Secretos del Repositorio](#12-secretos-del-repositorio)
  - [Inicio de Sesion OAuth / SSO](#13-inicio-de-sesion-oauth--sso)
  - [Importar Repositorio](#14-importar-repositorio)
  - [Forks y Sincronizacion Upstream](#15-forks-y-sincronizacion-upstream)
  - [Auto-Release CI/CD](#16-auto-release-cicd)
  - [Feeds RSS/Atom](#17-feeds-rssatom)
- [Configuracion de Base de Datos](#configuracion-de-base-de-datos)
  - [Usar PostgreSQL](#usar-postgresql)
  - [Cambiar desde el Panel de Administracion](#cambiar-desde-el-panel-de-administracion)
  - [Elegir una Base de Datos](#elegir-una-base-de-datos)
- [Desplegar en un NAS](#desplegar-en-un-nas)
- [Configuracion](#configuracion)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Ejecutar Tests](#ejecutar-tests)
- [Licencia](#licencia)

---

## Funcionalidades

### Codigo y Repositorios
- **Gestion de Repositorios** — Crea, navega y elimina repositorios Git con un explorador de codigo completo, editor de archivos, historial de commits, ramas y etiquetas
- **Importacion/Migracion de Repositorios** — Importa repositorios desde GitHub, GitLab, Bitbucket o cualquier URL Git con importacion opcional de incidencias y PRs. Procesamiento en segundo plano con seguimiento de progreso
- **Archivado de Repositorios** — Marca repositorios como solo lectura con insignias visuales; los pushes se bloquean en repositorios archivados
- **Git Smart HTTP** — Clona, fetch y push a traves de HTTP con Basic Auth
- **Servidor SSH Integrado** — Servidor SSH nativo para operaciones Git — no requiere OpenSSH externo. Soporta intercambio de claves ECDH, cifrado AES-CTR y autenticacion con clave publica (RSA, ECDSA, Ed25519)
- **Autenticacion con Clave SSH** — Agrega claves publicas SSH a tu cuenta y autentica operaciones Git via SSH con gestion automatica de `authorized_keys` (o el servidor SSH integrado)
- **Forks y Sincronizacion Upstream** — Haz fork de repositorios, sincroniza forks con upstream con un clic y visualiza las relaciones de forks en la interfaz
- **Git LFS** — Soporte de Large File Storage para el seguimiento de archivos binarios
- **Espejo de Repositorios** — Refleja repositorios hacia/desde remotos Git externos
- **Vista de Comparacion** — Compara ramas con conteo de commits adelante/atras y renderizado completo de diffs
- **Estadisticas de Lenguaje** — Barra de desglose de lenguajes al estilo GitHub en cada pagina de repositorio
- **Proteccion de Ramas** — Reglas configurables para revisiones requeridas, verificaciones de estado, prevencion de force-push y aplicacion de aprobacion de CODEOWNERS
- **Proteccion de Etiquetas** — Protege etiquetas contra eliminacion, actualizaciones forzadas y creacion no autorizada con coincidencia de patrones glob y listas de permitidos por usuario
- **Verificacion de Firmas de Commits** — Verificacion de firmas GPG en commits y etiquetas anotadas con insignias "Verified" / "Signed" en la interfaz
- **Etiquetas de Repositorio** — Gestiona etiquetas con colores personalizados por repositorio; las etiquetas se copian automaticamente al crear repositorios a partir de plantillas
- **Flujo AGit** — Flujo de trabajo push-to-review: `git push origin HEAD:refs/for/main` crea un pull request sin necesidad de hacer fork ni crear ramas remotas. Actualiza PRs abiertos existentes en pushes posteriores
- **Explorar** — Navega por todos los repositorios accesibles con busqueda, ordenamiento y filtrado por temas
- **Busqueda** — Busqueda de texto completo en repositorios, incidencias, PRs y codigo

### Colaboracion
- **Incidencias y Pull Requests** — Crea, comenta, cierra/reabre incidencias y PRs con etiquetas, multiples asignados, fechas limite y revisiones. Fusiona PRs con estrategias de merge commit, squash o rebase. Resolucion de conflictos de merge basada en web con vista de diff lado a lado
- **Dependencias de Incidencias** — Define relaciones "bloqueado por" y "bloquea" entre incidencias con deteccion de dependencias circulares
- **Fijado y Bloqueo de Incidencias** — Fija incidencias importantes en la parte superior de la lista y bloquea conversaciones para evitar mas comentarios
- **Edicion y Eliminacion de Comentarios** — Edita o elimina tus propios comentarios en incidencias y pull requests con indicador "(editado)"
- **Resolucion de Conflictos de Merge** — Resuelve conflictos de merge directamente en el navegador con un editor visual que muestra vistas base/nuestro/suyo, botones de aceptacion rapida y validacion de marcadores de conflicto
- **Discusiones** — Conversaciones encadenadas al estilo GitHub Discussions por repositorio con categorias (General, Preguntas y Respuestas, Anuncios, Ideas, Mostrar y Contar, Encuestas), fijar/bloquear, marcar como respuesta y votos positivos
- **Sugerencias de Revision de Codigo** — El modo "Sugerir cambios" en revisiones en linea de PR permite a los revisores proponer reemplazos de codigo directamente en el diff
- **Emojis de Reaccion** — Reacciona a incidencias, PRs, discusiones y comentarios con pulgar arriba/abajo, corazon, risa, hurra, confundido, cohete y ojos
- **CODEOWNERS** — Asignacion automatica de revisores de PR basada en rutas de archivos con aplicacion opcional que requiere aprobacion de CODEOWNERS antes de fusionar
- **Plantillas de Repositorio** — Crea nuevos repositorios a partir de plantillas con copia automatica de archivos, etiquetas, plantillas de incidencias y reglas de proteccion de ramas
- **Incidencias Borrador y Plantillas de Incidencias** — Crea incidencias borrador (trabajo en progreso) y define plantillas de incidencias reutilizables (reporte de error, solicitud de funcionalidad) por repositorio con etiquetas predeterminadas
- **Wiki** — Paginas wiki basadas en Markdown por repositorio con historial de revisiones
- **Proyectos** — Tableros Kanban con tarjetas arrastrables para organizar el trabajo
- **Snippets** — Comparte fragmentos de codigo (como GitHub Gists) con resaltado de sintaxis y multiples archivos
- **Organizaciones y Equipos** — Crea organizaciones con miembros y equipos, asigna permisos de equipo a repositorios
- **Permisos Granulares** — Modelo de permisos de cinco niveles (Lectura, Triaje, Escritura, Mantenimiento, Administrador) para control de acceso detallado en repositorios
- **Hitos** — Rastrea el progreso de incidencias hacia hitos con barras de progreso y fechas limite
- **Comentarios de Commits** — Comenta en commits individuales con referencias opcionales a archivo/linea
- **Temas de Repositorio** — Etiqueta repositorios con temas para descubrimiento y filtrado en la pagina de Explorar

### CI/CD y DevOps
- **Ejecutor CI/CD** — Define flujos de trabajo en `.github/workflows/*.yml` y ejecutalos en contenedores Docker. Se activa automaticamente en eventos de push y pull request
- **Compatibilidad con GitHub Actions** — El mismo YAML de flujo de trabajo funciona tanto en MyPersonalGit como en GitHub Actions. Traduce acciones `uses:` (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) en comandos shell equivalentes
- **Jobs Paralelos con `needs:`** — Los jobs declaran dependencias via `needs:` y se ejecutan en paralelo cuando son independientes. Los jobs dependientes esperan a sus prerrequisitos y se cancelan automaticamente si una dependencia falla
- **Pasos Condicionales (`if:`)** — Los pasos soportan expresiones `if:`: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Los pasos de limpieza con `if: failure()` o `if: always()` se ejecutan incluso despues de fallos anteriores
- **Salidas de Pasos (`$GITHUB_OUTPUT`)** — Los pasos pueden escribir pares `key=value` o `key<<DELIMITER` multilinea en `$GITHUB_OUTPUT` y los pasos siguientes los reciben como variables de entorno, compatible con la sintaxis `${{ steps.X.outputs.Y }}`
- **Contexto `github`** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` y `CI=true` se inyectan automaticamente en cada job
- **Builds de Matriz** — `strategy.matrix` expande jobs a traves de multiples combinaciones de variables (ej., SO x version). Soporta `fail-fast` y sustitucion `${{ matrix.X }}` en `runs-on`, comandos de pasos y nombres de pasos
- **Entradas `workflow_dispatch`** — Disparadores manuales con parametros de entrada tipados (string, boolean, choice, number). La interfaz muestra un formulario de entrada al disparar flujos de trabajo con entradas. Los valores se inyectan como variables de entorno `INPUT_*`
- **Timeouts de Jobs (`timeout-minutes`)** — Establece `timeout-minutes` en jobs para fallarlos automaticamente si exceden el limite. Por defecto: 360 minutos (igual que GitHub Actions)
- **`if:` a Nivel de Job** — Omite jobs completos basandose en condiciones. Los jobs con `if: always()` se ejecutan incluso cuando las dependencias fallan. Los jobs omitidos no fallan la ejecucion
- **Salidas de Jobs** — Los jobs declaran `outputs:` que los jobs dependientes con `needs:` consumen via `${{ needs.X.outputs.Y }}`. Las salidas se resuelven desde las salidas de pasos despues de que el job se completa
- **`continue-on-error`** — Marca pasos individuales como permitidos para fallar sin fallar el job. Util para pasos opcionales de validacion o notificacion
- **Filtro `on.push.paths`** — Solo activa flujos de trabajo cuando cambian archivos especificos. Soporta patrones glob (`src/**`, `*.ts`) y `paths-ignore:` para exclusiones
- **Re-ejecutar Flujos de Trabajo** — Re-ejecuta ejecuciones de flujos de trabajo fallidas, exitosas o canceladas con un clic desde la interfaz de Actions. Crea una ejecucion nueva con la misma configuracion
- **`working-directory`** — Establece `defaults.run.working-directory` a nivel de flujo de trabajo o `working-directory:` por paso para controlar donde se ejecutan los comandos
- **`defaults.run.shell`** — Configura un shell personalizado por flujo de trabajo o por paso (`bash`, `sh`, `python3`, etc.)
- **`strategy.max-parallel`** — Limita la ejecucion concurrente de jobs de matriz
- **`on.workflow_run`** — Encadena flujos de trabajo: activa el flujo de trabajo B cuando el flujo de trabajo A se completa. Filtra por nombre de flujo de trabajo y `types: [completed]`
- **Creacion Automatica de Releases** — `softprops/action-gh-release` crea entidades Release reales con etiqueta, titulo, cuerpo de changelog y flags de pre-release/borrador. Los archivos de codigo fuente (ZIP y TAR.GZ) se adjuntan automaticamente como activos descargables
- **Pipeline de Auto-Release** — Flujo de trabajo integrado que auto-etiqueta versiones, genera changelogs y publica imagenes Docker en Docker Hub en cada push a main
- **Verificaciones de Estado de Commits** — Los flujos de trabajo establecen automaticamente estados pendiente/exito/fallo en commits, visibles en pull requests
- **Cancelacion de Flujos de Trabajo** — Cancela flujos de trabajo en ejecucion o en cola desde la interfaz de Actions
- **Controles de Concurrencia** — Los nuevos pushes cancelan automaticamente las ejecuciones en cola del mismo flujo de trabajo
- **Variables de Entorno de Flujos de Trabajo** — Establece `env:` a nivel de flujo de trabajo, job o paso en YAML
- **Insignias de Estado** — Insignias SVG incrustables para estado de flujo de trabajo y commit (`/api/badge/{repo}/workflow`)
- **Descarga de Artefactos** — Descarga artefactos de compilacion directamente desde la interfaz de Actions
- **Gestion de Secretos** — Secretos de repositorio cifrados (AES-256) inyectados como variables de entorno en ejecuciones de flujos de trabajo CI/CD
- **Webhooks** — Activa servicios externos en eventos del repositorio
- **Metricas Prometheus** — Endpoint `/metrics` integrado para monitoreo

### Alojamiento de Paquetes y Contenedores
- **Registro de Contenedores** — Aloja imagenes Docker/OCI con `docker push` y `docker pull` (OCI Distribution Spec)
- **Registro NuGet** — Aloja paquetes .NET con API NuGet v3 completa (indice de servicios, busqueda, push, restauracion)
- **Registro npm** — Aloja paquetes Node.js con publicacion/instalacion npm estandar
- **Registro PyPI** — Aloja paquetes Python con PEP 503 Simple API, JSON metadata API y compatibilidad con `twine upload`
- **Registro Maven** — Aloja paquetes Java/JVM con disposicion de repositorio Maven estandar, generacion de `maven-metadata.xml` y soporte de `mvn deploy`
- **Paquetes Genericos** — Sube y descarga artefactos binarios arbitrarios via REST API

### Sitios Estaticos
- **Pages** — Sirve sitios web estaticos directamente desde una rama del repositorio (como GitHub Pages) en `/pages/{owner}/{repo}/`

### Feeds RSS/Atom
- **Feeds de Repositorio** — Feeds Atom para commits, releases y etiquetas por repositorio (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Feed de Actividad de Usuario** — Feed de actividad por usuario (`/api/feeds/users/{username}/activity.atom`)
- **Feed de Actividad Global** — Feed de actividad de todo el sitio (`/api/feeds/global/activity.atom`)

### Notificaciones
- **Notificaciones en la Aplicacion** — Menciones, comentarios y actividad del repositorio
- **Notificaciones Push** — Integracion con Ntfy y Gotify para alertas en tiempo real en moviles/escritorio con opcion de activacion por usuario

### Autenticacion
- **OAuth2 / SSO** — Inicia sesion con GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord o Twitter/X. Los administradores configuran Client ID y Secret por proveedor en el panel de Administracion — solo los proveedores con credenciales completas se muestran a los usuarios
- **Proveedor OAuth2** — Actua como proveedor de identidad para que otras aplicaciones puedan usar "Iniciar sesion con MyPersonalGit". Implementa flujo de Authorization Code con PKCE, actualizacion de tokens, endpoint userinfo y descubrimiento OpenID Connect (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Autentica usuarios contra un directorio LDAP o dominio Active Directory. Los usuarios se aprovisionan automaticamente en el primer inicio de sesion con atributos sincronizados (correo electronico, nombre para mostrar). Soporta promocion a administrador basada en grupos, SSL/TLS y StartTLS
- **SSPI / Autenticacion Integrada de Windows** — Inicio de sesion unico transparente para usuarios de dominio Windows via Negotiate/NTLM. Los usuarios en un dominio se autentican automaticamente sin ingresar credenciales. Habilitalo en Admin > Settings (solo Windows)
- **Autenticacion de Dos Factores** — 2FA basada en TOTP con soporte de aplicacion autenticadora y codigos de recuperacion
- **WebAuthn / Passkeys** — Soporte de llaves de seguridad de hardware FIDO2 y passkeys como segundo factor. Registra YubiKeys, autenticadores de plataforma (Face ID, Windows Hello, Touch ID) y otros dispositivos FIDO2. Verificacion de conteo de firmas para deteccion de claves clonadas
- **Cuentas Vinculadas** — Los usuarios pueden vincular multiples proveedores OAuth a su cuenta desde Configuracion

### Administracion
- **Panel de Administracion** — Configuracion del sistema (incluyendo proveedor de base de datos, servidor SSH, LDAP/AD, paginas de pie de pagina), gestion de usuarios, registros de auditoria y estadisticas
- **Paginas de Pie de Pagina Personalizables** — Terminos de Servicio, Politica de Privacidad, Documentacion y paginas de Contacto con contenido Markdown editable desde Admin > Settings
- **Perfiles de Usuario** — Mapa de calor de contribuciones, feed de actividad y estadisticas por usuario
- **Tokens de Acceso Personal** — Autenticacion API basada en tokens con alcances configurables y restricciones opcionales a nivel de ruta (patrones glob como `/api/packages/**` para limitar el acceso del token a rutas API especificas)
- **Respaldo y Restauracion** — Exporta e importa datos del servidor
- **Escaneo de Seguridad** — Escaneo real de vulnerabilidades en dependencias impulsado por la base de datos [OSV.dev](https://osv.dev/). Extrae automaticamente dependencias de `.csproj` (NuGet), `package.json` (npm) y `requirements.txt` (PyPI), luego verifica cada una contra CVEs conocidos. Reporta severidad, versiones corregidas y enlaces a avisos. Ademas, avisos de seguridad manuales con flujo de trabajo borrador/publicar/cerrar
- **Modo Oscuro** — Soporte completo de modo oscuro/claro con un interruptor en el encabezado
- **Multi-Idioma / i18n** — Localizacion completa en las 27 paginas con 676 claves de recursos. Se distribuye con 11 idiomas: ingles, espanol, frances, aleman, japones, coreano, chino (simplificado), portugues, ruso, italiano y turco. Agrega mas creando archivos `SharedResource.{locale}.resx`

## Stack Tecnologico

| Componente | Tecnologia |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (renderizado interactivo del lado del servidor) |
| Base de Datos | SQLite (por defecto) o PostgreSQL via Entity Framework Core 10 |
| Motor Git | LibGit2Sharp |
| Autenticacion | Hashing de contrasenas BCrypt, autenticacion basada en sesiones, tokens PAT, OAuth2 (8 proveedores + modo proveedor), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| Servidor SSH | Implementacion integrada del protocolo SSH2 (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Monitoreo | Metricas Prometheus |

## Inicio Rapido

### Prerrequisitos

- [Docker](https://docs.docker.com/get-docker/) (recomendado)
- O [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git para desarrollo local

### Docker (Recomendado)

Descarga desde Docker Hub y ejecuta:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> El puerto 2222 es opcional — solo es necesario si habilitas el servidor SSH integrado en Admin > Settings.

O usa Docker Compose:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

La aplicacion estara disponible en **http://localhost:8080**.

> **Credenciales por defecto**: `admin` / `admin`
>
> **Cambia la contrasena por defecto inmediatamente** a traves del panel de Administracion despues del primer inicio de sesion.

### Ejecutar Localmente

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

La aplicacion se inicia en **http://localhost:5146**.

### Variables de Entorno

| Variable | Descripcion | Por Defecto |
|----------|-------------|---------|
| `Database__Provider` | Motor de base de datos: `sqlite` o `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Cadena de conexion a la base de datos | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Directorio donde se almacenan los repositorios Git | `/repos` |
| `Git__RequireAuth` | Requerir autenticacion para operaciones Git HTTP | `true` |
| `Git__Users__<username>` | Establecer contrasena para usuario de Git HTTP Basic Auth | — |
| `RESET_ADMIN_PASSWORD` | Restablecimiento de emergencia de la contrasena de administrador al iniciar | — |
| `Secrets__EncryptionKey` | Clave de cifrado personalizada para secretos del repositorio | Derivada de la cadena de conexion de la BD |
| `Ssh__DataDir` | Directorio para datos SSH (claves de host, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Ruta al archivo authorized_keys generado | `<DataDir>/authorized_keys` |

> **Nota:** El puerto del servidor SSH integrado y la configuracion LDAP se configuran a traves del panel de Administracion (Admin > Settings), no mediante variables de entorno. Esto te permite cambiarlos sin redesplegar.

## Uso

### 1. Iniciar Sesion

Abre la aplicacion y haz clic en **Sign In**. En una instalacion nueva, usa las credenciales por defecto (`admin` / `admin`). Crea usuarios adicionales a traves del panel de **Admin** o habilitando el registro de usuarios en Admin > Settings.

### 2. Crear un Repositorio

Haz clic en el boton verde **New** en la pagina principal, ingresa un nombre y haz clic en **Create**. Esto crea un repositorio Git bare en el servidor que puedes clonar, hacer push y gestionar a traves de la interfaz web.

### 3. Clonar y Hacer Push

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Si la autenticacion Git HTTP esta habilitada, se te pediran las credenciales configuradas mediante las variables de entorno `Git__Users__<username>`. Estas son independientes del inicio de sesion de la interfaz web.

### 4. Clonar desde un IDE

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > pega `http://localhost:8080/git/MyRepo.git`

**Visual Studio**: **Git > Clone Repository** > pega la URL

**JetBrains**: **File > New > Project from Version Control** > pega la URL

### 5. Usar el Editor Web

Puedes editar archivos directamente en el navegador:
- Navega a un repositorio y haz clic en cualquier archivo, luego haz clic en **Edit**
- Usa **Add files > Create new file** para agregar archivos sin un clon local
- Usa **Add files > Upload files/folder** para subir desde tu maquina

### 6. Registro de Contenedores

Sube y descarga imagenes Docker/OCI directamente a tu servidor:

```bash
# Iniciar sesion (usa un Token de Acceso Personal desde Settings > Access Tokens)
docker login localhost:8080 -u youruser

# Subir una imagen
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Descargar una imagen
docker pull localhost:8080/myapp:v1
```

> **Nota:** Docker requiere HTTPS por defecto. Para HTTP, agrega tu servidor a los `insecure-registries` de Docker en `~/.docker/daemon.json`:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Registro de Paquetes

**NuGet (paquetes .NET):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (paquetes Node.js):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (paquetes Python):**
```bash
# Instalar un paquete
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# Subir con twine
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

**Maven (paquetes Java/JVM):**
```xml
<!-- En tu pom.xml, agrega el repositorio -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- En settings.xml, agrega las credenciales -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Generico (cualquier binario):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Explora todos los paquetes en `/packages` en la interfaz web.

### 8. Pages (Alojamiento de Sitios Estaticos)

Sirve sitios web estaticos desde una rama del repositorio:

1. Ve a la pestana **Settings** de tu repositorio y habilita **Pages**
2. Establece la rama (por defecto: `gh-pages`)
3. Sube HTML/CSS/JS a esa rama
4. Visita `http://localhost:8080/pages/{username}/{repo}/`

### 9. Notificaciones Push

Configura Ntfy o Gotify en **Admin > System Settings** para recibir notificaciones push en tu telefono o escritorio cuando se creen incidencias, PRs o comentarios. Los usuarios pueden activar/desactivar en **Settings > Notifications**.

### 10. Autenticacion con Clave SSH

Usa claves SSH para operaciones Git sin contrasena. Hay dos opciones:

#### Opcion A: Servidor SSH Integrado (Recomendado)

No requiere demonio SSH externo — MyPersonalGit ejecuta su propio servidor SSH:

1. Ve a **Admin > Settings** y habilita **Built-in SSH Server**
2. Establece el puerto SSH (por defecto: 2222) — usa 22 si no estas ejecutando SSH del sistema
3. Guarda la configuracion y reinicia el servidor (los cambios de puerto requieren reinicio)
4. Ve a **Settings > SSH Keys** y agrega tu clave publica (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` o `~/.ssh/id_ecdsa.pub`)
5. Clona via SSH:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

El servidor SSH integrado soporta intercambio de claves ECDH-SHA2-NISTP256, cifrado AES-128/256-CTR, HMAC-SHA2-256 y autenticacion con clave publica con claves Ed25519, RSA y ECDSA.

#### Opcion B: OpenSSH del Sistema

Si prefieres usar el demonio SSH de tu sistema:

1. Ve a **Settings > SSH Keys** y agrega tu clave publica
2. MyPersonalGit mantiene automaticamente un archivo `authorized_keys` con todas las claves SSH registradas
3. Configura el OpenSSH de tu servidor para usar el archivo authorized_keys generado:
   ```
   # En /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Clona via SSH:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

El servicio de autenticacion SSH tambien expone una API en `/api/ssh/authorized-keys` para uso con la directiva `AuthorizedKeysCommand` de OpenSSH.

### 11. Autenticacion LDAP / Active Directory

Autentica usuarios contra el directorio LDAP o dominio Active Directory de tu organizacion:

1. Ve a **Admin > Settings** y desplazate hasta **LDAP / Active Directory Authentication**
2. Habilita LDAP y completa los detalles de tu servidor:
   - **Server**: El nombre de host de tu servidor LDAP (ej., `dc01.corp.local`)
   - **Port**: 389 para LDAP, 636 para LDAPS
   - **SSL/TLS**: Habilita para LDAPS, o usa StartTLS para actualizar una conexion sin cifrar
3. Configura una cuenta de servicio para buscar usuarios:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password**: La contrasena de la cuenta de servicio
4. Establece los parametros de busqueda:
   - **Search Base DN**: `OU=Users,DC=corp,DC=local`
   - **User Filter**: `(sAMAccountName={0})` para AD, `(uid={0})` para OpenLDAP
5. Mapea los atributos LDAP a campos de usuario:
   - **Username**: `sAMAccountName` (AD) o `uid` (OpenLDAP)
   - **Email**: `mail`
   - **Display Name**: `displayName`
6. Opcionalmente establece un **Admin Group DN** — los miembros de este grupo son promovidos automaticamente a administrador
7. Haz clic en **Test LDAP Connection** para verificar la configuracion
8. Guarda la configuracion

Los usuarios ahora pueden iniciar sesion con sus credenciales de dominio en la pagina de inicio de sesion. En el primer inicio de sesion, se crea automaticamente una cuenta local con atributos sincronizados desde el directorio. La autenticacion LDAP tambien se usa para operaciones Git HTTP (clone/push).

### 12. Secretos del Repositorio

Agrega secretos cifrados a los repositorios para usar en flujos de trabajo CI/CD:

1. Ve a la pestana **Settings** de tu repositorio
2. Desplazate hasta la tarjeta **Secrets** y haz clic en **Add secret**
3. Ingresa un nombre (ej., `DEPLOY_TOKEN`) y un valor — el valor se cifra con AES-256
4. Los secretos se inyectan automaticamente como variables de entorno en cada ejecucion de flujo de trabajo

Referencia secretos en tu flujo de trabajo:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. Inicio de Sesion OAuth / SSO

Inicia sesion con proveedores de identidad externos:

1. Ve a **Admin > OAuth / SSO** y configura los proveedores que deseas habilitar
2. Ingresa el **Client ID** y **Client Secret** de la consola de desarrollador del proveedor
3. Marca **Enable** — solo los proveedores con ambas credenciales completadas apareceran en la pagina de inicio de sesion
4. La URL de callback para cada proveedor se muestra en el panel de administracion (ej., `https://yourserver/oauth/callback/github`)

Proveedores soportados: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Los usuarios pueden vincular multiples proveedores a su cuenta en **Settings > Linked Accounts**.

### 14. Importar Repositorio

Importa repositorios desde fuentes externas con historial completo:

1. Haz clic en **Import** en la pagina principal
2. Selecciona un tipo de fuente (URL Git, GitHub, GitLab o Bitbucket)
3. Ingresa la URL del repositorio y opcionalmente un token de autenticacion para repositorios privados
4. Para importaciones de GitHub/GitLab/Bitbucket, opcionalmente importa incidencias y pull requests
5. Rastrea el progreso de la importacion en tiempo real en la pagina de importacion

### 15. Forks y Sincronizacion Upstream

Haz fork de un repositorio y mantenlo sincronizado:

1. Haz clic en el boton **Fork** en cualquier pagina de repositorio
2. Se crea un fork bajo tu nombre de usuario con un enlace al original
3. Haz clic en **Sync fork** junto a la insignia "forked from" para obtener los ultimos cambios de upstream

### 16. Auto-Release CI/CD

MyPersonalGit incluye un pipeline CI/CD integrado que auto-etiqueta, crea releases y publica imagenes Docker en cada push a main. Los flujos de trabajo se activan automaticamente en push — no se necesita servicio CI externo.

**Como funciona:**
1. Un push a `main` activa automaticamente `.github/workflows/release.yml`
2. Incrementa la version de parche (`v1.15.1` -> `v1.15.2`), crea una etiqueta git
3. Inicia sesion en Docker Hub, construye la imagen y la publica como `:latest` y `:vX.Y.Z`

**Configuracion:**
1. Ve a **Settings > Secrets** de tu repositorio en MyPersonalGit
2. Agrega un secreto llamado `DOCKERHUB_TOKEN` con tu token de acceso de Docker Hub
3. Asegurate de que el contenedor MyPersonalGit tenga montado el socket Docker (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Haz push a main — el flujo de trabajo se activa automaticamente

**Compatibilidad con GitHub Actions:**
El mismo YAML de flujo de trabajo tambien funciona en GitHub Actions — sin cambios necesarios. MyPersonalGit traduce acciones `uses:` en comandos shell equivalentes en tiempo de ejecucion:

| Accion de GitHub | Traduccion en MyPersonalGit |
|---|---|
| `actions/checkout@v4` | El repositorio ya esta clonado en `/workspace` |
| `actions/setup-dotnet@v4` | Instala .NET SDK via script de instalacion oficial |
| `actions/setup-node@v4` | Instala Node.js via NodeSource |
| `actions/setup-python@v5` | Instala Python via apt/apk |
| `actions/setup-java@v4` | Instala OpenJDK via apt/apk |
| `docker/login-action@v3` | `docker login` con contrasena por stdin |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op (usa el builder por defecto) |
| `softprops/action-gh-release@v2` | Crea una entidad Release real en la base de datos |
| `${{ secrets.X }}` | Variable de entorno `$X` |
| `${{ steps.X.outputs.Y }}` | Variable de entorno `$Y` |
| `${{ github.sha }}` | Variable de entorno `$GITHUB_SHA` |

**Jobs paralelos:**
Los jobs se ejecutan en paralelo por defecto. Usa `needs:` para declarar dependencias:
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
Los jobs sin `needs:` inician inmediatamente. Un job se cancela si alguna de sus dependencias falla.

**Pasos condicionales:**
Usa `if:` para controlar cuando se ejecutan los pasos:
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
Expresiones soportadas: `always()`, `success()` (por defecto), `failure()`, `cancelled()`, `true`, `false`.

**Salidas de pasos:**
Los pasos pueden pasar valores a pasos posteriores via `$GITHUB_OUTPUT`:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Builds de matriz:**
Expande jobs a traves de multiples combinaciones usando `strategy.matrix`:
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
Esto crea 4 jobs: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)`, etc. Todos se ejecutan en paralelo.

**Disparadores manuales con entradas (`workflow_dispatch`):**
Define entradas tipadas que se muestran como un formulario en la interfaz al disparar manualmente:
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
Los valores de entrada se inyectan como variables de entorno `INPUT_<NAME>` (en mayusculas).

**Timeouts de jobs:**
Establece `timeout-minutes` en jobs para fallarlos automaticamente si se ejecutan demasiado tiempo:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
El timeout por defecto es 360 minutos (6 horas), igual que GitHub Actions.

**Condicionales a nivel de job:**
Usa `if:` en jobs para omitirlos basandose en condiciones:
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

**Salidas de jobs:**
Los jobs pueden pasar valores a jobs dependientes via `outputs:`:
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

**Continuar en caso de error:**
Permite que un paso falle sin fallar el job:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Filtrado de rutas:**
Solo activa flujos de trabajo cuando cambian archivos especificos:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # o usa paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Directorio de trabajo:**
Establece donde se ejecutan los comandos:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # se ejecuta en src/app
      - run: npm test
        working-directory: tests  # sobrescribe el valor por defecto
```

**Re-ejecutar flujos de trabajo:**
Haz clic en el boton **Re-run** en cualquier ejecucion de flujo de trabajo completada, fallida o cancelada para crear una nueva ejecucion con los mismos jobs, pasos y configuracion.

**Flujos de trabajo de pull request:**
Los flujos de trabajo con `on: pull_request` se activan automaticamente cuando se crea un PR no borrador, ejecutando verificaciones contra la rama de origen.

**Verificaciones de estado de commits:**
Los flujos de trabajo establecen automaticamente estados de commits (pendiente/exito/fallo) para que puedas ver los resultados de compilacion en los PRs y aplicar verificaciones requeridas via proteccion de ramas.

**Cancelacion de flujos de trabajo:**
Haz clic en el boton **Cancel** en cualquier flujo de trabajo en ejecucion o en cola en la interfaz de Actions para detenerlo inmediatamente.

**Insignias de estado:**
Incrusta insignias de estado de compilacion en tu README o en cualquier lugar:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Filtra por nombre de flujo de trabajo: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. Feeds RSS/Atom

Suscribete a la actividad del repositorio usando feeds Atom estandar en cualquier lector RSS:

```
# Commits del repositorio
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Releases del repositorio
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Etiquetas del repositorio
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Actividad del usuario
http://localhost:8080/api/feeds/users/admin/activity.atom

# Actividad global (todos los repositorios)
http://localhost:8080/api/feeds/global/activity.atom
```

No se requiere autenticacion para repositorios publicos. Agrega estas URLs a cualquier lector de feeds (Feedly, Miniflux, FreshRSS, etc.) para mantenerte informado de los cambios.

## Configuracion de Base de Datos

MyPersonalGit usa **SQLite** por defecto — sin configuracion, base de datos de archivo unico, perfecto para uso personal y equipos pequenos.

Para despliegues mas grandes (muchos usuarios concurrentes, alta disponibilidad, o si ya ejecutas PostgreSQL), puedes cambiar a **PostgreSQL**:

### Usar PostgreSQL

**Docker Compose** (recomendado para PostgreSQL):
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

**Solo variables de entorno** (si ya tienes un servidor PostgreSQL):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

Las migraciones de EF Core se ejecutan automaticamente al iniciar para ambos proveedores. No se requiere configuracion manual del esquema.

### Cambiar desde el Panel de Administracion

Tambien puedes cambiar de proveedor de base de datos directamente desde la interfaz web:

1. Ve a **Admin > Settings** — la tarjeta **Database** esta en la parte superior
2. Selecciona **PostgreSQL** del menu desplegable de proveedores
3. Ingresa tu cadena de conexion de PostgreSQL (ej., `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Haz clic en **Save Database Settings**
5. Reinicia la aplicacion para que el cambio surta efecto

La configuracion se guarda en `~/.mypersonalgit/database.json` (fuera de la base de datos misma, para que pueda leerse antes de conectar).

### Elegir una Base de Datos

| | SQLite | PostgreSQL |
|---|---|---|
| **Configuracion** | Sin configuracion (por defecto) | Requiere un servidor PostgreSQL |
| **Ideal para** | Uso personal, equipos pequenos, NAS | Equipos de 50+, alta concurrencia |
| **Respaldo** | Copiar el archivo `.db` | `pg_dump` estandar |
| **Concurrencia** | Escritor unico (suficiente para la mayoria de usos) | Multi-escritor completo |
| **Migracion** | N/A | Cambiar proveedor + ejecutar la app (auto-migra) |

## Desplegar en un NAS

MyPersonalGit funciona excelente en un NAS (QNAP, Synology, etc.) via Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

El montaje del socket Docker es opcional — solo es necesario si deseas la ejecucion de flujos de trabajo CI/CD. El puerto 2222 solo es necesario si habilitas el servidor SSH integrado.

## Configuracion

Todos los ajustes se pueden configurar en `appsettings.json`, mediante variables de entorno o a traves del panel de Administracion en `/admin`:

- Proveedor de base de datos (SQLite o PostgreSQL)
- Directorio raiz del proyecto
- Requisitos de autenticacion
- Configuracion de registro de usuarios
- Interruptores de funcionalidades (Issues, Wiki, Projects, Actions)
- Tamano maximo del repositorio y cantidad por usuario
- Configuracion SMTP para notificaciones por correo electronico
- Configuracion de notificaciones push (Ntfy/Gotify)
- Servidor SSH integrado (habilitar/deshabilitar, puerto)
- Autenticacion LDAP/Active Directory (servidor, bind DN, base de busqueda, filtro de usuarios, mapeo de atributos, grupo de administradores)
- Configuracion de proveedores OAuth/SSO (Client ID/Secret por proveedor)

## Estructura del Proyecto

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Paginas Blazor (Home, RepoDetails, Issues, PRs, Packages, etc.)
  Controllers/       # Endpoints REST API (NuGet, npm, Generic, Registry, etc.)
  Data/              # EF Core DbContext, implementaciones de servicios
  Models/            # Modelos de dominio
  Migrations/        # Migraciones de EF Core
  Services/          # Middleware (autenticacion, backend Git HTTP, Pages, autenticacion Registry)
    SshServer/       # Servidor SSH integrado (protocolo SSH2, ECDH, AES-CTR)
  Program.cs         # Inicio de la app, DI, pipeline de middleware
MyPersonalGit.Tests/
  UnitTest1.cs       # Tests xUnit con base de datos InMemory
```

## Ejecutar Tests

```bash
dotnet test
```

## Licencia

MIT
