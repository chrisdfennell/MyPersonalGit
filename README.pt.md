🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

Um servidor Git auto-hospedado com interface web semelhante ao GitHub, construído com ASP.NET Core e Blazor Server. Navegue por repositórios, gerencie issues, pull requests, wikis, projetos e muito mais — tudo a partir da sua própria máquina ou servidor.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## Índice

- [Funcionalidades](#funcionalidades)
- [Stack Tecnológica](#stack-tecnológica)
- [Início Rápido](#início-rápido)
  - [Docker (Recomendado)](#docker-recomendado)
  - [Executar Localmente](#executar-localmente)
  - [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Uso](#uso)
  - [Entrar](#1-entrar)
  - [Criar um Repositório](#2-criar-um-repositório)
  - [Clonar e Enviar](#3-clonar-e-enviar)
  - [Clonar de uma IDE](#4-clonar-de-uma-ide)
  - [Editor Web](#5-usar-o-editor-web)
  - [Registro de Contêineres](#6-registro-de-contêineres)
  - [Registro de Pacotes](#7-registro-de-pacotes)
  - [Pages (Sites Estáticos)](#8-pages-hospedagem-de-sites-estáticos)
  - [Notificações Push](#9-notificações-push)
  - [Autenticação por Chave SSH](#10-autenticação-por-chave-ssh)
  - [LDAP / Active Directory](#11-autenticação-ldap--active-directory)
  - [Segredos do Repositório](#12-segredos-do-repositório)
  - [Login OAuth / SSO](#13-login-oauth--sso)
  - [Importar Repositório](#14-importar-repositório)
  - [Fork e Sincronização Upstream](#15-fork-e-sincronização-upstream)
  - [CI/CD Auto-Release](#16-cicd-auto-release)
  - [Feeds RSS/Atom](#17-feeds-rssatom)
- [Configuração do Banco de Dados](#configuração-do-banco-de-dados)
  - [Usando PostgreSQL](#usando-postgresql)
  - [Alternando pelo Painel Admin](#alternando-pelo-painel-admin)
  - [Escolhendo um Banco de Dados](#escolhendo-um-banco-de-dados)
- [Implantar em um NAS](#implantar-em-um-nas)
- [Configuração](#configuração)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Executando Testes](#executando-testes)
- [Licença](#licença)

---

## Funcionalidades

### Código e Repositórios
- **Gerenciamento de Repositórios** — Crie, navegue e exclua repositórios Git com navegador de código completo, editor de arquivos, histórico de commits, branches e tags
- **Importação/Migração de Repositórios** — Importe repositórios do GitHub, GitLab, Bitbucket, Gitea/Forgejo/Gogs ou qualquer URL Git com importação opcional de issues e PRs. Processamento em segundo plano com acompanhamento de progresso
- **Arquivamento de Repositórios** — Marque repositórios como somente leitura com badges visuais; pushes são bloqueados para repositórios arquivados
- **Git Smart HTTP** — Clone, fetch e push via HTTP com Basic Auth
- **Servidor SSH Integrado** — Servidor SSH nativo para operações Git — sem necessidade de OpenSSH externo. Suporta troca de chaves ECDH, criptografia AES-CTR e autenticação por chave pública (RSA, ECDSA, Ed25519)
- **Autenticação por Chave SSH** — Adicione chaves públicas SSH à sua conta e autentique operações Git via SSH com gerenciamento automático de `authorized_keys` (ou o servidor SSH integrado)
- **Forks e Sincronização Upstream** — Faça fork de repositórios, sincronize forks com upstream com um clique e veja relacionamentos de fork na interface
- **Git LFS** — Suporte a Large File Storage para rastreamento de arquivos binários
- **Espelhamento de Repositórios** — Espelhe repositórios de/para remotos Git externos
- **Visualização de Comparação** — Compare branches com contagens de commits à frente/atrás e renderização completa de diff
- **Estatísticas de Linguagem** — Barra de distribuição de linguagens estilo GitHub em cada página de repositório
- **Proteção de Branch** — Regras configuráveis para revisões obrigatórias, verificações de status, prevenção de force-push e aplicação de aprovação CODEOWNERS
- **Commits assinados obrigatórios** — Regra de proteção de branch que exige que todos os commits sejam assinados com GPG antes do merge
- **Proteção de Tags** — Proteja tags contra exclusão, atualizações forçadas e criação não autorizada com correspondência de padrões glob e listas de permissão por usuário
- **Verificação de Assinatura de Commits** — Verificação de assinatura GPG em commits e tags anotadas com badges "Verificado" / "Assinado" na interface
- **Labels de Repositório** — Gerencie labels com cores personalizadas por repositório; labels são automaticamente copiadas ao criar repositórios a partir de templates
- **AGit Flow** — Fluxo de trabalho push-to-review: `git push origin HEAD:refs/for/main` cria um pull request sem fazer fork ou criar branches remotos. Pushes subsequentes atualizam PRs abertos existentes
- **Explorar** — Navegue por todos os repositórios acessíveis com busca, ordenação e filtragem por tópicos
- **Autolink References** — Converte automaticamente `#123` em links de issues, além de padrões personalizados configuráveis (por exemplo, `JIRA-456` → URLs externas) por repositório
- **Busca** — Busca de texto completo em repositórios, issues, PRs e código

### Colaboração
- **Issues e Pull Requests** — Crie, comente, feche/reabra issues e PRs com labels, múltiplos responsáveis, datas de vencimento e revisões. Mescle PRs com estratégias de merge commit, squash ou rebase. Resolução de conflitos de merge baseada na web com visualização de diff lado a lado
- **Dependências de Issues** — Defina relacionamentos "bloqueado por" e "bloqueia" entre issues com detecção de dependências circulares
- **Fixação e Bloqueio de Issues** — Fixe issues importantes no topo da lista e bloqueie conversas para impedir comentários adicionais
- **Edição e Exclusão de Comentários** — Edite ou exclua seus próprios comentários em issues e pull requests com indicador "(editado)"
- **Resolução de Conflitos de Merge** — Resolva conflitos de merge diretamente no navegador com um editor visual mostrando visualizações base/nosso/deles, botões de aceitação rápida e validação de marcadores de conflito
- **Discussões** — Conversas encadeadas por repositório no estilo GitHub Discussions com categorias (Geral, Perguntas e Respostas, Anúncios, Ideias, Mostra e Conta, Enquetes), fixar/bloquear, marcar como resposta e votos positivos
- **Sugestões de Revisão de Código** — Modo "Sugerir alterações" em revisões inline de PR permite que revisores proponham substituições de código diretamente no diff
- **Image Diff** — Comparação de imagens lado a lado em pull requests com controle deslizante de opacidade para diff visual de imagens alteradas (PNG, JPG, GIF, SVG, WebP)
- **File Tree em PRs** — Barra lateral com árvore de arquivos recolhível na visualização de diff de pull requests para navegação fácil entre arquivos alterados
- **Marcar arquivos como vistos** — Acompanhamento do progresso de revisão em pull requests com caixas de seleção "Visto" por arquivo e um contador de progresso
- **Destaque de sintaxe em Diffs** — Coloração de sintaxe baseada em linguagem em diffs de pull requests e comparações via Prism.js
- **Reações com Emoji** — Reaja a issues, PRs, discussões e comentários com polegar para cima/baixo, coração, risada, comemoração, confuso, foguete e olhos
- **Auto-Merge** — Ative o auto-merge em pull requests para mesclar automaticamente quando todas as verificações de status obrigatórias passarem e as revisões forem aprovadas
- **Cherry-Pick / Revert via UI** — Faça cherry-pick de qualquer commit para outro branch ou reverta um commit, diretamente ou como um novo pull request, pela interface web
- **Transfer Issues** — Mova issues entre repositórios, preservando título, corpo, comentários, labels correspondentes e vinculando o original com uma nota de transferência
- **CODEOWNERS** — Atribuição automática de revisores de PR com base em caminhos de arquivo com aplicação opcional exigindo aprovação do CODEOWNERS antes do merge
- **Templates de Repositório** — Crie novos repositórios a partir de templates com cópia automática de arquivos, labels, templates de issues e regras de proteção de branch
- **Issues Rascunho e Templates de Issues** — Crie issues rascunho (trabalho em andamento) e defina templates de issues reutilizáveis (relatório de bug, solicitação de recurso) por repositório com labels padrão
- **Wiki** — Páginas wiki baseadas em Markdown por repositório com histórico de revisões
- **Projetos** — Quadros Kanban com cartões arrastar e soltar para organizar o trabalho
- **Snippets** — Compartilhe trechos de código (como GitHub Gists) com destaque de sintaxe e múltiplos arquivos
- **Organizações e Equipes** — Crie organizações com membros e equipes, atribua permissões de equipe a repositórios
- **Permissões Granulares** — Modelo de permissões em cinco níveis (Leitura, Triagem, Escrita, Manutenção, Admin) para controle de acesso refinado em repositórios
- **Marcos** — Acompanhe o progresso de issues em direção a marcos com barras de progresso e datas de vencimento
- **Comentários em Commits** — Comente em commits individuais com referências opcionais de arquivo/linha
- **Tópicos de Repositório** — Marque repositórios com tópicos para descoberta e filtragem na página Explorar

### CI/CD e DevOps
- **Runner CI/CD** — Defina workflows em `.github/workflows/*.yml` e execute-os em contêineres Docker. Disparo automático em eventos de push e pull request
- **Compatibilidade com GitHub Actions** — O mesmo YAML de workflow funciona tanto no MyPersonalGit quanto no GitHub Actions. Traduz ações `uses:` (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) em comandos shell equivalentes
- **Jobs Paralelos com `needs:`** — Jobs declaram dependências via `needs:` e executam em paralelo quando independentes. Jobs dependentes aguardam seus pré-requisitos e são automaticamente cancelados se uma dependência falhar
- **Steps Condicionais (`if:`)** — Steps suportam expressões `if:`: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Steps de limpeza com `if: failure()` ou `if: always()` ainda executam após falhas anteriores
- **Saídas de Steps (`$GITHUB_OUTPUT`)** — Steps podem escrever pares `key=value` ou `key<<DELIMITER` multilinha em `$GITHUB_OUTPUT` e steps subsequentes os recebem como variáveis de ambiente, compatível com a sintaxe `${{ steps.X.outputs.Y }}`
- **Contexto `github`** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` e `CI=true` são automaticamente injetados em cada job
- **Builds com Matrix** — `strategy.matrix` expande jobs em múltiplas combinações de variáveis (ex.: SO x versão). Suporta `fail-fast` e substituição `${{ matrix.X }}` em `runs-on`, comandos de steps e nomes de steps
- **Inputs `workflow_dispatch`** — Disparos manuais com parâmetros de entrada tipados (string, boolean, choice, number). A interface mostra um formulário de entrada ao disparar workflows com inputs. Valores injetados como variáveis de ambiente `INPUT_*`
- **Timeouts de Jobs (`timeout-minutes`)** — Defina `timeout-minutes` em jobs para falhar automaticamente se excederem o limite. Padrão: 360 minutos (compatível com GitHub Actions)
- **`if:` em Nível de Job** — Pule jobs inteiros com base em condições. Jobs com `if: always()` executam mesmo quando dependências falham. Jobs pulados não falham a execução
- **Saídas de Jobs** — Jobs declaram `outputs:` que jobs downstream com `needs:` consomem via `${{ needs.X.outputs.Y }}`. Saídas são resolvidas a partir das saídas de steps após a conclusão do job
- **`continue-on-error`** — Marque steps individuais como permitido falhar sem falhar o job. Útil para steps de validação ou notificação opcionais
- **Filtro `on.push.paths`** — Dispare workflows apenas quando arquivos específicos mudam. Suporta padrões glob (`src/**`, `*.ts`) e `paths-ignore:` para exclusões
- **Reexecutar Workflows** — Reexecute workflows com falha, sucesso ou cancelados com um clique na interface Actions. Cria uma nova execução com a mesma configuração
- **`working-directory`** — Defina `defaults.run.working-directory` no nível do workflow ou `working-directory:` por step para controlar onde comandos executam
- **`defaults.run.shell`** — Configure shell personalizado por workflow ou por step (`bash`, `sh`, `python3`, etc.)
- **`strategy.max-parallel`** — Limite a execução concorrente de jobs com matrix
- **Reusable Workflows (`workflow_call`)** — Defina workflows com `on: workflow_call` que outros workflows podem invocar com `uses: ./.github/workflows/build.yml`. Suporta entradas, saídas e segredos tipados. Os jobs do workflow chamado são incorporados no chamador
- **Composite Actions** — Defina ações de múltiplos passos em `.github/actions/{name}/action.yml` com `runs: using: composite`. Os passos das ações compostas são expandidos inline durante a execução
- **Environment Deployments** — Configure ambientes de implantação (ex., `staging`, `production`) com regras de proteção: revisores obrigatórios, temporizadores de espera e restrições de branch. Jobs de workflow com `environment:` requerem aprovação antes da execução. Histórico completo de implantações com interface de aprovação/rejeição
- **`on.workflow_run`** — Encadeie workflows: dispare o workflow B quando o workflow A completar. Filtre por nome de workflow e `types: [completed]`
- **Criação Automática de Release** — `softprops/action-gh-release` cria entidades de Release reais com tag, título, corpo de changelog e flags de pré-release/rascunho. Arquivos de código-fonte (ZIP e TAR.GZ) são automaticamente anexados como assets para download
- **Pipeline de Auto-Release** — Workflow integrado que auto-tageia versões, gera changelogs e envia imagens Docker para o Docker Hub em cada push para main
- **Verificações de Status de Commit** — Workflows automaticamente definem status pending/success/failure em commits, visíveis em pull requests
- **Cancelamento de Workflow** — Cancele workflows em execução ou na fila pela interface Actions
- **Controles de Concorrência** — Novos pushes automaticamente cancelam execuções em fila do mesmo workflow
- **Variáveis de Ambiente de Workflow** — Defina `env:` no nível de workflow, job ou step no YAML
- **Badges de Status** — Badges SVG embutíveis para status de workflow e commit (`/api/badge/{repo}/workflow`)
- **Download de Artefatos** — Baixe artefatos de build diretamente da interface Actions
- **Gerenciamento de Segredos** — Segredos de repositório criptografados (AES-256) injetados como variáveis de ambiente em execuções de workflows CI/CD
- **Webhooks** — Dispare serviços externos em eventos de repositório
- **Métricas Prometheus** — Endpoint `/metrics` integrado para monitoramento

### Hospedagem de Pacotes e Contêineres (20 registries)
- **Registro de Contêineres** — Hospede imagens Docker/OCI com `docker push` e `docker pull` (OCI Distribution Spec)
- **Registro NuGet** — Hospede pacotes .NET com API NuGet v3 completa (índice de serviço, busca, push, restauração)
- **Registro npm** — Hospede pacotes Node.js com npm publish/install padrão
- **Registro PyPI** — Hospede pacotes Python com PEP 503 Simple API, API de metadados JSON e compatibilidade com `twine upload`
- **Registro Maven** — Hospede pacotes Java/JVM com layout de repositório Maven padrão, geração de `maven-metadata.xml` e suporte a `mvn deploy`
- **Alpine Registry** — Hospede pacotes Alpine Linux `.apk` com geração de APKINDEX
- **RPM Registry** — Hospede pacotes RPM com metadados `repomd.xml` para `dnf`/`yum`
- **Chef Registry** — Hospede cookbooks Chef com API compatível com Chef Supermarket
- **Pacotes Genéricos** — Faça upload e download de artefatos binários arbitrários via REST API

### Sites Estáticos
- **Pages** — Sirva sites estáticos diretamente de uma branch do repositório (como GitHub Pages) em `/pages/{owner}/{repo}/`

### Feeds RSS/Atom
- **Feeds de Repositório** — Feeds Atom para commits, releases e tags por repositório (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Feed de Atividade do Usuário** — Feed de atividade por usuário (`/api/feeds/users/{username}/activity.atom`)
- **Feed de Atividade Global** — Feed de atividade de todo o site (`/api/feeds/global/activity.atom`)

### Notificações
- **Notificações no Aplicativo** — Menções, comentários e atividade do repositório
- **Notificações Push** — Integração Ntfy e Gotify para alertas em tempo real no celular/desktop com opt-in por usuário

### Autenticação
- **OAuth2 / SSO** — Entre com GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord ou Twitter/X. Administradores configuram Client ID e Secret por provedor no painel Admin — apenas provedores com credenciais preenchidas são exibidos aos usuários
- **Provedor OAuth2** — Atue como provedor de identidade para que outros aplicativos possam usar "Entrar com MyPersonalGit". Implementa fluxo Authorization Code com PKCE, atualização de token, endpoint userinfo e descoberta OpenID Connect (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Autentique usuários contra um diretório LDAP ou domínio Active Directory. Usuários são provisionados automaticamente no primeiro login com atributos sincronizados (email, nome de exibição). Suporta promoção a admin baseada em grupo, SSL/TLS e StartTLS
- **SSPI / Autenticação Integrada Windows** — Single Sign-On transparente para usuários de domínio Windows via Negotiate/NTLM. Usuários em um domínio são autenticados automaticamente sem inserir credenciais. Ative em Admin > Configurações (apenas Windows)
- **Autenticação de Dois Fatores** — 2FA baseado em TOTP com suporte a aplicativo autenticador e códigos de recuperação
- **WebAuthn / Passkeys** — Suporte a chaves de segurança de hardware FIDO2 e passkeys como segundo fator. Registre YubiKeys, autenticadores de plataforma (Face ID, Windows Hello, Touch ID) e outros dispositivos FIDO2. Verificação de contagem de assinaturas para detecção de chaves clonadas
- **Contas Vinculadas** — Usuários podem vincular múltiplos provedores OAuth à sua conta em Configurações

### Administração
- **Painel Admin** — Configurações do sistema (incluindo provedor de banco de dados, servidor SSH, LDAP/AD, páginas de rodapé), gerenciamento de usuários, logs de auditoria e estatísticas
- **Páginas de Rodapé Personalizáveis** — Termos de Serviço, Política de Privacidade, Documentação e páginas de Contato com conteúdo Markdown editável em Admin > Configurações
- **Perfis de Usuário** — Mapa de calor de contribuições, feed de atividade e estatísticas por usuário
- **Tokens de Acesso Pessoal** — Autenticação de API baseada em tokens com escopos configuráveis e restrições opcionais no nível de rota (padrões glob como `/api/packages/**` para limitar o acesso do token a caminhos de API específicos)
- **Backup e Restauração** — Exporte e importe dados do servidor
- **Varredura de Segurança** — Varredura real de vulnerabilidades de dependências alimentada pelo banco de dados [OSV.dev](https://osv.dev/). Extrai automaticamente dependências de `.csproj` (NuGet), `package.json` (npm), `requirements.txt` (PyPI), `Cargo.toml` (Rust), `Gemfile` (Ruby), `composer.json` (PHP), `go.mod` (Go), `pom.xml` (Maven/Java) e `pubspec.yaml` (Dart/Flutter), verificando cada uma contra CVEs conhecidos. Reporta severidade, versões corrigidas e links de consultoria. Além de consultoria de segurança manual com fluxo de trabalho rascunho/publicar/fechar
- **Secret Scanning** — Escaneia automaticamente cada push em busca de credenciais vazadas (chaves AWS, tokens GitHub/GitLab, tokens Slack, chaves privadas, chaves API, JWTs, strings de conexão e mais). 20 padrões integrados com suporte completo a regex. Escaneamento completo do repositório sob demanda. Alertas com fluxo de trabalho resolver/falso positivo. Padrões personalizados configuráveis via API
- **Dependabot-Style Auto-Update PRs** — Verifica automaticamente dependências desatualizadas e cria pull requests para atualizá-las. Suporta ecossistemas NuGet, npm e PyPI. Agendamento configurável (diário/semanal/mensal) e limite de PRs abertas por repositório
- **Repository Insights (Traffic)** — Acompanhe contagens de clone/fetch, visualizações de página, visitantes únicos, principais referenciadores e caminhos de conteúdo populares. Gráficos de tráfego na aba Insights com resumos de 14 dias. Agregação diária com retenção de 90 dias. Endereços IP são criptografados por hash para privacidade
- **Modo Escuro** — Suporte completo a modo escuro/claro com alternador no cabeçalho
- **Multi-idioma / i18n** — Localização completa em todas as 28 páginas com 836 chaves de recurso. Inclui 11 idiomas: inglês, espanhol, francês, alemão, japonês, coreano, chinês (simplificado), português, russo, italiano e turco. Seletor de idioma no cabeçalho. Adicione mais criando arquivos `SharedResource.{locale}.resx`
- **Swagger / OpenAPI** — Documentação interativa da API em `/swagger` com todos os endpoints REST descobríveis e testáveis
- **Mermaid Diagrams** — Renderização de diagramas Mermaid em arquivos Markdown (fluxogramas, diagramas de sequência, gráficos de Gantt, etc.)
- **Math Rendering** — Expressões matemáticas LaTeX/KaTeX em Markdown (sintaxe `$inline$` e `$$display$$`)
- **CSV/TSV Viewer** — Arquivos CSV e TSV são renderizados como tabelas formatadas e ordenáveis em vez de texto bruto
- **Jupyter Notebook Rendering** — Arquivos `.ipynb` são renderizados como notebooks formatados com células de código, Markdown, saídas e imagens inline
- **Repository Transfer** — Transfira a propriedade do repositório para outro usuário ou organização nas Configurações do repositório
- **Default Branch Configuration** — Altere a branch padrão por repositório na aba Configurações
- **Rename Repository** — Renomeie um repositório em Settings com atualização automática de todas as referências (issues, PRs, estrelas, webhooks, secrets, etc.)
- **User-Level Secrets** — Secrets criptografados compartilhados entre todos os repositórios de um usuário, gerenciados em Settings > Secrets
- **Organization-Level Secrets** — Secrets criptografados compartilhados entre todos os repositórios de uma organização, gerenciados na aba Secrets da organização
- **Repository Pinning** — Fixe até 6 repositórios favoritos na sua página de perfil de usuário para acesso rápido
- **Git Hooks Management** — Interface web para visualizar, editar e gerenciar Git hooks do lado do servidor (pre-receive, update, post-receive, post-update, pre-push) por repositório
- **Protected File Patterns** — Regra de proteção de branch com padrões glob para exigir aprovação de revisão para alterações em arquivos específicos (por exemplo, `*.lock`, `migrations/**`, `.github/workflows/*`)
- **External Issue Tracker** — Configure repositórios para vincular a um rastreador de issues externo (Jira, Linear, etc.) com padrões de URL personalizados
- **Federation (NodeInfo/WebFinger)** — Descoberta NodeInfo 2.0, WebFinger e host-meta para descoberta entre instâncias
- **Distributed CI Runners** — Runners externos podem se registrar via API, consultar jobs na fila e reportar resultados

## Stack Tecnológica

| Componente | Tecnologia |
|------------|------------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (renderização interativa do lado do servidor) |
| Banco de Dados | SQLite (padrão) ou PostgreSQL via Entity Framework Core 10 |
| Motor Git | LibGit2Sharp |
| Autenticação | Hash de senha BCrypt, autenticação baseada em sessão, tokens PAT, OAuth2 (8 provedores + modo provedor), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| Servidor SSH | Implementação integrada do protocolo SSH2 (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Monitoramento | Métricas Prometheus |

## Início Rápido

### Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) (recomendado)
- Ou [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git para desenvolvimento local

### Docker (Recomendado)

Baixe do Docker Hub e execute:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> A porta 2222 é opcional — necessária apenas se você habilitar o servidor SSH integrado em Admin > Configurações.

Ou use Docker Compose:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

O aplicativo estará disponível em **http://localhost:8080**.

> **Credenciais padrão**: `admin` / `admin`
>
> **Altere a senha padrão imediatamente** pelo painel Admin após o primeiro login.

### Executar Localmente

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

O aplicativo inicia em **http://localhost:5146**.

### Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `Database__Provider` | Motor de banco de dados: `sqlite` ou `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | String de conexão do banco de dados | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Diretório onde os repositórios Git são armazenados | `/repos` |
| `Git__RequireAuth` | Exigir autenticação para operações Git HTTP | `true` |
| `Git__Users__<username>` | Definir senha para usuário Git HTTP Basic Auth | — |
| `RESET_ADMIN_PASSWORD` | Reset de emergência da senha do admin na inicialização | — |
| `Secrets__EncryptionKey` | Chave de criptografia personalizada para segredos do repositório | Derivada da string de conexão do BD |
| `Ssh__DataDir` | Diretório para dados SSH (chaves do host, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Caminho para o arquivo authorized_keys gerado | `<DataDir>/authorized_keys` |

> **Nota:** A porta do servidor SSH integrado e as configurações LDAP são definidas pelo painel Admin (Admin > Configurações), não por variáveis de ambiente. Isso permite alterá-las sem reimplantar.

## Uso

### 1. Entrar

Abra o aplicativo e clique em **Entrar**. Em uma instalação nova, use as credenciais padrão (`admin` / `admin`). Crie usuários adicionais pelo painel **Admin** ou habilitando o registro de usuários em Admin > Configurações.

### 2. Criar um Repositório

Clique no botão verde **Novo** na página inicial, insira um nome e clique em **Criar**. Isso cria um repositório Git bare no servidor que você pode clonar, enviar commits e gerenciar pela interface web.

### 3. Clonar e Enviar

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Se a autenticação Git HTTP estiver habilitada, será solicitada a senha configurada via variáveis de ambiente `Git__Users__<username>`. Essas credenciais são separadas do login da interface web.

### 4. Clonar de uma IDE

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > cole `http://localhost:8080/git/MyRepo.git`

**Visual Studio**: **Git > Clonar Repositório** > cole a URL

**JetBrains**: **Arquivo > Novo > Projeto do Controle de Versão** > cole a URL

### 5. Usar o Editor Web

Você pode editar arquivos diretamente no navegador:
- Navegue até um repositório e clique em qualquer arquivo, depois clique em **Editar**
- Use **Adicionar arquivos > Criar novo arquivo** para adicionar arquivos sem clone local
- Use **Adicionar arquivos > Enviar arquivos/pasta** para fazer upload da sua máquina

### 6. Registro de Contêineres

Envie e baixe imagens Docker/OCI diretamente para o seu servidor:

```bash
# Faça login (use um Token de Acesso Pessoal de Configurações > Tokens de Acesso)
docker login localhost:8080 -u youruser

# Envie uma imagem
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Baixe uma imagem
docker pull localhost:8080/myapp:v1
```

> **Nota:** Docker requer HTTPS por padrão. Para HTTP, adicione seu servidor ao `insecure-registries` do Docker em `~/.docker/daemon.json`:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Registro de Pacotes

**NuGet (pacotes .NET):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (pacotes Node.js):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (pacotes Python):**
```bash
# Instalar um pacote
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# Enviar com twine
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

**Maven (pacotes Java/JVM):**
```xml
<!-- No seu pom.xml, adicione o repositório -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- No settings.xml, adicione as credenciais -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Genérico (qualquer binário):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Navegue por todos os pacotes em `/packages` na interface web.

### 8. Pages (Hospedagem de Sites Estáticos)

Sirva sites estáticos a partir de uma branch do repositório:

1. Vá até a aba **Configurações** do seu repositório e habilite **Pages**
2. Defina a branch (padrão: `gh-pages`)
3. Envie HTML/CSS/JS para essa branch
4. Visite `http://localhost:8080/pages/{username}/{repo}/`

### 9. Notificações Push

Configure Ntfy ou Gotify em **Admin > Configurações do Sistema** para receber notificações push no celular ou desktop quando issues, PRs ou comentários são criados. Usuários podem ativar/desativar em **Configurações > Notificações**.

### 10. Autenticação por Chave SSH

Use chaves SSH para operações Git sem senha. Existem duas opções:

#### Opção A: Servidor SSH Integrado (Recomendado)

Nenhum daemon SSH externo necessário — o MyPersonalGit executa seu próprio servidor SSH:

1. Vá em **Admin > Configurações** e habilite **Servidor SSH Integrado**
2. Defina a porta SSH (padrão: 2222) — use 22 se não estiver executando SSH do sistema
3. Salve as configurações e reinicie o servidor (mudanças de porta requerem reinicialização)
4. Vá em **Configurações > Chaves SSH** e adicione sua chave pública (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` ou `~/.ssh/id_ecdsa.pub`)
5. Clone via SSH:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

O servidor SSH integrado suporta troca de chaves ECDH-SHA2-NISTP256, criptografia AES-128/256-CTR, HMAC-SHA2-256 e autenticação por chave pública com chaves Ed25519, RSA e ECDSA.

#### Opção B: OpenSSH do Sistema

Se preferir usar o daemon SSH do sistema:

1. Vá em **Configurações > Chaves SSH** e adicione sua chave pública
2. O MyPersonalGit mantém automaticamente um arquivo `authorized_keys` de todas as chaves SSH registradas
3. Configure o OpenSSH do seu servidor para usar o arquivo authorized_keys gerado:
   ```
   # Em /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Clone via SSH:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

O serviço de autenticação SSH também expõe uma API em `/api/ssh/authorized-keys` para uso com a diretiva `AuthorizedKeysCommand` do OpenSSH.

### 11. Autenticação LDAP / Active Directory

Autentique usuários contra o diretório LDAP ou domínio Active Directory da sua organização:

1. Vá em **Admin > Configurações** e role até **Autenticação LDAP / Active Directory**
2. Habilite LDAP e preencha os detalhes do servidor:
   - **Servidor**: Nome do host do seu servidor LDAP (ex.: `dc01.corp.local`)
   - **Porta**: 389 para LDAP, 636 para LDAPS
   - **SSL/TLS**: Habilite para LDAPS, ou use StartTLS para atualizar uma conexão simples
3. Configure uma conta de serviço para busca de usuários:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Senha do Bind**: A senha da conta de serviço
4. Defina os parâmetros de busca:
   - **Base DN de Busca**: `OU=Users,DC=corp,DC=local`
   - **Filtro de Usuário**: `(sAMAccountName={0})` para AD, `(uid={0})` para OpenLDAP
5. Mapeie atributos LDAP para campos do usuário:
   - **Nome de Usuário**: `sAMAccountName` (AD) ou `uid` (OpenLDAP)
   - **Email**: `mail`
   - **Nome de Exibição**: `displayName`
6. Opcionalmente defina um **DN de Grupo Admin** — membros deste grupo são automaticamente promovidos a admin
7. Clique em **Testar Conexão LDAP** para verificar as configurações
8. Salve as configurações

Usuários agora podem entrar com suas credenciais de domínio na página de login. No primeiro login, uma conta local é criada automaticamente com atributos sincronizados do diretório. A autenticação LDAP também é usada para operações Git HTTP (clone/push).

### 12. Segredos do Repositório

Adicione segredos criptografados a repositórios para uso em workflows CI/CD:

1. Vá até a aba **Configurações** do seu repositório
2. Role até o cartão **Segredos** e clique em **Adicionar segredo**
3. Insira um nome (ex.: `DEPLOY_TOKEN`) e valor — o valor é criptografado com AES-256
4. Segredos são automaticamente injetados como variáveis de ambiente em cada execução de workflow

Referencie segredos no seu workflow:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. Login OAuth / SSO

Entre com provedores de identidade externos:

1. Vá em **Admin > OAuth / SSO** e configure os provedores que deseja habilitar
2. Insira o **Client ID** e **Client Secret** do console de desenvolvedor do provedor
3. Marque **Habilitar** — apenas provedores com ambas as credenciais preenchidas aparecerão na página de login
4. A URL de callback para cada provedor é exibida no painel admin (ex.: `https://yourserver/oauth/callback/github`)

Provedores suportados: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Usuários podem vincular múltiplos provedores à sua conta em **Configurações > Contas Vinculadas**.

### 14. Importar Repositório

Importe repositórios de fontes externas com histórico completo:

1. Clique em **Importar** na página inicial
2. Selecione um tipo de fonte (URL Git, GitHub, GitLab ou Bitbucket)
3. Insira a URL do repositório e opcionalmente um token de autenticação para repositórios privados
4. Para importações do GitHub/GitLab/Bitbucket, opcionalmente importe issues e pull requests
5. Acompanhe o progresso da importação em tempo real na página de Importação

### 15. Fork e Sincronização Upstream

Faça fork de um repositório e mantenha-o sincronizado:

1. Clique no botão **Fork** em qualquer página de repositório
2. Um fork é criado sob seu nome de usuário com um link de volta ao original
3. Clique em **Sincronizar fork** ao lado do badge "forked from" para puxar as últimas alterações do upstream

### 16. CI/CD Auto-Release

O MyPersonalGit inclui um pipeline CI/CD integrado que auto-tageia, faz release e envia imagens Docker em cada push para main. Workflows são disparados automaticamente no push — nenhum serviço CI externo necessário.

**Como funciona:**
1. Push para `main` dispara automaticamente `.github/workflows/release.yml`
2. Incrementa a versão de patch (`v1.15.1` -> `v1.15.2`), cria uma tag git
3. Faz login no Docker Hub, constrói a imagem e envia como `:latest` e `:vX.Y.Z`

**Configuração:**
1. Vá em **Configurações > Segredos** do seu repositório no MyPersonalGit
2. Adicione um segredo chamado `DOCKERHUB_TOKEN` com seu token de acesso do Docker Hub
3. Certifique-se de que o contêiner MyPersonalGit tenha o socket Docker montado (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Envie para main — o workflow dispara automaticamente

**Compatibilidade com GitHub Actions:**
O mesmo YAML de workflow também funciona no GitHub Actions — sem alterações necessárias. O MyPersonalGit traduz ações `uses:` em comandos shell equivalentes em tempo de execução:

| GitHub Action | Tradução MyPersonalGit |
|---|---|
| `actions/checkout@v4` | Repositório já clonado em `/workspace` |
| `actions/setup-dotnet@v4` | Instala .NET SDK via script de instalação oficial |
| `actions/setup-node@v4` | Instala Node.js via NodeSource |
| `actions/setup-python@v5` | Instala Python via apt/apk |
| `actions/setup-java@v4` | Instala OpenJDK via apt/apk |
| `docker/login-action@v3` | `docker login` com senha via stdin |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | Nenhuma operação (usa builder padrão) |
| `softprops/action-gh-release@v2` | Cria uma entidade Release real no banco de dados |
| `${{ secrets.X }}` | Variável de ambiente `$X` |
| `${{ steps.X.outputs.Y }}` | Variável de ambiente `$Y` |
| `${{ github.sha }}` | Variável de ambiente `$GITHUB_SHA` |

**Jobs paralelos:**
Jobs executam em paralelo por padrão. Use `needs:` para declarar dependências:
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
Jobs sem `needs:` iniciam imediatamente. Um job é cancelado se qualquer uma de suas dependências falhar.

**Steps condicionais:**
Use `if:` para controlar quando steps executam:
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
Expressões suportadas: `always()`, `success()` (padrão), `failure()`, `cancelled()`, `true`, `false`.

**Saídas de steps:**
Steps podem passar valores para steps subsequentes via `$GITHUB_OUTPUT`:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Builds com matrix:**
Expanda jobs em múltiplas combinações usando `strategy.matrix`:
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
Isso cria 4 jobs: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)`, etc. Todos executam em paralelo.

**Disparos manuais com inputs (`workflow_dispatch`):**
Defina inputs tipados que aparecem como formulário na interface ao disparar manualmente:
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
Valores de input são injetados como variáveis de ambiente `INPUT_<NAME>` (em maiúsculas).

**Timeouts de jobs:**
Defina `timeout-minutes` em jobs para falhar automaticamente se executarem por muito tempo:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Timeout padrão é 360 minutos (6 horas), compatível com GitHub Actions.

**Condicionais em nível de job:**
Use `if:` em jobs para pulá-los com base em condições:
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

**Saídas de jobs:**
Jobs podem passar valores para jobs downstream via `outputs:`:
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

**Continuar com erro:**
Permita que um step falhe sem falhar o job:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Filtragem por caminho:**
Dispare workflows apenas quando arquivos específicos mudam:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # ou use paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Diretório de trabalho:**
Defina onde comandos executam:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # executa em src/app
      - run: npm test
        working-directory: tests  # sobrescreve o padrão
```

**Reexecutar workflows:**
Clique no botão **Reexecutar** em qualquer execução de workflow concluída, com falha ou cancelada para criar uma nova execução com os mesmos jobs, steps e configuração.

**Workflows de pull request:**
Workflows com `on: pull_request` disparam automaticamente quando um PR não-rascunho é criado, executando verificações contra a branch de origem.

**Verificações de status de commit:**
Workflows automaticamente definem status de commit (pending/success/failure) para que você possa ver resultados de build em PRs e impor verificações obrigatórias via proteção de branch.

**Cancelamento de workflow:**
Clique no botão **Cancelar** em qualquer workflow em execução ou na fila na interface Actions para pará-lo imediatamente.

**Badges de status:**
Incorpore badges de status de build no seu README ou em qualquer lugar:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Filtre por nome de workflow: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. Feeds RSS/Atom

Assine a atividade do repositório usando feeds Atom padrão em qualquer leitor RSS:

```
# Commits do repositório
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Releases do repositório
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Tags do repositório
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Atividade do usuário
http://localhost:8080/api/feeds/users/admin/activity.atom

# Atividade global (todos os repositórios)
http://localhost:8080/api/feeds/global/activity.atom
```

Nenhuma autenticação necessária para repositórios públicos. Adicione essas URLs a qualquer leitor de feeds (Feedly, Miniflux, FreshRSS, etc.) para se manter notificado sobre alterações.

## Configuração do Banco de Dados

O MyPersonalGit usa **SQLite** por padrão — configuração zero, banco de dados em arquivo único, perfeito para uso pessoal e pequenas equipes.

Para implantações maiores (muitos usuários simultâneos, alta disponibilidade ou se você já executa PostgreSQL), você pode mudar para **PostgreSQL**:

### Usando PostgreSQL

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

**Apenas variáveis de ambiente** (se você já tem um servidor PostgreSQL):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

As migrações do EF Core executam automaticamente na inicialização para ambos os provedores. Nenhuma configuração manual de esquema necessária.

### Alternando pelo Painel Admin

Você também pode alternar provedores de banco de dados diretamente pela interface web:

1. Vá em **Admin > Configurações** — o cartão **Banco de Dados** está no topo
2. Selecione **PostgreSQL** no dropdown de provedor
3. Insira sua string de conexão PostgreSQL (ex.: `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Clique em **Salvar Configurações do Banco de Dados**
5. Reinicie o aplicativo para a alteração entrar em vigor

A configuração é salva em `~/.mypersonalgit/database.json` (fora do banco de dados, para poder ser lida antes de conectar).

### Escolhendo um Banco de Dados

| | SQLite | PostgreSQL |
|---|---|---|
| **Configuração** | Zero configuração (padrão) | Requer um servidor PostgreSQL |
| **Ideal para** | Uso pessoal, pequenas equipes, NAS | Equipes de 50+, alta concorrência |
| **Backup** | Copie o arquivo `.db` | `pg_dump` padrão |
| **Concorrência** | Escritor único (suficiente para a maioria dos usos) | Multi-escritor completo |
| **Migração** | N/A | Altere o provedor + execute o app (migra automaticamente) |

## Implantar em um NAS

O MyPersonalGit funciona muito bem em um NAS (QNAP, Synology, etc.) via Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

A montagem do socket Docker é opcional — necessária apenas se você deseja execução de workflows CI/CD. A porta 2222 é necessária apenas se você habilitar o servidor SSH integrado.

## Configuração

Todas as configurações podem ser definidas em `appsettings.json`, via variáveis de ambiente ou pelo painel Admin em `/admin`:

- Provedor de banco de dados (SQLite ou PostgreSQL)
- Diretório raiz do projeto
- Requisitos de autenticação
- Configurações de registro de usuários
- Alternadores de funcionalidades (Issues, Wiki, Projetos, Actions)
- Tamanho máximo de repositório e quantidade por usuário
- Configurações SMTP para notificações por email
- Configurações de notificações push (Ntfy/Gotify)
- Servidor SSH integrado (habilitar/desabilitar, porta)
- Autenticação LDAP/Active Directory (servidor, Bind DN, base de busca, filtro de usuário, mapeamento de atributos, grupo admin)
- Configuração de provedores OAuth/SSO (Client ID/Secret por provedor)

## Estrutura do Projeto

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Páginas Blazor (Home, Detalhes do Repositório, Issues, PRs, Pacotes, etc.)
  Controllers/       # Endpoints REST API (NuGet, npm, Genérico, Registro, etc.)
  Data/              # EF Core DbContext, implementações de serviço
  Models/            # Modelos de domínio
  Migrations/        # Migrações EF Core
  Services/          # Middleware (autenticação, backend Git HTTP, Pages, autenticação de registro)
    SshServer/       # Servidor SSH integrado (protocolo SSH2, ECDH, AES-CTR)
  Program.cs         # Inicialização do app, DI, pipeline de middleware
MyPersonalGit.Tests/
  UnitTest1.cs       # Testes xUnit com banco de dados InMemory
```

## Executando Testes

```bash
dotnet test
```

## Licença

MIT
