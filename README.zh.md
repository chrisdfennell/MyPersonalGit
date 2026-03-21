🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

一个自托管的 Git 服务器，带有类似 GitHub 的 Web 界面，基于 ASP.NET Core 和 Blazor Server 构建。可以在您自己的机器或服务器上浏览仓库、管理 Issue、Pull Request、Wiki、项目等。

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## 目录

- [功能特性](#功能特性)
- [技术栈](#技术栈)
- [快速开始](#快速开始)
  - [Docker（推荐）](#docker推荐)
  - [本地运行](#本地运行)
  - [环境变量](#环境变量)
- [使用说明](#使用说明)
  - [登录](#1-登录)
  - [创建仓库](#2-创建仓库)
  - [克隆和推送](#3-克隆和推送)
  - [从 IDE 克隆](#4-从-ide-克隆)
  - [Web 编辑器](#5-使用-web-编辑器)
  - [容器注册表](#6-容器注册表)
  - [软件包注册表](#7-软件包注册表)
  - [Pages（静态站点）](#8-pages静态站点托管)
  - [推送通知](#9-推送通知)
  - [SSH 密钥认证](#10-ssh-密钥认证)
  - [LDAP / Active Directory](#11-ldap--active-directory-认证)
  - [仓库密钥](#12-仓库密钥)
  - [OAuth / SSO 登录](#13-oauth--sso-登录)
  - [导入仓库](#14-导入仓库)
  - [Fork 与上游同步](#15-fork-与上游同步)
  - [CI/CD 自动发布](#16-cicd-自动发布)
  - [RSS/Atom 订阅源](#17-rssatom-订阅源)
- [数据库配置](#数据库配置)
  - [使用 PostgreSQL](#使用-postgresql)
  - [从管理后台切换](#从管理后台切换)
  - [选择数据库](#选择数据库)
- [部署到 NAS](#部署到-nas)
- [配置](#配置)
- [项目结构](#项目结构)
- [运行测试](#运行测试)
- [许可证](#许可证)

---

## 功能特性

### 代码与仓库
- **仓库管理** — 创建、浏览和删除 Git 仓库，配备完整的代码浏览器、文件编辑器、提交历史、分支和标签
- **仓库导入/迁移** — 从 GitHub、GitLab、Bitbucket、Gitea/Forgejo/Gogs 或任意 Git URL 导入仓库，可选导入 Issue 和 PR。支持后台处理和进度跟踪
- **仓库归档** — 将仓库标记为只读并显示视觉徽章；归档仓库的推送操作将被阻止
- **Git Smart HTTP** — 通过 HTTP 使用 Basic Auth 进行克隆、拉取和推送
- **内置 SSH 服务器** — 原生 SSH 服务器用于 Git 操作——无需外部 OpenSSH。支持 ECDH 密钥交换、AES-CTR 加密和公钥认证（RSA、ECDSA、Ed25519）
- **SSH 密钥认证** — 将 SSH 公钥添加到您的账户，通过 SSH 进行 Git 操作认证，自动管理 `authorized_keys`（或使用内置 SSH 服务器）
- **Fork 与上游同步** — Fork 仓库，一键同步 Fork 与上游，在界面中查看 Fork 关系
- **Git LFS** — 大文件存储支持，用于跟踪二进制文件
- **仓库镜像** — 与外部 Git 远程仓库进行双向镜像
- **比较视图** — 比较分支，显示领先/落后的提交数量和完整差异渲染
- **语言统计** — 每个仓库页面上显示 GitHub 风格的语言分布条
- **分支保护** — 可配置的规则，包括必需审查、状态检查、禁止强制推送和 CODEOWNERS 审批强制执行
- **签名提交必需** — 分支保护规则，要求所有提交在合并前必须经过 GPG 签名
- **标签保护** — 保护标签不被删除、强制更新和未授权创建，支持 glob 模式匹配和按用户的允许列表
- **提交签名验证** — 对提交和带注释的标签进行 GPG 签名验证，在界面中显示"已验证"/"已签名"徽章
- **仓库标签** — 管理每个仓库的自定义颜色标签；从模板创建仓库时自动复制标签
- **AGit 工作流** — 推送审查工作流：`git push origin HEAD:refs/for/main` 无需 Fork 或创建远程分支即可创建 Pull Request。后续推送自动更新已有的开放 PR
- **探索** — 浏览所有可访问的仓库，支持搜索、排序和主题过滤
- **Autolink References** — 自动将 `#123` 转换为 Issue 链接，并支持按仓库配置自定义模式（例如 `JIRA-456` → 外部 URL）
- **搜索** — 跨仓库、Issue、PR 和代码的全文搜索
- **License Detection** — 自动检测 LICENSE 文件并识别常见许可证（MIT、Apache-2.0、GPL、BSD、ISC、MPL、Unlicense），在仓库侧边栏显示徽章

### 协作
- **Issue 与 Pull Request** — 创建、评论、关闭/重新打开 Issue 和 PR，支持标签、多个指派人、截止日期和审查。使用合并提交、压缩或变基策略合并 PR。基于 Web 的合并冲突解决，支持并排差异视图
- **Issue 依赖** — 定义 Issue 之间的"被阻塞"和"阻塞"关系，支持循环依赖检测
- **Issue 置顶与锁定** — 将重要 Issue 置顶到列表顶部，锁定对话以防止进一步评论
- **评论编辑与删除** — 编辑或删除您在 Issue 和 Pull Request 上的评论，显示"(已编辑)"标识
- **合并冲突解决** — 直接在浏览器中使用可视化编辑器解决合并冲突，显示 base/ours/theirs 视图、快速接受按钮和冲突标记验证
- **讨论** — GitHub Discussions 风格的每仓库分类线程对话（常规、问答、公告、想法、展示、投票），支持置顶/锁定、标记为答案和点赞
- **代码审查建议** — PR 内联审查中的"建议更改"模式允许审查者直接在差异中提出代码替换
- **Image Diff** — Pull Request 中的并排图片比较，带有不透明度滑块，用于已更改图片（PNG、JPG、GIF、SVG、WebP）的视觉差异对比
- **PR 中的 File Tree** — Pull Request diff 视图中可折叠的文件树侧边栏，方便在已更改文件之间导航
- **标记文件为已查看** — 通过每个文件的"已查看"复选框和进度计数器跟踪 Pull Request 中的审查进度
- **Diff 语法高亮** — 通过 Prism.js 在 Pull Request 和比较 diff 中实现语言感知的语法着色
- **表情反应** — 对 Issue、PR、讨论和评论使用表情反应：赞/踩、爱心、笑脸、庆祝、困惑、火箭和关注
- **Auto-Merge** — 在 Pull Request 上启用自动合并，当所有必需的状态检查通过且审查已批准时自动合并
- **Cherry-Pick / Revert via UI** — 从 Web 界面将任何提交 Cherry-pick 到另一个分支，或直接或作为新的 Pull Request 还原提交
- **Transfer Issues** — 在仓库之间移动 Issue，保留标题、正文、评论、匹配的标签，并在原始 Issue 上创建带有转移说明的链接
- **CODEOWNERS** — 根据文件路径自动分配 PR 审查者，可选强制要求 CODEOWNERS 在合并前审批
- **仓库模板** — 从模板创建新仓库，自动复制文件、标签、Issue 模板和分支保护规则
- **草稿 Issue 与 Issue 模板** — 创建草稿 Issue（进行中的工作），为每个仓库定义可重用的 Issue 模板（Bug 报告、功能请求），支持默认标签
- **Release Editing** — 创建后编辑发布的标题、描述和草稿/预发布标志
- **Wiki** — 基于 Markdown 的每仓库 Wiki 页面，带有修订历史
- **项目** — 带有拖放卡片的看板，用于组织工作
- **代码片段** — 分享代码片段（类似 GitHub Gists），支持语法高亮和多文件
- **组织与团队** — 创建包含成员和团队的组织，为仓库分配团队权限
- **细粒度权限** — 五级权限模型（读取、分类、写入、维护、管理），实现仓库的细粒度访问控制
- **里程碑** — 使用进度条和截止日期跟踪 Issue 的里程碑进度
- **提交评论** — 对单个提交进行评论，可选文件/行引用
- **仓库主题** — 为仓库添加主题标签，用于在探索页面发现和过滤
- **Activity Pulse** — 每个仓库的每周摘要页面，显示过去7天内合并的PR、打开/关闭的Issue、提交、顶级贡献者和活跃分支

### CI/CD 与 DevOps
- **CI/CD 运行器** — 在 `.github/workflows/*.yml` 中定义工作流并在 Docker 容器中运行。在 push 和 pull request 事件时自动触发
- **GitHub Actions 兼容性** — 相同的工作流 YAML 在 MyPersonalGit 和 GitHub Actions 上都能运行。将 `uses:` 动作（`actions/checkout`、`actions/setup-dotnet`、`actions/setup-node`、`actions/setup-python`、`actions/setup-java`、`docker/login-action`、`docker/build-push-action`、`softprops/action-gh-release`）翻译为等效的 shell 命令
- **并行作业与 `needs:`** — 作业通过 `needs:` 声明依赖关系，独立时并行运行。依赖作业等待其先决条件，如果依赖失败则自动取消
- **条件步骤 (`if:`)** — 步骤支持 `if:` 表达式：`always()`、`success()`、`failure()`、`cancelled()`、`true`、`false`。带有 `if: failure()` 或 `if: always()` 的清理步骤在之前的失败后仍会运行
- **步骤输出 (`$GITHUB_OUTPUT`)** — 步骤可以将 `key=value` 或 `key<<DELIMITER` 多行对写入 `$GITHUB_OUTPUT`，后续步骤将其作为环境变量接收，兼容 `${{ steps.X.outputs.Y }}` 语法
- **`github` 上下文** — `GITHUB_SHA`、`GITHUB_REF`、`GITHUB_REF_NAME`、`GITHUB_ACTOR`、`GITHUB_REPOSITORY`、`GITHUB_EVENT_NAME`、`GITHUB_WORKSPACE`、`GITHUB_RUN_ID`、`GITHUB_JOB`、`GITHUB_WORKFLOW` 和 `CI=true` 自动注入到每个作业中
- **矩阵构建** — `strategy.matrix` 在多个变量组合（例如操作系统 x 版本）之间展开作业。支持 `fail-fast` 和在 `runs-on`、步骤命令和步骤名称中使用 `${{ matrix.X }}` 替换
- **`workflow_dispatch` 输入** — 带有类型化输入参数（string、boolean、choice、number）的手动触发。手动触发带有输入的工作流时，界面会显示输入表单。值以 `INPUT_*` 环境变量注入
- **作业超时 (`timeout-minutes`)** — 在作业上设置 `timeout-minutes`，如果超出限制则自动失败。默认：360 分钟（与 GitHub Actions 一致）
- **作业级 `if:`** — 根据条件跳过整个作业。带有 `if: always()` 的作业即使依赖失败也会运行。被跳过的作业不会导致运行失败
- **作业输出** — 作业声明 `outputs:`，下游 `needs:` 作业通过 `${{ needs.X.outputs.Y }}` 使用。输出在作业完成后从步骤输出中解析
- **`continue-on-error`** — 将单个步骤标记为允许失败而不导致作业失败。适用于可选的验证或通知步骤
- **`on.push.paths` 过滤** — 仅在特定文件更改时触发工作流。支持 glob 模式（`src/**`、`*.ts`）和 `paths-ignore:` 排除项
- **重新运行工作流** — 从 Actions 界面一键重新运行失败、成功或已取消的工作流运行。使用相同配置创建新的运行
- **`working-directory`** — 在工作流级别设置 `defaults.run.working-directory` 或在每个步骤设置 `working-directory:` 来控制命令执行位置
- **`defaults.run.shell`** — 为每个工作流或每个步骤配置自定义 shell（`bash`、`sh`、`python3` 等）
- **`strategy.max-parallel`** — 限制并发矩阵作业执行数量
- **Reusable Workflows (`workflow_call`)** — 使用 `on: workflow_call` 定义工作流，其他工作流可通过 `uses: ./.github/workflows/build.yml` 调用。支持类型化的输入、输出和密钥。被调用工作流的作业内联到调用者中
- **Composite Actions** — 在 `.github/actions/{name}/action.yml` 中使用 `runs: using: composite` 定义多步骤操作。组合操作的步骤在执行时内联展开
- **Environment Deployments** — 配置部署环境（如 `staging`、`production`）及保护规则：必需审查者、等待计时器和分支限制。带有 `environment:` 的工作流作业在执行前需要审批。包含批准/拒绝 UI 的完整部署历史
- **`on.workflow_run`** — 工作流链：在工作流 A 完成时触发工作流 B。通过工作流名称和 `types: [completed]` 过滤
- **自动创建发布** — `softprops/action-gh-release` 创建包含标签、标题、变更日志正文和预发布/草稿标志的真实 Release 实体。源代码归档（ZIP 和 TAR.GZ）自动作为可下载资产附加
- **自动发布流水线** — 内置工作流在每次推送到 main 时自动标记版本、生成变更日志并推送 Docker 镜像到 Docker Hub
- **提交状态检查** — 工作流自动在提交上设置 pending/success/failure 状态，在 Pull Request 上可见
- **工作流取消** — 从 Actions 界面取消正在运行或排队的工作流
- **并发控制** — 新的推送自动取消同一工作流的排队运行
- **工作流环境变量** — 在 YAML 中的工作流、作业或步骤级别设置 `env:`
- **状态徽章** — 可嵌入的工作流和提交状态 SVG 徽章（`/api/badge/{repo}/workflow`）
- **制品下载** — 直接从 Actions 界面下载构建制品
- **密钥管理** — 加密的仓库密钥（AES-256）作为环境变量注入到 CI/CD 工作流运行中
- **Webhooks** — 在仓库事件上触发外部服务
- **Prometheus 指标** — 内置 `/metrics` 端点用于监控

### 软件包与容器托管 (20 registries)
- **容器注册表** — 使用 `docker push` 和 `docker pull` 托管 Docker/OCI 镜像（OCI Distribution Spec）
- **NuGet 注册表** — 托管 .NET 软件包，完整的 NuGet v3 API（服务索引、搜索、推送、还原）
- **npm 注册表** — 托管 Node.js 软件包，标准的 npm 发布/安装
- **PyPI 注册表** — 托管 Python 软件包，支持 PEP 503 Simple API、JSON 元数据 API 和 `twine upload` 兼容性
- **Maven 注册表** — 托管 Java/JVM 软件包，标准的 Maven 仓库布局、`maven-metadata.xml` 生成和 `mvn deploy` 支持
- **Alpine Registry** — 托管 Alpine Linux `.apk` 软件包，支持 APKINDEX 生成
- **RPM Registry** — 托管 RPM 软件包，包含 `dnf`/`yum` 用的 `repomd.xml` 元数据
- **Chef Registry** — 托管 Chef Cookbook，兼容 Chef Supermarket API
- **通用软件包** — 通过 REST API 上传和下载任意二进制制品

### 静态站点
- **Pages** — 直接从仓库分支提供静态网站服务（类似 GitHub Pages），路径为 `/pages/{owner}/{repo}/`

### RSS/Atom 订阅源
- **仓库订阅源** — 每个仓库的提交、发布和标签的 Atom 订阅源（`/api/feeds/{repo}/commits.atom`、`/api/feeds/{repo}/releases.atom`、`/api/feeds/{repo}/tags.atom`）
- **用户活动订阅源** — 每用户活动订阅源（`/api/feeds/users/{username}/activity.atom`）
- **全局活动订阅源** — 全站活动订阅源（`/api/feeds/global/activity.atom`）

### 通知
- **应用内通知** — 提及、评论和仓库活动
- **推送通知** — Ntfy 和 Gotify 集成，用于实时移动/桌面提醒，支持每用户选择加入

### 认证
- **OAuth2 / SSO** — 使用 GitHub、Google、Microsoft、GitLab、Bitbucket、Facebook、Discord 或 Twitter/X 登录。管理员在管理后台为每个提供商配置 Client ID 和 Secret——只有填写了凭据的提供商才会对用户显示
- **OAuth2 提供商** — 作为身份提供商，让其他应用可以使用"使用 MyPersonalGit 登录"。实现带 PKCE 的授权码流程、令牌刷新、用户信息端点和 OpenID Connect 发现（`.well-known/openid-configuration`）
- **LDAP / Active Directory** — 针对 LDAP 目录或 Active Directory 域对用户进行认证。用户在首次登录时自动创建并同步属性（邮箱、显示名称）。支持基于组的管理员提升、SSL/TLS 和 StartTLS
- **SSPI / Windows 集成认证** — 通过 Negotiate/NTLM 为 Windows 域用户提供透明的单点登录。域用户无需输入凭据即可自动认证。在管理 > 设置中启用（仅限 Windows）
- **双因素认证** — 基于 TOTP 的双因素认证，支持验证器应用和恢复码
- **WebAuthn / Passkeys** — FIDO2 硬件安全密钥和 Passkey 支持作为第二因素。注册 YubiKeys、平台验证器（Face ID、Windows Hello、Touch ID）和其他 FIDO2 设备。签名计数验证用于检测克隆密钥
- **关联账户** — 用户可以在设置中将多个 OAuth 提供商关联到其账户

### 管理
- **管理后台** — 系统设置（包括数据库提供商、SSH 服务器、LDAP/AD、页脚页面）、用户管理、审计日志和统计
- **自定义页脚页面** — 服务条款、隐私政策、文档和联系页面，支持从管理 > 设置中编辑 Markdown 内容
- **用户资料** — 每用户的贡献热力图、活动源和统计
- **个人访问令牌** — 基于令牌的 API 认证，支持可配置作用域和可选的路由级限制（glob 模式如 `/api/packages/**` 限制令牌访问特定 API 路径）
- **备份与恢复** — 导出和导入服务器数据
- **安全扫描** — 由 [OSV.dev](https://osv.dev/) 数据库驱动的真实依赖漏洞扫描。自动从 `.csproj`（NuGet）、`package.json`（npm）、`requirements.txt`（PyPI）、`Cargo.toml`（Rust）、`Gemfile`（Ruby）、`composer.json`（PHP）、`go.mod`（Go）、`pom.xml`（Maven/Java）和 `pubspec.yaml`（Dart/Flutter）提取依赖项，然后对照已知 CVE 进行检查。报告严重性、修复版本和咨询链接。另外还有手动安全公告的草稿/发布/关闭工作流
- **Secret Scanning** — 自动扫描每次推送以检测泄露的凭证（AWS 密钥、GitHub/GitLab 令牌、Slack 令牌、私钥、API 密钥、JWT、连接字符串等）。20 个内置模式，支持完整正则表达式。按需全仓库扫描。带解决/误报工作流的警报。可通过 API 配置自定义模式
- **Dependabot-Style Auto-Update PRs** — 自动检查过时的依赖项并创建 Pull Request 进行更新。支持 NuGet、npm 和 PyPI 生态系统。可配置的计划（每日/每周/每月）以及每个仓库的开放 PR 限制
- **Repository Insights (Traffic)** — 跟踪克隆/拉取计数、页面浏览量、独立访客、热门来源和热门内容路径。Insights 选项卡中的流量图表包含 14 天摘要。每日聚合，保留 90 天。IP 地址经过哈希处理以保护隐私
- **深色模式** — 完整的深色/浅色模式支持，在页头有切换开关
- **多语言 / i18n** — 所有 29 个页面的完整本地化，共 920 个资源键。内置 11 种语言：英语、西班牙语、法语、德语、日语、韩语、简体中文、葡萄牙语、俄语、意大利语和土耳其语。通过创建 `SharedResource.{locale}.resx` 文件添加更多语言。页头的语言选择器可切换语言
- **Swagger / OpenAPI** — 在 `/swagger` 提供交互式 API 文档，所有 REST 端点均可发现和测试
- **Open Graph Meta Tags** — 仓库、Issue 和 PR 页面包含 og:title 和 og:description，用于在 Slack、Discord 和社交媒体中显示丰富的链接预览
- **Mermaid 图表** — 在 Markdown 文件中渲染 Mermaid 图表（流程图、时序图、甘特图等）
- **数学公式渲染** — Markdown 中的 LaTeX/KaTeX 数学表达式（`$inline$` 和 `$$display$$` 语法）
- **CSV/TSV 查看器** — CSV 和 TSV 文件以格式化、可排序的表格呈现，而非原始文本
- **Keyboard Shortcuts** — 按 `?` 显示快捷键帮助模态框。`/` 聚焦搜索，`g i` 跳转到 Issue，`g p` 跳转到 Pull Request，`g h` 跳转到首页，`g n` 跳转到通知
- **Health Check Endpoint** — `/health` 返回包含数据库连接状态的 JSON，用于 Docker/Kubernetes 监控
- **Line Linking** — 在文件查看器中点击行号生成可分享的 `#L42` URL，加载时高亮显示对应行
- **File Download** — 使用正确的 Content-Disposition 头从文件查看器下载单个文件
- **Jupyter Notebook 渲染** — `.ipynb` 文件以格式化的笔记本形式呈现，包含代码单元格、Markdown、输出和内联图片
- **仓库转移** — 从仓库设置中将仓库所有权转移给其他用户或组织
- **默认分支配置** — 从设置选项卡更改每个仓库的默认分支
- **Rename Repository** — 从 Settings 重命名仓库，自动更新所有引用（Issues、PRs、星标、Webhooks、Secrets 等）
- **User-Level Secrets** — 用户拥有的所有仓库共享的加密 Secrets，在 Settings > Secrets 中管理
- **Organization-Level Secrets** — 组织内所有仓库共享的加密 Secrets，在组织的 Secrets 选项卡中管理
- **Repository Pinning** — 将最多6个常用仓库固定到用户个人资料页面以便快速访问
- **Git Hooks Management** — 用于查看、编辑和管理每个仓库的服务器端 Git Hooks（pre-receive、update、post-receive、post-update、pre-push）的 Web UI
- **Protected File Patterns** — 使用 glob 模式的分支保护规则，要求对特定文件的更改进行审查批准（例如 `*.lock`、`migrations/**`、`.github/workflows/*`）
- **External Issue Tracker** — 配置仓库链接到外部 Issue 跟踪器（Jira、Linear 等），支持自定义 URL 模式
- **Federation (NodeInfo/WebFinger)** — NodeInfo 2.0 发现、WebFinger 和 host-meta，实现跨实例可发现性
- **Distributed CI Runners** — 外部运行器可通过 API 注册、轮询排队任务并报告结果

## 技术栈

| 组件 | 技术 |
|------|------|
| 后端 | ASP.NET Core 10.0 |
| 前端 | Blazor Server（交互式服务端渲染） |
| 数据库 | SQLite（默认）或 PostgreSQL，通过 Entity Framework Core 10 |
| Git 引擎 | LibGit2Sharp |
| 认证 | BCrypt 密码哈希、基于会话的认证、PAT 令牌、OAuth2（8 个提供商 + 提供商模式）、TOTP 2FA、WebAuthn/Passkeys、LDAP/AD、SSPI |
| SSH 服务器 | 内置 SSH2 协议实现（ECDH、AES-CTR、HMAC-SHA2） |
| Markdown | Markdig |
| CI/CD | Docker.DotNet、YamlDotNet |
| 监控 | Prometheus 指标 |

## 快速开始

### 前置条件

- [Docker](https://docs.docker.com/get-docker/)（推荐）
- 或 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git 用于本地开发

### Docker（推荐）

从 Docker Hub 拉取并运行：

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> 端口 2222 是可选的——仅在您在管理 > 设置中启用内置 SSH 服务器时需要。

或使用 Docker Compose：

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

应用将在 **http://localhost:8080** 上可用。

> **默认凭据**：`admin` / `admin`
>
> 首次登录后，请通过管理后台**立即更改默认密码**。

### 本地运行

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

应用在 **http://localhost:5146** 启动。

### 环境变量

| 变量 | 描述 | 默认值 |
|------|------|--------|
| `Database__Provider` | 数据库引擎：`sqlite` 或 `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | 数据库连接字符串 | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Git 仓库存储目录 | `/repos` |
| `Git__RequireAuth` | Git HTTP 操作是否需要认证 | `true` |
| `Git__Users__<username>` | 设置 Git HTTP Basic Auth 用户的密码 | — |
| `RESET_ADMIN_PASSWORD` | 启动时紧急重置管理员密码 | — |
| `Secrets__EncryptionKey` | 仓库密钥的自定义加密密钥 | 从数据库连接字符串派生 |
| `Ssh__DataDir` | SSH 数据目录（主机密钥、authorized_keys） | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | 生成的 authorized_keys 文件路径 | `<DataDir>/authorized_keys` |

> **注意：** 内置 SSH 服务器端口和 LDAP 设置通过管理后台（管理 > 设置）配置，而非环境变量。这允许您无需重新部署即可更改它们。

## 使用说明

### 1. 登录

打开应用并点击**登录**。在全新安装上，使用默认凭据（`admin` / `admin`）。通过**管理**后台创建其他用户，或在管理 > 设置中启用用户注册。

### 2. 创建仓库

点击主页上的绿色**新建**按钮，输入名称，然后点击**创建**。这将在服务器上创建一个裸 Git 仓库，您可以通过 Web 界面进行克隆、推送和管理。

### 3. 克隆和推送

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

如果启用了 Git HTTP 认证，系统会提示您输入通过 `Git__Users__<username>` 环境变量配置的凭据。这些凭据与 Web 界面登录是分开的。

### 4. 从 IDE 克隆

**VS Code**：`Ctrl+Shift+P` > **Git: Clone** > 粘贴 `http://localhost:8080/git/MyRepo.git`

**Visual Studio**：**Git > 克隆仓库** > 粘贴 URL

**JetBrains**：**文件 > 新建 > 从版本控制获取项目** > 粘贴 URL

### 5. 使用 Web 编辑器

您可以直接在浏览器中编辑文件：
- 导航到仓库并点击任意文件，然后点击**编辑**
- 使用**添加文件 > 创建新文件**在无本地克隆的情况下添加文件
- 使用**添加文件 > 上传文件/文件夹**从您的机器上传

### 6. 容器注册表

直接向您的服务器推送和拉取 Docker/OCI 镜像：

```bash
# 登录（使用来自设置 > 访问令牌的个人访问令牌）
docker login localhost:8080 -u youruser

# 推送镜像
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# 拉取镜像
docker pull localhost:8080/myapp:v1
```

> **注意：** Docker 默认要求 HTTPS。对于 HTTP，请将您的服务器添加到 `~/.docker/daemon.json` 中 Docker 的 `insecure-registries`：
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. 软件包注册表

**NuGet（.NET 软件包）：**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm（Node.js 软件包）：**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI（Python 软件包）：**
```bash
# 安装软件包
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# 使用 twine 上传
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

**Maven（Java/JVM 软件包）：**
```xml
<!-- 在您的 pom.xml 中添加仓库 -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- 在 settings.xml 中添加凭据 -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**通用（任意二进制文件）：**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

在 Web 界面的 `/packages` 浏览所有软件包。

### 8. Pages（静态站点托管）

从仓库分支提供静态网站服务：

1. 进入仓库的**设置**标签页并启用 **Pages**
2. 设置分支（默认：`gh-pages`）
3. 将 HTML/CSS/JS 推送到该分支
4. 访问 `http://localhost:8080/pages/{username}/{repo}/`

### 9. 推送通知

在**管理 > 系统设置**中配置 Ntfy 或 Gotify，以在创建 Issue、PR 或评论时在手机或桌面上接收推送通知。用户可以在**设置 > 通知**中选择加入/退出。

### 10. SSH 密钥认证

使用 SSH 密钥进行免密码的 Git 操作。有两种选择：

#### 选项 A：内置 SSH 服务器（推荐）

无需外部 SSH 守护进程——MyPersonalGit 运行自己的 SSH 服务器：

1. 进入**管理 > 设置**并启用**内置 SSH 服务器**
2. 设置 SSH 端口（默认：2222）——如果您没有运行系统 SSH，可以使用 22
3. 保存设置并重启服务器（端口更改需要重启）
4. 进入**设置 > SSH 密钥**并添加您的公钥（`~/.ssh/id_ed25519.pub`、`~/.ssh/id_rsa.pub` 或 `~/.ssh/id_ecdsa.pub`）
5. 通过 SSH 克隆：
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

内置 SSH 服务器支持 ECDH-SHA2-NISTP256 密钥交换、AES-128/256-CTR 加密、HMAC-SHA2-256，以及 Ed25519、RSA 和 ECDSA 密钥的公钥认证。

#### 选项 B：系统 OpenSSH

如果您偏好使用系统的 SSH 守护进程：

1. 进入**设置 > SSH 密钥**并添加您的公钥
2. MyPersonalGit 自动从所有注册的 SSH 密钥维护 `authorized_keys` 文件
3. 配置您服务器的 OpenSSH 使用生成的 authorized_keys 文件：
   ```
   # 在 /etc/ssh/sshd_config 中
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. 通过 SSH 克隆：
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

SSH 认证服务还在 `/api/ssh/authorized-keys` 暴露了一个 API，可与 OpenSSH 的 `AuthorizedKeysCommand` 指令配合使用。

### 11. LDAP / Active Directory 认证

针对您组织的 LDAP 目录或 Active Directory 域对用户进行认证：

1. 进入**管理 > 设置**并滚动到 **LDAP / Active Directory 认证**
2. 启用 LDAP 并填写您的服务器详情：
   - **服务器**：您的 LDAP 服务器主机名（例如 `dc01.corp.local`）
   - **端口**：LDAP 使用 389，LDAPS 使用 636
   - **SSL/TLS**：为 LDAPS 启用，或使用 StartTLS 升级普通连接
3. 配置用于搜索用户的服务账户：
   - **Bind DN**：`CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind 密码**：服务账户密码
4. 设置搜索参数：
   - **搜索基础 DN**：`OU=Users,DC=corp,DC=local`
   - **用户过滤器**：AD 使用 `(sAMAccountName={0})`，OpenLDAP 使用 `(uid={0})`
5. 将 LDAP 属性映射到用户字段：
   - **用户名**：`sAMAccountName`（AD）或 `uid`（OpenLDAP）
   - **邮箱**：`mail`
   - **显示名称**：`displayName`
6. 可选设置**管理员组 DN**——该组成员自动提升为管理员
7. 点击**测试 LDAP 连接**验证设置
8. 保存设置

用户现在可以在登录页面使用域凭据登录。首次登录时，系统会自动创建本地账户并从目录同步属性。LDAP 认证也用于 Git HTTP 操作（克隆/推送）。

### 12. 仓库密钥

向仓库添加加密密钥，用于 CI/CD 工作流：

1. 进入仓库的**设置**标签页
2. 滚动到**密钥**卡片并点击**添加密钥**
3. 输入名称（例如 `DEPLOY_TOKEN`）和值——值使用 AES-256 加密
4. 密钥自动作为环境变量注入到每个工作流运行中

在您的工作流中引用密钥：
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO 登录

使用外部身份提供商登录：

1. 进入**管理 > OAuth / SSO**并配置您要启用的提供商
2. 输入提供商开发者控制台中的 **Client ID** 和 **Client Secret**
3. 勾选**启用**——只有填写了两个凭据的提供商才会出现在登录页面
4. 每个提供商的回调 URL 在管理面板中显示（例如 `https://yourserver/oauth/callback/github`）

支持的提供商：GitHub、Google、Microsoft、GitLab、Bitbucket、Facebook、Discord、Twitter/X。

用户可以在**设置 > 关联账户**中将多个提供商关联到其账户。

### 14. 导入仓库

从外部来源导入完整历史的仓库：

1. 在主页点击**导入**
2. 选择来源类型（Git URL、GitHub、GitLab 或 Bitbucket）
3. 输入仓库 URL，对于私有仓库可选输入认证令牌
4. 对于 GitHub/GitLab/Bitbucket 导入，可选导入 Issue 和 Pull Request
5. 在导入页面实时跟踪导入进度

### 15. Fork 与上游同步

Fork 仓库并保持同步：

1. 在任意仓库页面点击 **Fork** 按钮
2. Fork 将在您的用户名下创建，并带有返回原始仓库的链接
3. 点击"forked from"徽章旁边的**同步 Fork**以从上游拉取最新更改

### 16. CI/CD 自动发布

MyPersonalGit 包含一个内置的 CI/CD 流水线，在每次推送到 main 时自动标记、发布并推送 Docker 镜像。工作流在推送时自动触发——无需外部 CI 服务。

**工作原理：**
1. 推送到 `main` 自动触发 `.github/workflows/release.yml`
2. 递增补丁版本（`v1.15.1` -> `v1.15.2`），创建 git 标签
3. 登录 Docker Hub，构建镜像，并推送为 `:latest` 和 `:vX.Y.Z`

**设置：**
1. 在 MyPersonalGit 中进入仓库的**设置 > 密钥**
2. 添加名为 `DOCKERHUB_TOKEN` 的密钥，值为您的 Docker Hub 访问令牌
3. 确保 MyPersonalGit 容器已挂载 Docker socket（`-v /var/run/docker.sock:/var/run/docker.sock`）
4. 推送到 main——工作流自动触发

**GitHub Actions 兼容性：**
相同的工作流 YAML 也可在 GitHub Actions 上运行——无需更改。MyPersonalGit 在运行时将 `uses:` 动作翻译为等效的 shell 命令：

| GitHub Action | MyPersonalGit 翻译 |
|---|---|
| `actions/checkout@v4` | 仓库已克隆到 `/workspace` |
| `actions/setup-dotnet@v4` | 通过官方安装脚本安装 .NET SDK |
| `actions/setup-node@v4` | 通过 NodeSource 安装 Node.js |
| `actions/setup-python@v5` | 通过 apt/apk 安装 Python |
| `actions/setup-java@v4` | 通过 apt/apk 安装 OpenJDK |
| `docker/login-action@v3` | 使用 stdin 密码的 `docker login` |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | 无操作（使用默认构建器） |
| `softprops/action-gh-release@v2` | 在数据库中创建真实的 Release 实体 |
| `${{ secrets.X }}` | `$X` 环境变量 |
| `${{ steps.X.outputs.Y }}` | `$Y` 环境变量 |
| `${{ github.sha }}` | `$GITHUB_SHA` 环境变量 |

**并行作业：**
作业默认并行运行。使用 `needs:` 声明依赖关系：
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
没有 `needs:` 的作业立即启动。如果任何依赖失败，作业将被取消。

**条件步骤：**
使用 `if:` 控制步骤何时运行：
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
支持的表达式：`always()`、`success()`（默认）、`failure()`、`cancelled()`、`true`、`false`。

**步骤输出：**
步骤可以通过 `$GITHUB_OUTPUT` 将值传递给后续步骤：
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**矩阵构建：**
使用 `strategy.matrix` 在多个组合之间展开作业：
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
这将创建 4 个作业：`test (ubuntu-latest, 1.0)`、`test (ubuntu-latest, 2.0)` 等。所有作业并行运行。

**带输入的手动触发 (`workflow_dispatch`)：**
定义在手动触发时在界面中显示为表单的类型化输入：
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
输入值以 `INPUT_<NAME>` 环境变量注入（大写）。

**作业超时：**
在作业上设置 `timeout-minutes`，如果运行时间过长则自动失败：
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
默认超时为 360 分钟（6 小时），与 GitHub Actions 一致。

**作业级条件：**
在作业上使用 `if:` 根据条件跳过它们：
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

**作业输出：**
作业可以通过 `outputs:` 将值传递给下游作业：
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

**允许错误继续：**
让步骤失败而不导致作业失败：
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**路径过滤：**
仅在特定文件更改时触发工作流：
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # 或使用 paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**工作目录：**
设置命令执行位置：
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # 在 src/app 中运行
      - run: npm test
        working-directory: tests  # 覆盖默认值
```

**重新运行工作流：**
在任何已完成、失败或已取消的工作流运行上点击**重新运行**按钮，使用相同的作业、步骤和配置创建新的运行。

**Pull Request 工作流：**
带有 `on: pull_request` 的工作流在创建非草稿 PR 时自动触发，针对源分支运行检查。

**提交状态检查：**
工作流自动设置提交状态（pending/success/failure），以便您可以在 PR 上查看构建结果，并通过分支保护强制要求检查通过。

**工作流取消：**
在 Actions 界面中点击任何正在运行或排队的工作流的**取消**按钮以立即停止它。

**状态徽章：**
在您的 README 或任何地方嵌入构建状态徽章：
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
按工作流名称过滤：`/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom 订阅源

使用标准 Atom 订阅源在任何 RSS 阅读器中订阅仓库活动：

```
# 仓库提交
http://localhost:8080/api/feeds/MyRepo/commits.atom

# 仓库发布
http://localhost:8080/api/feeds/MyRepo/releases.atom

# 仓库标签
http://localhost:8080/api/feeds/MyRepo/tags.atom

# 用户活动
http://localhost:8080/api/feeds/users/admin/activity.atom

# 全局活动（所有仓库）
http://localhost:8080/api/feeds/global/activity.atom
```

公开仓库无需认证。将这些 URL 添加到任何 Feed 阅读器（Feedly、Miniflux、FreshRSS 等）以保持对变更的关注。

## 数据库配置

MyPersonalGit 默认使用 **SQLite**——零配置、单文件数据库，非常适合个人使用和小团队。

对于更大的部署（许多并发用户、高可用性，或者您已经在运行 PostgreSQL），可以切换到 **PostgreSQL**：

### 使用 PostgreSQL

**Docker Compose**（PostgreSQL 推荐使用）：
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

**仅环境变量**（如果您已有 PostgreSQL 服务器）：
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

EF Core 迁移在两种提供商启动时自动运行。无需手动设置架构。

### 从管理后台切换

您也可以直接从 Web 界面切换数据库提供商：

1. 进入**管理 > 设置**——**数据库**卡片在顶部
2. 从提供商下拉列表中选择 **PostgreSQL**
3. 输入您的 PostgreSQL 连接字符串（例如 `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`）
4. 点击**保存数据库设置**
5. 重启应用程序以使更改生效

配置保存到 `~/.mypersonalgit/database.json`（在数据库外部，以便在连接前可以读取）。

### 选择数据库

| | SQLite | PostgreSQL |
|---|---|---|
| **设置** | 零配置（默认） | 需要 PostgreSQL 服务器 |
| **适用于** | 个人使用、小团队、NAS | 50+ 人团队、高并发 |
| **备份** | 复制 `.db` 文件 | 标准的 `pg_dump` |
| **并发** | 单写入者（大多数使用场景足够） | 完整的多写入者 |
| **迁移** | 不适用 | 切换提供商 + 运行应用（自动迁移） |

## 部署到 NAS

MyPersonalGit 可以通过 Docker 很好地运行在 NAS（QNAP、Synology 等）上：

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Docker socket 挂载是可选的——仅在您需要 CI/CD 工作流执行时需要。端口 2222 仅在您启用内置 SSH 服务器时需要。

## 配置

所有设置都可以在 `appsettings.json` 中、通过环境变量或通过 `/admin` 的管理后台进行配置：

- 数据库提供商（SQLite 或 PostgreSQL）
- 项目根目录
- 认证要求
- 用户注册设置
- 功能开关（Issue、Wiki、项目、Actions）
- 每用户最大仓库大小和数量
- SMTP 邮件通知设置
- 推送通知设置（Ntfy/Gotify）
- 内置 SSH 服务器（启用/禁用、端口）
- LDAP/Active Directory 认证（服务器、Bind DN、搜索基础、用户过滤器、属性映射、管理员组）
- OAuth/SSO 提供商配置（每个提供商的 Client ID/Secret）

## 项目结构

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout、NavMenu
    Pages/           # Blazor 页面（首页、仓库详情、Issue、PR、软件包等）
  Controllers/       # REST API 端点（NuGet、npm、通用、注册表等）
  Data/              # EF Core DbContext、服务实现
  Models/            # 领域模型
  Migrations/        # EF Core 迁移
  Services/          # 中间件（认证、Git HTTP 后端、Pages、注册表认证）
    SshServer/       # 内置 SSH 服务器（SSH2 协议、ECDH、AES-CTR）
  Program.cs         # 应用启动、依赖注入、中间件管道
MyPersonalGit.Tests/
  UnitTest1.cs       # 使用 InMemory 数据库的 xUnit 测试
```

## 运行测试

```bash
dotnet test
```

## 许可证

MIT
