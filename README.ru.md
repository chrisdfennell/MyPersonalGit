🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Самостоятельно размещаемый Git-сервер с веб-интерфейсом в стиле GitHub, построенный на ASP.NET Core и Blazor Server. Просматривайте репозитории, управляйте задачами, пулл-реквестами, вики, проектами и многим другим — всё на вашей собственной машине или сервере.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## Содержание

- [Возможности](#возможности)
- [Технологический стек](#технологический-стек)
- [Быстрый старт](#быстрый-старт)
  - [Docker (рекомендуется)](#docker-рекомендуется)
  - [Локальный запуск](#локальный-запуск)
  - [Переменные окружения](#переменные-окружения)
- [Использование](#использование)
  - [Вход в систему](#1-вход-в-систему)
  - [Создание репозитория](#2-создание-репозитория)
  - [Клонирование и отправка](#3-клонирование-и-отправка)
  - [Клонирование из IDE](#4-клонирование-из-ide)
  - [Веб-редактор](#5-веб-редактор)
  - [Реестр контейнеров](#6-реестр-контейнеров)
  - [Реестр пакетов](#7-реестр-пакетов)
  - [Pages (статические сайты)](#8-pages-хостинг-статических-сайтов)
  - [Push-уведомления](#9-push-уведомления)
  - [Аутентификация по SSH-ключам](#10-аутентификация-по-ssh-ключам)
  - [LDAP / Active Directory](#11-аутентификация-ldap--active-directory)
  - [Секреты репозитория](#12-секреты-репозитория)
  - [OAuth / SSO вход](#13-oauth--sso-вход)
  - [Импорт репозитория](#14-импорт-репозитория)
  - [Форки и синхронизация с upstream](#15-форки-и-синхронизация-с-upstream)
  - [CI/CD автоматический релиз](#16-cicd-автоматический-релиз)
  - [RSS/Atom ленты](#17-rssatom-ленты)
- [Настройка базы данных](#настройка-базы-данных)
  - [Использование PostgreSQL](#использование-postgresql)
  - [Переключение через панель администратора](#переключение-через-панель-администратора)
  - [Выбор базы данных](#выбор-базы-данных)
- [Развёртывание на NAS](#развёртывание-на-nas)
- [Конфигурация](#конфигурация)
- [Структура проекта](#структура-проекта)
- [Запуск тестов](#запуск-тестов)
- [Лицензия](#лицензия)

---

## Возможности

### Код и репозитории
- **Управление репозиториями** — Создание, просмотр и удаление Git-репозиториев с полноценным браузером кода, редактором файлов, историей коммитов, ветками и тегами
- **Импорт/миграция репозиториев** — Импорт репозиториев с GitHub, GitLab, Bitbucket или по любому Git URL с опциональным импортом задач и PR. Фоновая обработка с отслеживанием прогресса
- **Архивирование репозиториев** — Пометка репозиториев как доступных только для чтения с визуальными бейджами; push-операции заблокированы для архивных репозиториев
- **Git Smart HTTP** — Клонирование, fetch и push по HTTP с Basic Auth
- **Встроенный SSH-сервер** — Нативный SSH-сервер для Git-операций — внешний OpenSSH не требуется. Поддерживает обмен ключами ECDH, шифрование AES-CTR и аутентификацию по открытым ключам (RSA, ECDSA, Ed25519)
- **Аутентификация по SSH-ключам** — Добавляйте SSH-ключи к вашей учётной записи и аутентифицируйте Git-операции через SSH с автоматическим управлением `authorized_keys` (или через встроенный SSH-сервер)
- **Форки и синхронизация с upstream** — Создание форков репозиториев, синхронизация с upstream одним кликом, отображение связей форков в интерфейсе
- **Git LFS** — Поддержка Large File Storage для отслеживания бинарных файлов
- **Зеркалирование репозиториев** — Зеркалирование репозиториев на/с внешних Git-серверов
- **Сравнение веток** — Сравнение веток с подсчётом коммитов впереди/позади и полным отображением различий
- **Статистика языков** — Полоса распределения языков в стиле GitHub на каждой странице репозитория
- **Защита веток** — Настраиваемые правила для обязательных ревью, проверок статуса, предотвращения force-push и обязательного одобрения CODEOWNERS
- **Защита тегов** — Защита тегов от удаления, принудительного обновления и несанкционированного создания с помощью glob-шаблонов и списков разрешённых пользователей
- **Верификация подписей коммитов** — Проверка GPG-подписей коммитов и аннотированных тегов с бейджами «Verified» / «Signed» в интерфейсе
- **Метки репозитория** — Управление метками с пользовательскими цветами для каждого репозитория; метки автоматически копируются при создании репозиториев из шаблонов
- **AGit Flow** — Рабочий процесс push-to-review: `git push origin HEAD:refs/for/main` создаёт пулл-реквест без форка или создания удалённых веток. При последующих push-операциях обновляются существующие открытые PR
- **Обзор** — Просмотр всех доступных репозиториев с поиском, сортировкой и фильтрацией по темам
- **Поиск** — Полнотекстовый поиск по репозиториям, задачам, PR и коду

### Совместная работа
- **Задачи и пулл-реквесты** — Создание, комментирование, закрытие/повторное открытие задач и PR с метками, множественными исполнителями, сроками и ревью. Слияние PR стратегиями merge commit, squash или rebase. Веб-разрешение конфликтов слияния с параллельным отображением различий
- **Зависимости задач** — Определение связей «блокируется» и «блокирует» между задачами с обнаружением циклических зависимостей
- **Закрепление и блокировка задач** — Закрепление важных задач вверху списка и блокировка обсуждений для предотвращения дальнейших комментариев
- **Редактирование и удаление комментариев** — Редактирование или удаление собственных комментариев в задачах и пулл-реквестах с пометкой «(отредактировано)»
- **Разрешение конфликтов слияния** — Разрешение конфликтов слияния прямо в браузере с визуальным редактором, показывающим base/ours/theirs, кнопками быстрого принятия и валидацией маркеров конфликтов
- **Обсуждения** — Обсуждения с ветками в стиле GitHub Discussions для каждого репозитория с категориями (Общее, Вопрос-Ответ, Объявления, Идеи, Покажи и расскажи, Опросы), закрепление/блокировка, отметка как ответ и голосование
- **Предложения по коду в ревью** — Режим «Предложить изменения» в инлайн-ревью PR позволяет рецензентам предлагать замены кода прямо в diff
- **Эмодзи-реакции** — Реакции на задачи, PR, обсуждения и комментарии: палец вверх/вниз, сердце, смех, ура, смущение, ракета и глаза
- **CODEOWNERS** — Автоматическое назначение рецензентов PR на основе путей файлов с опциональным требованием одобрения CODEOWNERS перед слиянием
- **Шаблоны репозиториев** — Создание новых репозиториев из шаблонов с автоматическим копированием файлов, меток, шаблонов задач и правил защиты веток
- **Черновые задачи и шаблоны задач** — Создание черновых задач (в процессе) и определение переиспользуемых шаблонов задач (отчёт об ошибке, запрос функции) для каждого репозитория с метками по умолчанию
- **Вики** — Markdown-вики для каждого репозитория с историей ревизий
- **Проекты** — Kanban-доски с перетаскиваемыми карточками для организации работы
- **Сниппеты** — Публикация фрагментов кода (аналог GitHub Gists) с подсветкой синтаксиса и несколькими файлами
- **Организации и команды** — Создание организаций с участниками и командами, назначение прав команд на репозитории
- **Гранулярные права доступа** — Пятиуровневая модель прав (Read, Triage, Write, Maintain, Admin) для тонкой настройки доступа к репозиториям
- **Вехи** — Отслеживание прогресса задач по вехам с индикаторами прогресса и сроками
- **Комментарии к коммитам** — Комментирование отдельных коммитов с опциональными ссылками на файл/строку
- **Темы репозиториев** — Присвоение тем репозиториям для обнаружения и фильтрации на странице «Обзор»

### CI/CD и DevOps
- **CI/CD Runner** — Определение рабочих процессов в `.github/workflows/*.yml` и их запуск в Docker-контейнерах. Автоматический запуск при push и pull request событиях
- **Совместимость с GitHub Actions** — Один и тот же YAML-файл рабочего процесса работает как в MyPersonalGit, так и в GitHub Actions. Переводит `uses:` действия (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) в эквивалентные shell-команды
- **Параллельные задания с `needs:`** — Задания объявляют зависимости через `needs:` и выполняются параллельно, когда независимы. Зависимые задания ожидают своих предшественников и автоматически отменяются при сбое зависимости
- **Условные шаги (`if:`)** — Шаги поддерживают выражения `if:`: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Шаги очистки с `if: failure()` или `if: always()` всё равно выполняются после предыдущих сбоев
- **Выходные данные шагов (`$GITHUB_OUTPUT`)** — Шаги могут записывать пары `key=value` или многострочные `key<<DELIMITER` в `$GITHUB_OUTPUT`, и последующие шаги получают их как переменные окружения, совместимо с синтаксисом `${{ steps.X.outputs.Y }}`
- **Контекст `github`** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` и `CI=true` автоматически подставляются в каждое задание
- **Матричные сборки** — `strategy.matrix` разворачивает задания по нескольким комбинациям переменных (например, ОС x версия). Поддерживает `fail-fast` и подстановку `${{ matrix.X }}` в `runs-on`, командах шагов и именах шагов
- **Входные параметры `workflow_dispatch`** — Ручные триггеры с типизированными входными параметрами (string, boolean, choice, number). UI показывает форму ввода при ручном запуске рабочих процессов. Значения подставляются как переменные окружения `INPUT_*`
- **Тайм-ауты заданий (`timeout-minutes`)** — Установите `timeout-minutes` для заданий, чтобы автоматически завершать их при превышении лимита. По умолчанию: 360 минут (как в GitHub Actions)
- **`if:` на уровне задания** — Пропуск целых заданий по условию. Задания с `if: always()` выполняются даже при сбое зависимостей. Пропущенные задания не приводят к сбою запуска
- **Выходные данные заданий** — Задания объявляют `outputs:`, которые нижестоящие `needs:`-задания потребляют через `${{ needs.X.outputs.Y }}`. Выходные данные формируются из выходов шагов после завершения задания
- **`continue-on-error`** — Пометка отдельных шагов как допускающих сбой без провала задания. Полезно для необязательных валидаций или уведомлений
- **Фильтр `on.push.paths`** — Запуск рабочих процессов только при изменении определённых файлов. Поддерживает glob-шаблоны (`src/**`, `*.ts`) и `paths-ignore:` для исключений
- **Повторный запуск рабочих процессов** — Повторный запуск неудавшихся, успешных или отменённых запусков одним кликом из интерфейса Actions. Создаёт новый запуск с той же конфигурацией
- **`working-directory`** — Установите `defaults.run.working-directory` на уровне рабочего процесса или `working-directory:` на уровне шага для управления местом выполнения команд
- **`defaults.run.shell`** — Настройка оболочки для рабочего процесса или отдельного шага (`bash`, `sh`, `python3` и т.д.)
- **`strategy.max-parallel`** — Ограничение параллельного выполнения матричных заданий
- **`on.workflow_run`** — Цепочка рабочих процессов: запуск процесса B при завершении процесса A. Фильтрация по имени рабочего процесса и `types: [completed]`
- **Автоматическое создание релизов** — `softprops/action-gh-release` создаёт реальные сущности Release с тегом, заголовком, телом changelog и флагами pre-release/draft. Архивы исходного кода (ZIP и TAR.GZ) автоматически прикрепляются как загружаемые ассеты
- **Конвейер автоматического релиза** — Встроенный рабочий процесс автоматически создаёт теги версий, генерирует changelog и отправляет Docker-образы в Docker Hub при каждом push в main
- **Проверки статуса коммитов** — Рабочие процессы автоматически устанавливают статус pending/success/failure для коммитов, видимый в пулл-реквестах
- **Отмена рабочих процессов** — Отмена запущенных или стоящих в очереди рабочих процессов из интерфейса Actions
- **Управление параллелизмом** — Новые push-операции автоматически отменяют находящиеся в очереди запуски того же рабочего процесса
- **Переменные окружения рабочих процессов** — Установка `env:` на уровне рабочего процесса, задания или шага в YAML
- **Бейджи статуса** — Встраиваемые SVG-бейджи для статуса рабочего процесса и коммита (`/api/badge/{repo}/workflow`)
- **Загрузка артефактов** — Загрузка артефактов сборки прямо из интерфейса Actions
- **Управление секретами** — Зашифрованные секреты репозитория (AES-256), подставляемые как переменные окружения в запуски CI/CD
- **Вебхуки** — Запуск внешних сервисов по событиям репозитория
- **Метрики Prometheus** — Встроенный эндпоинт `/metrics` для мониторинга

### Хостинг пакетов и контейнеров
- **Реестр контейнеров** — Размещение Docker/OCI-образов с `docker push` и `docker pull` (OCI Distribution Spec)
- **Реестр NuGet** — Размещение .NET-пакетов с полным NuGet v3 API (индекс сервисов, поиск, push, restore)
- **Реестр npm** — Размещение Node.js-пакетов со стандартными npm publish/install
- **Реестр PyPI** — Размещение Python-пакетов с PEP 503 Simple API, JSON metadata API и совместимостью с `twine upload`
- **Реестр Maven** — Размещение Java/JVM-пакетов со стандартной структурой Maven-репозитория, генерацией `maven-metadata.xml` и поддержкой `mvn deploy`
- **Универсальные пакеты** — Загрузка и скачивание произвольных бинарных артефактов через REST API

### Статические сайты
- **Pages** — Обслуживание статических веб-сайтов прямо из ветки репозитория (аналог GitHub Pages) по адресу `/pages/{owner}/{repo}/`

### RSS/Atom ленты
- **Ленты репозитория** — Atom-ленты коммитов, релизов и тегов для каждого репозитория (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Лента активности пользователя** — Лента активности для каждого пользователя (`/api/feeds/users/{username}/activity.atom`)
- **Глобальная лента активности** — Общая лента активности сайта (`/api/feeds/global/activity.atom`)

### Уведомления
- **Уведомления в приложении** — Упоминания, комментарии и активность в репозиториях
- **Push-уведомления** — Интеграция с Ntfy и Gotify для мобильных/десктопных уведомлений в реальном времени с индивидуальной подпиской для пользователей

### Аутентификация
- **OAuth2 / SSO** — Вход через GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord или Twitter/X. Администраторы настраивают Client ID и Secret для каждого провайдера в панели администратора — пользователям показываются только провайдеры с заполненными учётными данными
- **Провайдер OAuth2** — Работа в качестве провайдера идентификации, чтобы другие приложения могли использовать «Войти через MyPersonalGit». Реализует Authorization Code flow с PKCE, обновление токенов, эндпоинт userinfo и обнаружение OpenID Connect (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Аутентификация пользователей через LDAP-каталог или домен Active Directory. Пользователи автоматически создаются при первом входе с синхронизированными атрибутами (email, отображаемое имя). Поддерживает назначение администраторов на основе групп, SSL/TLS и StartTLS
- **SSPI / Интегрированная аутентификация Windows** — Прозрачный Single Sign-On для доменных пользователей Windows через Negotiate/NTLM. Пользователи домена аутентифицируются автоматически без ввода учётных данных. Включается в Admin > Settings (только Windows)
- **Двухфакторная аутентификация** — TOTP-базированная 2FA с поддержкой приложений-аутентификаторов и кодами восстановления
- **WebAuthn / Passkeys** — Поддержка аппаратных ключей безопасности FIDO2 и passkeys в качестве второго фактора. Регистрация YubiKeys, платформенных аутентификаторов (Face ID, Windows Hello, Touch ID) и других FIDO2-устройств. Верификация счётчика подписей для обнаружения клонированных ключей
- **Привязанные аккаунты** — Пользователи могут привязать несколько OAuth-провайдеров к своей учётной записи в настройках

### Администрирование
- **Панель администратора** — Системные настройки (включая провайдер БД, SSH-сервер, LDAP/AD, страницы подвала), управление пользователями, журналы аудита и статистика
- **Настраиваемые страницы подвала** — Условия использования, Политика конфиденциальности, Документация и Контакты с Markdown-содержимым, редактируемым из Admin > Settings
- **Профили пользователей** — Тепловая карта вкладов, лента активности и статистика для каждого пользователя
- **Персональные токены доступа** — Токенная аутентификация API с настраиваемыми областями доступа и опциональными ограничениями на уровне маршрутов (glob-шаблоны вроде `/api/packages/**` для ограничения доступа токена к конкретным API-путям)
- **Резервное копирование и восстановление** — Экспорт и импорт данных сервера
- **Сканирование безопасности** — Реальное сканирование уязвимостей зависимостей на базе [OSV.dev](https://osv.dev/). Автоматически извлекает зависимости из `.csproj` (NuGet), `package.json` (npm) и `requirements.txt` (PyPI), затем проверяет каждую по известным CVE. Отчёты о серьёзности, исправленных версиях и ссылках на рекомендации. Плюс ручные рекомендации по безопасности с рабочим процессом черновик/публикация/закрытие
- **Тёмная тема** — Полная поддержка тёмной/светлой темы с переключателем в заголовке
- **Многоязычность / i18n** — Полная локализация всех 27 страниц с 676 ключами ресурсов. Поставляется с 11 языками: английский, испанский, французский, немецкий, японский, корейский, китайский (упрощённый), португальский, русский, итальянский и турецкий. Добавьте больше, создав файлы `SharedResource.{locale}.resx`

## Технологический стек

| Компонент | Технология |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (интерактивный серверный рендеринг) |
| База данных | SQLite (по умолчанию) или PostgreSQL через Entity Framework Core 10 |
| Git-движок | LibGit2Sharp |
| Аутентификация | BCrypt хеширование паролей, сессионная аутентификация, PAT-токены, OAuth2 (8 провайдеров + режим провайдера), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| SSH-сервер | Встроенная реализация протокола SSH2 (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Мониторинг | Метрики Prometheus |

## Быстрый старт

### Предварительные требования

- [Docker](https://docs.docker.com/get-docker/) (рекомендуется)
- Или [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git для локальной разработки

### Docker (рекомендуется)

Загрузите с Docker Hub и запустите:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Порт 2222 опционален — нужен только если вы включите встроенный SSH-сервер в Admin > Settings.

Или используйте Docker Compose:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

Приложение будет доступно по адресу **http://localhost:8080**.

> **Учётные данные по умолчанию**: `admin` / `admin`
>
> **Немедленно смените пароль по умолчанию** через панель администратора после первого входа.

### Локальный запуск

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

Приложение запустится по адресу **http://localhost:5146**.

### Переменные окружения

| Переменная | Описание | По умолчанию |
|----------|-------------|---------|
| `Database__Provider` | Движок базы данных: `sqlite` или `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Строка подключения к базе данных | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Директория хранения Git-репозиториев | `/repos` |
| `Git__RequireAuth` | Требовать аутентификацию для Git HTTP операций | `true` |
| `Git__Users__<username>` | Установка пароля для пользователя Git HTTP Basic Auth | — |
| `RESET_ADMIN_PASSWORD` | Экстренный сброс пароля администратора при запуске | — |
| `Secrets__EncryptionKey` | Пользовательский ключ шифрования для секретов репозитория | Выводится из строки подключения к БД |
| `Ssh__DataDir` | Директория для данных SSH (ключи хоста, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Путь к сгенерированному файлу authorized_keys | `<DataDir>/authorized_keys` |

> **Примечание:** Порт встроенного SSH-сервера и настройки LDAP конфигурируются через панель администратора (Admin > Settings), а не через переменные окружения. Это позволяет менять их без повторного развёртывания.

## Использование

### 1. Вход в систему

Откройте приложение и нажмите **Sign In**. При первой установке используйте учётные данные по умолчанию (`admin` / `admin`). Создавайте дополнительных пользователей через панель **Admin** или включив регистрацию пользователей в Admin > Settings.

### 2. Создание репозитория

Нажмите зелёную кнопку **New** на главной странице, введите имя и нажмите **Create**. Это создаст голый Git-репозиторий на сервере, который можно клонировать, отправлять изменения и управлять через веб-интерфейс.

### 3. Клонирование и отправка

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Если включена Git HTTP аутентификация, вам будет предложено ввести учётные данные, настроенные через переменные окружения `Git__Users__<username>`. Они отличаются от входа в веб-интерфейс.

### 4. Клонирование из IDE

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > вставьте `http://localhost:8080/git/MyRepo.git`

**Visual Studio**: **Git > Clone Repository** > вставьте URL

**JetBrains**: **File > New > Project from Version Control** > вставьте URL

### 5. Веб-редактор

Вы можете редактировать файлы прямо в браузере:
- Перейдите в репозиторий, нажмите на любой файл, затем нажмите **Edit**
- Используйте **Add files > Create new file** для добавления файлов без локального клонирования
- Используйте **Add files > Upload files/folder** для загрузки с вашей машины

### 6. Реестр контейнеров

Отправляйте и получайте Docker/OCI-образы прямо на вашем сервере:

```bash
# Вход (используйте персональный токен доступа из Settings > Access Tokens)
docker login localhost:8080 -u youruser

# Отправка образа
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Получение образа
docker pull localhost:8080/myapp:v1
```

> **Примечание:** Docker по умолчанию требует HTTPS. Для HTTP добавьте ваш сервер в `insecure-registries` Docker в `~/.docker/daemon.json`:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Реестр пакетов

**NuGet (.NET-пакеты):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (Node.js-пакеты):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (Python-пакеты):**
```bash
# Установка пакета
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# Загрузка с помощью twine
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

**Maven (Java/JVM-пакеты):**
```xml
<!-- В вашем pom.xml добавьте репозиторий -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- В settings.xml добавьте учётные данные -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Универсальные (любые бинарные файлы):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Просматривайте все пакеты на странице `/packages` в веб-интерфейсе.

### 8. Pages (хостинг статических сайтов)

Обслуживание статических веб-сайтов из ветки репозитория:

1. Перейдите во вкладку **Settings** вашего репозитория и включите **Pages**
2. Установите ветку (по умолчанию: `gh-pages`)
3. Отправьте HTML/CSS/JS в эту ветку
4. Перейдите по адресу `http://localhost:8080/pages/{username}/{repo}/`

### 9. Push-уведомления

Настройте Ntfy или Gotify в **Admin > System Settings** для получения push-уведомлений на телефон или компьютер при создании задач, PR или комментариев. Пользователи могут подписаться/отписаться в **Settings > Notifications**.

### 10. Аутентификация по SSH-ключам

Используйте SSH-ключи для беспарольных Git-операций. Есть два варианта:

#### Вариант A: Встроенный SSH-сервер (рекомендуется)

Внешний SSH-демон не требуется — MyPersonalGit запускает собственный SSH-сервер:

1. Перейдите в **Admin > Settings** и включите **Built-in SSH Server**
2. Установите SSH-порт (по умолчанию: 2222) — используйте 22, если системный SSH не запущен
3. Сохраните настройки и перезапустите сервер (изменение порта требует перезапуска)
4. Перейдите в **Settings > SSH Keys** и добавьте ваш открытый ключ (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` или `~/.ssh/id_ecdsa.pub`)
5. Клонирование через SSH:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Встроенный SSH-сервер поддерживает обмен ключами ECDH-SHA2-NISTP256, шифрование AES-128/256-CTR, HMAC-SHA2-256 и аутентификацию по открытым ключам Ed25519, RSA и ECDSA.

#### Вариант B: Системный OpenSSH

Если вы предпочитаете использовать системный SSH-демон:

1. Перейдите в **Settings > SSH Keys** и добавьте ваш открытый ключ
2. MyPersonalGit автоматически поддерживает файл `authorized_keys` из всех зарегистрированных SSH-ключей
3. Настройте OpenSSH вашего сервера на использование сгенерированного файла authorized_keys:
   ```
   # В /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Клонирование через SSH:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

Сервис SSH-аутентификации также предоставляет API по адресу `/api/ssh/authorized-keys` для использования с директивой `AuthorizedKeysCommand` OpenSSH.

### 11. Аутентификация LDAP / Active Directory

Аутентификация пользователей через LDAP-каталог или домен Active Directory вашей организации:

1. Перейдите в **Admin > Settings** и прокрутите до **LDAP / Active Directory Authentication**
2. Включите LDAP и заполните данные вашего сервера:
   - **Server**: Имя хоста вашего LDAP-сервера (например, `dc01.corp.local`)
   - **Port**: 389 для LDAP, 636 для LDAPS
   - **SSL/TLS**: Включите для LDAPS или используйте StartTLS для обновления обычного соединения
3. Настройте сервисную учётную запись для поиска пользователей:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password**: Пароль сервисной учётной записи
4. Установите параметры поиска:
   - **Search Base DN**: `OU=Users,DC=corp,DC=local`
   - **User Filter**: `(sAMAccountName={0})` для AD, `(uid={0})` для OpenLDAP
5. Сопоставьте атрибуты LDAP с полями пользователя:
   - **Username**: `sAMAccountName` (AD) или `uid` (OpenLDAP)
   - **Email**: `mail`
   - **Display Name**: `displayName`
6. Опционально установите **Admin Group DN** — члены этой группы автоматически получают права администратора
7. Нажмите **Test LDAP Connection** для проверки настроек
8. Сохраните настройки

Теперь пользователи могут входить с доменными учётными данными на странице входа. При первом входе автоматически создаётся локальная учётная запись с синхронизированными атрибутами из каталога. LDAP-аутентификация также используется для Git HTTP операций (clone/push).

### 12. Секреты репозитория

Добавляйте зашифрованные секреты в репозитории для использования в CI/CD рабочих процессах:

1. Перейдите во вкладку **Settings** вашего репозитория
2. Прокрутите до карточки **Secrets** и нажмите **Add secret**
3. Введите имя (например, `DEPLOY_TOKEN`) и значение — значение шифруется с помощью AES-256
4. Секреты автоматически подставляются как переменные окружения в каждый запуск рабочего процесса

Ссылка на секреты в рабочем процессе:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO вход

Вход через внешних провайдеров идентификации:

1. Перейдите в **Admin > OAuth / SSO** и настройте провайдеров, которых хотите включить
2. Введите **Client ID** и **Client Secret** из консоли разработчика провайдера
3. Отметьте **Enable** — на странице входа будут показаны только провайдеры с заполненными учётными данными
4. URL обратного вызова для каждого провайдера показан в панели администратора (например, `https://yourserver/oauth/callback/github`)

Поддерживаемые провайдеры: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Пользователи могут привязать несколько провайдеров к своей учётной записи в **Settings > Linked Accounts**.

### 14. Импорт репозитория

Импорт репозиториев из внешних источников с полной историей:

1. Нажмите **Import** на главной странице
2. Выберите тип источника (Git URL, GitHub, GitLab или Bitbucket)
3. Введите URL репозитория и опционально токен аутентификации для приватных репозиториев
4. Для импорта с GitHub/GitLab/Bitbucket опционально импортируйте задачи и пулл-реквесты
5. Отслеживайте прогресс импорта в реальном времени на странице Import

### 15. Форки и синхронизация с upstream

Создайте форк репозитория и поддерживайте его в актуальном состоянии:

1. Нажмите кнопку **Fork** на любой странице репозитория
2. Форк создаётся под вашим именем пользователя со ссылкой на оригинал
3. Нажмите **Sync fork** рядом с бейджем «forked from» для получения последних изменений из upstream

### 16. CI/CD автоматический релиз

MyPersonalGit включает встроенный CI/CD конвейер, который автоматически создаёт теги, релизы и отправляет Docker-образы при каждом push в main. Рабочие процессы запускаются автоматически при push — внешний CI-сервис не нужен.

**Как это работает:**
1. Push в `main` автоматически запускает `.github/workflows/release.yml`
2. Увеличивает версию патча (`v1.15.1` -> `v1.15.2`), создаёт git-тег
3. Входит в Docker Hub, собирает образ и отправляет его как `:latest` и `:vX.Y.Z`

**Настройка:**
1. Перейдите в **Settings > Secrets** вашего репозитория в MyPersonalGit
2. Добавьте секрет с именем `DOCKERHUB_TOKEN` с вашим токеном доступа Docker Hub
3. Убедитесь, что контейнер MyPersonalGit имеет смонтированный Docker-сокет (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Сделайте push в main — рабочий процесс запустится автоматически

**Совместимость с GitHub Actions:**
Тот же YAML рабочего процесса работает и в GitHub Actions — без изменений. MyPersonalGit транслирует `uses:` действия в эквивалентные shell-команды во время выполнения:

| GitHub Action | Трансляция MyPersonalGit |
|---|---|
| `actions/checkout@v4` | Репозиторий уже клонирован в `/workspace` |
| `actions/setup-dotnet@v4` | Устанавливает .NET SDK через официальный скрипт установки |
| `actions/setup-node@v4` | Устанавливает Node.js через NodeSource |
| `actions/setup-python@v5` | Устанавливает Python через apt/apk |
| `actions/setup-java@v4` | Устанавливает OpenJDK через apt/apk |
| `docker/login-action@v3` | `docker login` с паролем через stdin |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | Нет операции (используется сборщик по умолчанию) |
| `softprops/action-gh-release@v2` | Создаёт реальную сущность Release в базе данных |
| `${{ secrets.X }}` | Переменная окружения `$X` |
| `${{ steps.X.outputs.Y }}` | Переменная окружения `$Y` |
| `${{ github.sha }}` | Переменная окружения `$GITHUB_SHA` |

**Параллельные задания:**
Задания выполняются параллельно по умолчанию. Используйте `needs:` для объявления зависимостей:
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
Задания без `needs:` запускаются немедленно. Задание отменяется, если любая из его зависимостей завершается с ошибкой.

**Условные шаги:**
Используйте `if:` для управления выполнением шагов:
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
Поддерживаемые выражения: `always()`, `success()` (по умолчанию), `failure()`, `cancelled()`, `true`, `false`.

**Выходные данные шагов:**
Шаги могут передавать значения последующим шагам через `$GITHUB_OUTPUT`:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Матричные сборки:**
Развёртывание заданий по нескольким комбинациям с помощью `strategy.matrix`:
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
Это создаёт 4 задания: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)` и т.д. Все выполняются параллельно.

**Ручные триггеры с входными параметрами (`workflow_dispatch`):**
Определите типизированные входные параметры, которые отображаются как форма в интерфейсе при ручном запуске:
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
Значения входных параметров подставляются как переменные окружения `INPUT_<NAME>` (в верхнем регистре).

**Тайм-ауты заданий:**
Установите `timeout-minutes` для заданий, чтобы автоматически завершать их при превышении времени:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Тайм-аут по умолчанию — 360 минут (6 часов), как в GitHub Actions.

**Условия на уровне задания:**
Используйте `if:` на заданиях для их пропуска по условию:
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

**Выходные данные заданий:**
Задания могут передавать значения нижестоящим заданиям через `outputs:`:
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

**Продолжение при ошибке:**
Позволяет шагу завершиться с ошибкой без провала задания:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Фильтрация по путям:**
Запуск рабочих процессов только при изменении определённых файлов:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # или используйте paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Рабочая директория:**
Установка места выполнения команд:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # выполняется в src/app
      - run: npm test
        working-directory: tests  # переопределяет значение по умолчанию
```

**Повторный запуск рабочих процессов:**
Нажмите кнопку **Re-run** на любом завершённом, неудавшемся или отменённом запуске рабочего процесса, чтобы создать новый запуск с теми же заданиями, шагами и конфигурацией.

**Рабочие процессы для пулл-реквестов:**
Рабочие процессы с `on: pull_request` автоматически запускаются при создании не-черновых PR, выполняя проверки исходной ветки.

**Проверки статуса коммитов:**
Рабочие процессы автоматически устанавливают статусы коммитов (pending/success/failure), чтобы вы могли видеть результаты сборки в PR и требовать обязательные проверки через защиту веток.

**Отмена рабочих процессов:**
Нажмите кнопку **Cancel** на любом запущенном или стоящем в очереди рабочем процессе в интерфейсе Actions для немедленной остановки.

**Бейджи статуса:**
Встраивайте бейджи статуса сборки в README или в любое другое место:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Фильтрация по имени рабочего процесса: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom ленты

Подписывайтесь на активность репозитория с помощью стандартных Atom-лент в любом RSS-ридере:

```
# Коммиты репозитория
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Релизы репозитория
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Теги репозитория
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Активность пользователя
http://localhost:8080/api/feeds/users/admin/activity.atom

# Глобальная активность (все репозитории)
http://localhost:8080/api/feeds/global/activity.atom
```

Аутентификация не требуется для публичных репозиториев. Добавьте эти URL в любой RSS-ридер (Feedly, Miniflux, FreshRSS и т.д.), чтобы получать уведомления об изменениях.

## Настройка базы данных

MyPersonalGit по умолчанию использует **SQLite** — нулевая конфигурация, однофайловая база данных, идеальна для личного использования и небольших команд.

Для крупных развёртываний (много одновременных пользователей, высокая доступность или если вы уже используете PostgreSQL) можно переключиться на **PostgreSQL**:

### Использование PostgreSQL

**Docker Compose** (рекомендуется для PostgreSQL):
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

**Только переменные окружения** (если у вас уже есть PostgreSQL-сервер):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

Миграции EF Core выполняются автоматически при запуске для обоих провайдеров. Ручная настройка схемы не требуется.

### Переключение через панель администратора

Вы также можете переключить провайдер базы данных прямо из веб-интерфейса:

1. Перейдите в **Admin > Settings** — карточка **Database** находится вверху
2. Выберите **PostgreSQL** из выпадающего списка провайдеров
3. Введите строку подключения PostgreSQL (например, `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Нажмите **Save Database Settings**
5. Перезапустите приложение для применения изменений

Конфигурация сохраняется в `~/.mypersonalgit/database.json` (вне базы данных, чтобы её можно было прочитать перед подключением).

### Выбор базы данных

| | SQLite | PostgreSQL |
|---|---|---|
| **Настройка** | Нулевая конфигурация (по умолчанию) | Требуется PostgreSQL-сервер |
| **Лучше всего для** | Личного использования, небольших команд, NAS | Команд от 50+, высокой параллельности |
| **Резервное копирование** | Копирование файла `.db` | Стандартный `pg_dump` |
| **Параллелизм** | Один писатель (достаточно для большинства случаев) | Полная мультизапись |
| **Миграция** | Н/Д | Смена провайдера + запуск приложения (автомиграция) |

## Развёртывание на NAS

MyPersonalGit отлично работает на NAS (QNAP, Synology и др.) через Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Монтирование Docker-сокета опционально — требуется только если вы хотите выполнять CI/CD рабочие процессы. Порт 2222 нужен только при включении встроенного SSH-сервера.

## Конфигурация

Все настройки можно задать в `appsettings.json`, через переменные окружения или через панель администратора по адресу `/admin`:

- Провайдер базы данных (SQLite или PostgreSQL)
- Корневая директория проектов
- Требования аутентификации
- Настройки регистрации пользователей
- Переключатели функций (Issues, Wiki, Projects, Actions)
- Максимальный размер репозитория и количество на пользователя
- Настройки SMTP для email-уведомлений
- Настройки push-уведомлений (Ntfy/Gotify)
- Встроенный SSH-сервер (включение/отключение, порт)
- Аутентификация LDAP/Active Directory (сервер, Bind DN, база поиска, фильтр пользователей, сопоставление атрибутов, группа администраторов)
- Конфигурация OAuth/SSO провайдеров (Client ID/Secret для каждого провайдера)

## Структура проекта

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor-страницы (Home, RepoDetails, Issues, PRs, Packages и др.)
  Controllers/       # REST API эндпоинты (NuGet, npm, Generic, Registry и др.)
  Data/              # EF Core DbContext, реализации сервисов
  Models/            # Доменные модели
  Migrations/        # Миграции EF Core
  Services/          # Middleware (аутентификация, Git HTTP backend, Pages, Registry auth)
    SshServer/       # Встроенный SSH-сервер (протокол SSH2, ECDH, AES-CTR)
  Program.cs         # Запуск приложения, DI, конвейер middleware
MyPersonalGit.Tests/
  UnitTest1.cs       # xUnit тесты с InMemory базой данных
```

## Запуск тестов

```bash
dotnet test
```

## Лицензия

MIT
