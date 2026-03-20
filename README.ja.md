🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

ASP.NET Core と Blazor Server で構築された、GitHub ライクな Web インターフェースを備えたセルフホスト型 Git サーバーです。リポジトリの閲覧、Issue、プルリクエスト、Wiki、プロジェクトなどの管理を、すべて自分のマシンやサーバー上で行えます。

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## 目次

- [機能](#機能)
- [技術スタック](#技術スタック)
- [クイックスタート](#クイックスタート)
  - [Docker（推奨）](#docker推奨)
  - [ローカル実行](#ローカル実行)
  - [環境変数](#環境変数)
- [使い方](#使い方)
  - [サインイン](#1-サインイン)
  - [リポジトリの作成](#2-リポジトリの作成)
  - [クローンとプッシュ](#3-クローンとプッシュ)
  - [IDE からのクローン](#4-ide-からのクローン)
  - [Web エディタ](#5-web-エディタ)
  - [コンテナレジストリ](#6-コンテナレジストリ)
  - [パッケージレジストリ](#7-パッケージレジストリ)
  - [Pages（静的サイトホスティング）](#8-pages静的サイトホスティング)
  - [プッシュ通知](#9-プッシュ通知)
  - [SSH 鍵認証](#10-ssh-鍵認証)
  - [LDAP / Active Directory](#11-ldap--active-directory-認証)
  - [リポジトリシークレット](#12-リポジトリシークレット)
  - [OAuth / SSO ログイン](#13-oauth--sso-ログイン)
  - [リポジトリのインポート](#14-リポジトリのインポート)
  - [フォークとアップストリーム同期](#15-フォークとアップストリーム同期)
  - [CI/CD 自動リリース](#16-cicd-自動リリース)
  - [RSS/Atom フィード](#17-rssatom-フィード)
- [データベース設定](#データベース設定)
  - [PostgreSQL の使用](#postgresql-の使用)
  - [管理ダッシュボードからの切り替え](#管理ダッシュボードからの切り替え)
  - [データベースの選択](#データベースの選択)
- [NAS へのデプロイ](#nas-へのデプロイ)
- [設定](#設定)
- [プロジェクト構成](#プロジェクト構成)
- [テストの実行](#テストの実行)
- [ライセンス](#ライセンス)

---

## 機能

### コード & リポジトリ
- **リポジトリ管理** — コードブラウザ、ファイルエディタ、コミット履歴、ブランチ、タグを備えた Git リポジトリの作成、閲覧、削除
- **リポジトリのインポート/移行** — GitHub、GitLab、Bitbucket、Gitea/Forgejo/Gogs、または任意の Git URL からリポジトリをインポート。Issue や PR のインポートにも対応。バックグラウンド処理と進捗追跡が可能
- **リポジトリのアーカイブ** — リポジトリを読み取り専用としてマークし、ビジュアルバッジを表示。アーカイブされたリポジトリへのプッシュはブロックされます
- **Git Smart HTTP** — Basic Auth を使用した HTTP 経由のクローン、フェッチ、プッシュ
- **組み込み SSH サーバー** — Git 操作用のネイティブ SSH サーバー（外部の OpenSSH は不要）。ECDH 鍵交換、AES-CTR 暗号化、公開鍵認証（RSA、ECDSA、Ed25519）をサポート
- **SSH 鍵認証** — アカウントに SSH 公開鍵を追加し、自動管理される `authorized_keys`（または組み込み SSH サーバー）を使用した SSH 経由の Git 操作認証
- **フォーク & アップストリーム同期** — リポジトリのフォーク、ワンクリックでのアップストリームとの同期、UI でのフォーク関係の表示
- **Git LFS** — バイナリファイルのトラッキングに対応した Large File Storage サポート
- **リポジトリミラーリング** — 外部 Git リモートとのリポジトリミラーリング
- **比較ビュー** — ブランチ間の ahead/behind コミット数と完全な diff レンダリングによる比較
- **言語統計** — 各リポジトリページに GitHub スタイルの言語内訳バーを表示
- **ブランチ保護** — 必須レビュー、ステータスチェック、強制プッシュ防止、CODEOWNERS 承認の強制などの設定可能なルール
- **署名済みコミットの必須化** — マージ前にすべてのコミットが GPG 署名されていることを要求するブランチ保護ルール
- **タグ保護** — Glob パターンマッチングとユーザーごとの許可リストによるタグの削除、強制更新、不正な作成からの保護
- **コミット署名の検証** — コミットと注釈付きタグの GPG 署名検証。UI に「Verified」/「Signed」バッジを表示
- **リポジトリラベル** — リポジトリごとにカスタムカラーのラベルを管理。テンプレートからリポジトリを作成する際にラベルは自動的にコピーされます
- **AGit Flow** — プッシュによるレビューワークフロー: `git push origin HEAD:refs/for/main` でフォークやリモートブランチの作成なしにプルリクエストを作成。後続のプッシュで既存の未クローズ PR を更新
- **探索** — 検索、ソート、トピックフィルタリングを使用してアクセス可能なすべてのリポジトリを閲覧
- **Autolink References** — `#123` を自動的に Issue リンクに変換し、リポジトリごとに設定可能なカスタムパターン（例: `JIRA-456` → 外部 URL）にも対応
- **検索** — リポジトリ、Issue、PR、コード全体にわたる全文検索

### コラボレーション
- **Issue & プルリクエスト** — ラベル、複数アサイニー、期限、レビュー付きの Issue と PR の作成、コメント、クローズ/リオープン。マージコミット、スカッシュ、リベース戦略による PR のマージ。サイドバイサイド diff ビューによる Web ベースのマージコンフリクト解決
- **Issue の依存関係** — 循環依存の検出付きで Issue 間の「ブロックされている」「ブロックしている」関係を定義
- **Issue のピン留め & ロック** — 重要な Issue をリストの先頭にピン留めし、会話をロックしてコメントを防止
- **コメントの編集 & 削除** — Issue やプルリクエストの自分のコメントを編集・削除。「(edited)」インジケーター付き
- **マージコンフリクトの解決** — ブラウザ上で直接マージコンフリクトを解決。base/ours/theirs ビュー、クイック承認ボタン、コンフリクトマーカーの検証を備えたビジュアルエディタ
- **ディスカッション** — GitHub Discussions スタイルのリポジトリごとのスレッド会話。カテゴリ（General、Q&A、Announcements、Ideas、Show & Tell、Polls）、ピン留め/ロック、回答のマーク、投票に対応
- **コードレビューサジェスチョン** — PR のインラインレビューで「変更を提案」モードを使用し、レビュアーが diff 内で直接コードの置き換えを提案可能
- **Image Diff** — Pull Request での画像のサイドバイサイド比較。変更された画像（PNG、JPG、GIF、SVG、WebP）のビジュアル差分用に不透明度スライダー付き
- **PR の File Tree** — Pull Request の diff ビューに折りたたみ可能なファイルツリーサイドバーを表示し、変更されたファイル間を簡単にナビゲーション
- **ファイルを確認済みとしてマーク** — Pull Request でファイルごとの「確認済み」チェックボックスと進捗カウンターによるレビュー進捗の追跡
- **Diff のシンタックスハイライト** — Prism.js による Pull Request および比較 diff での言語対応のシンタックスカラーリング
- **リアクション絵文字** — Issue、PR、ディスカッション、コメントに対して👍/👎、❤️、😄、🎉、😕、🚀、👀でリアクション
- **Auto-Merge** — Pull Request で自動マージを有効にし、すべての必須ステータスチェックが合格しレビューが承認されたときに自動的にマージ
- **Cherry-Pick / Revert via UI** — Web インターフェースから任意のコミットを別のブランチにチェリーピック、またはコミットを直接もしくは新しい Pull Request としてリバート
- **Transfer Issues** — リポジトリ間で Issue を移動。タイトル、本文、コメント、一致するラベルを保持し、元の Issue に転送メモ付きリンクを作成
- **CODEOWNERS** — ファイルパスに基づく PR レビュアーの自動アサイン。マージ前に CODEOWNERS の承認を必須にするオプション付き
- **リポジトリテンプレート** — ファイル、ラベル、Issue テンプレート、ブランチ保護ルールの自動コピーによるテンプレートからの新規リポジトリ作成
- **ドラフト Issue & Issue テンプレート** — ドラフト Issue（作業中）の作成と、リポジトリごとの再利用可能な Issue テンプレート（バグレポート、機能リクエスト）のデフォルトラベル付き定義
- **Wiki** — リビジョン履歴付きのリポジトリごとの Markdown ベースの Wiki ページ
- **プロジェクト** — ドラッグ＆ドロップカードによる作業整理のためのカンバンボード
- **スニペット** — シンタックスハイライトと複数ファイル対応のコードスニペット共有（GitHub Gists のようなもの）
- **組織 & チーム** — メンバーとチームを持つ組織の作成、リポジトリへのチーム権限の割り当て
- **きめ細かな権限設定** — リポジトリに対する5段階の権限モデル（Read、Triage、Write、Maintain、Admin）による詳細なアクセス制御
- **マイルストーン** — 進捗バーと期限付きのマイルストーンに対する Issue の進捗追跡
- **コミットコメント** — ファイル/行の参照を任意で指定できる個別コミットへのコメント
- **リポジトリトピック** — 探索ページでの発見とフィルタリングのためにリポジトリにトピックをタグ付け

### CI/CD & DevOps
- **CI/CD ランナー** — `.github/workflows/*.yml` でワークフローを定義し、Docker コンテナ内で実行。プッシュとプルリクエストイベントで自動トリガー
- **GitHub Actions 互換** — 同じワークフロー YAML が MyPersonalGit と GitHub Actions の両方で動作。`uses:` アクション（`actions/checkout`、`actions/setup-dotnet`、`actions/setup-node`、`actions/setup-python`、`actions/setup-java`、`docker/login-action`、`docker/build-push-action`、`softprops/action-gh-release`）を同等のシェルコマンドに変換
- **`needs:` による並列ジョブ** — ジョブは `needs:` で依存関係を宣言し、独立したジョブは並列実行。依存ジョブは前提条件を待ち、依存先が失敗すると自動的にキャンセル
- **条件付きステップ (`if:`)** — ステップは `if:` 式をサポート: `always()`、`success()`、`failure()`、`cancelled()`、`true`、`false`。`if: failure()` や `if: always()` のクリーンアップステップは先行のステップが失敗しても実行
- **ステップ出力 (`$GITHUB_OUTPUT`)** — ステップは `key=value` または `key<<DELIMITER` の複数行ペアを `$GITHUB_OUTPUT` に書き込み、後続のステップは環境変数として受け取り可能。`${{ steps.X.outputs.Y }}` 構文と互換
- **`github` コンテキスト** — `GITHUB_SHA`、`GITHUB_REF`、`GITHUB_REF_NAME`、`GITHUB_ACTOR`、`GITHUB_REPOSITORY`、`GITHUB_EVENT_NAME`、`GITHUB_WORKSPACE`、`GITHUB_RUN_ID`、`GITHUB_JOB`、`GITHUB_WORKFLOW`、および `CI=true` がすべてのジョブに自動注入
- **マトリックスビルド** — `strategy.matrix` で複数の変数の組み合わせ（例: OS x バージョン）にジョブを展開。`fail-fast` と `runs-on`、ステップコマンド、ステップ名での `${{ matrix.X }}` 置換をサポート
- **`workflow_dispatch` 入力** — 型指定された入力パラメータ（string、boolean、choice、number）による手動トリガー。入力付きワークフローの手動トリガー時に UI に入力フォームを表示。値は `INPUT_*` 環境変数として注入
- **ジョブタイムアウト (`timeout-minutes`)** — ジョブに `timeout-minutes` を設定し、制限を超えた場合に自動的に失敗。デフォルト: 360分（GitHub Actions と同じ）
- **ジョブレベルの `if:`** — 条件に基づいてジョブ全体をスキップ。`if: always()` のジョブは依存先が失敗しても実行。スキップされたジョブはランを失敗させない
- **ジョブ出力** — ジョブは `outputs:` を宣言し、下流の `needs:` ジョブが `${{ needs.X.outputs.Y }}` で利用。出力はジョブ完了後にステップ出力から解決
- **`continue-on-error`** — 個々のステップを「失敗してもジョブを失敗させない」としてマーク。オプションのバリデーションや通知ステップに便利
- **`on.push.paths` フィルター** — 特定のファイルが変更された場合のみワークフローをトリガー。Glob パターン（`src/**`、`*.ts`）と除外用の `paths-ignore:` をサポート
- **ワークフローの再実行** — Actions UI からワンクリックで失敗、成功、またはキャンセルされたワークフローを再実行。同じ設定で新しいランを作成
- **`working-directory`** — ワークフローレベルで `defaults.run.working-directory` を設定、またはステップごとに `working-directory:` でコマンドの実行場所を制御
- **`defaults.run.shell`** — ワークフローまたはステップごとにカスタムシェルを設定（`bash`、`sh`、`python3` など）
- **`strategy.max-parallel`** — マトリックスジョブの同時実行数を制限
- **Reusable Workflows (`workflow_call`)** — `on: workflow_call` でワークフローを定義し、他のワークフローが `uses: ./.github/workflows/build.yml` で呼び出し可能。型付き入力、出力、シークレットをサポート。呼び出されたワークフローのジョブは呼び出し元にインライン化
- **Composite Actions** — `.github/actions/{name}/action.yml` に `runs: using: composite` で複数ステップのアクションを定義。コンポジットアクションのステップは実行時にインライン展開
- **Environment Deployments** — デプロイメント環境（例: `staging`、`production`）を保護ルール付きで設定: 必須レビュアー、待機タイマー、ブランチ制限。`environment:` を持つワークフロージョブは実行前に承認が必要。承認/拒否 UI 付きの完全なデプロイ履歴
- **`on.workflow_run`** — ワークフローの連鎖: ワークフロー A の完了時にワークフロー B をトリガー。ワークフロー名と `types: [completed]` でフィルタリング
- **自動リリース作成** — `softprops/action-gh-release` がタグ、タイトル、変更ログ本文、プレリリース/ドラフトフラグ付きの実際の Release エンティティを作成。ソースコードアーカイブ（ZIP と TAR.GZ）がダウンロード可能なアセットとして自動添付
- **自動リリースパイプライン** — 組み込みワークフローが main へのプッシュごとにバージョンを自動タグ付けし、変更ログを生成し、Docker イメージを Docker Hub にプッシュ
- **コミットステータスチェック** — ワークフローがコミットに pending/success/failure ステータスを自動設定。プルリクエストで確認可能
- **ワークフローのキャンセル** — Actions UI から実行中またはキューに入っているワークフローをキャンセル
- **同時実行制御** — 新しいプッシュは同じワークフローのキューに入っているランを自動的にキャンセル
- **ワークフロー環境変数** — YAML でワークフロー、ジョブ、またはステップレベルで `env:` を設定
- **ステータスバッジ** — ワークフローとコミットステータスの埋め込み可能な SVG バッジ（`/api/badge/{repo}/workflow`）
- **アーティファクトのダウンロード** — Actions UI からビルドアーティファクトを直接ダウンロード
- **シークレット管理** — 暗号化されたリポジトリシークレット（AES-256）を CI/CD ワークフロー実行時に環境変数として注入
- **Webhook** — リポジトリイベントで外部サービスをトリガー
- **Prometheus メトリクス** — 監視用の組み込み `/metrics` エンドポイント

### パッケージ & コンテナホスティング (20 registries)
- **コンテナレジストリ** — `docker push` と `docker pull` による Docker/OCI イメージのホスティング（OCI Distribution Spec）
- **NuGet レジストリ** — 完全な NuGet v3 API（サービスインデックス、検索、プッシュ、リストア）による .NET パッケージのホスティング
- **npm レジストリ** — 標準的な npm publish/install による Node.js パッケージのホスティング
- **PyPI レジストリ** — PEP 503 Simple API、JSON メタデータ API、`twine upload` 互換の Python パッケージのホスティング
- **Maven レジストリ** — 標準的な Maven リポジトリレイアウト、`maven-metadata.xml` 生成、`mvn deploy` サポートによる Java/JVM パッケージのホスティング
- **Alpine Registry** — APKINDEX 生成付きの Alpine Linux `.apk` パッケージのホスティング
- **RPM Registry** — `dnf`/`yum` 用の `repomd.xml` メタデータ付き RPM パッケージのホスティング
- **Chef Registry** — Chef Supermarket 互換 API による Chef Cookbook のホスティング
- **汎用パッケージ** — REST API 経由の任意のバイナリアーティファクトのアップロードとダウンロード

### 静的サイト
- **Pages** — リポジトリのブランチから直接静的 Web サイトを配信（GitHub Pages のように）。`/pages/{owner}/{repo}/` でアクセス

### RSS/Atom フィード
- **リポジトリフィード** — リポジトリごとのコミット、リリース、タグの Atom フィード（`/api/feeds/{repo}/commits.atom`、`/api/feeds/{repo}/releases.atom`、`/api/feeds/{repo}/tags.atom`）
- **ユーザーアクティビティフィード** — ユーザーごとのアクティビティフィード（`/api/feeds/users/{username}/activity.atom`）
- **グローバルアクティビティフィード** — サイト全体のアクティビティフィード（`/api/feeds/global/activity.atom`）

### 通知
- **アプリ内通知** — メンション、コメント、リポジトリアクティビティ
- **プッシュ通知** — ユーザーごとのオプトイン付きリアルタイムモバイル/デスクトップアラートのための Ntfy と Gotify の統合

### 認証
- **OAuth2 / SSO** — GitHub、Google、Microsoft、GitLab、Bitbucket、Facebook、Discord、または Twitter/X でサインイン。管理者が管理ダッシュボードでプロバイダーごとに Client ID と Secret を設定 — 認証情報が入力されたプロバイダーのみユーザーに表示
- **OAuth2 プロバイダー** — 他のアプリが「MyPersonalGit でサインイン」を使えるよう ID プロバイダーとして機能。PKCE 付き Authorization Code フロー、トークンリフレッシュ、userinfo エンドポイント、OpenID Connect ディスカバリ（`.well-known/openid-configuration`）を実装
- **LDAP / Active Directory** — LDAP ディレクトリまたは Active Directory ドメインに対するユーザー認証。初回ログイン時に同期された属性（メール、表示名）でユーザーを自動プロビジョニング。グループベースの管理者昇格、SSL/TLS、StartTLS をサポート
- **SSPI / Windows 統合認証** — Negotiate/NTLM 経由の Windows ドメインユーザー向け透過的シングルサインオン。ドメイン上のユーザーは認証情報の入力なしに自動認証。Admin > Settings で有効化（Windows のみ）
- **二要素認証** — 認証アプリとリカバリーコードをサポートする TOTP ベースの 2FA
- **WebAuthn / パスキー** — セカンドファクターとしての FIDO2 ハードウェアセキュリティキーとパスキーのサポート。YubiKey、プラットフォーム認証器（Face ID、Windows Hello、Touch ID）、その他の FIDO2 デバイスを登録。クローンキー検出のためのサインカウント検証
- **リンクされたアカウント** — ユーザーは設定画面から複数の OAuth プロバイダーをアカウントにリンク可能

### 管理
- **管理ダッシュボード** — システム設定（データベースプロバイダー、SSH サーバー、LDAP/AD、フッターページを含む）、ユーザー管理、監査ログ、統計
- **カスタマイズ可能なフッターページ** — Admin > Settings から Markdown コンテンツを編集可能な利用規約、プライバシーポリシー、ドキュメント、お問い合わせページ
- **ユーザープロフィール** — ユーザーごとのコントリビューションヒートマップ、アクティビティフィード、統計
- **Personal Access Token** — 設定可能なスコープとオプションのルートレベル制限（`/api/packages/**` のような Glob パターンで特定の API パスへのトークンアクセスを制限）付きのトークンベースの API 認証
- **バックアップ & リストア** — サーバーデータのエクスポートとインポート
- **セキュリティスキャン** — [OSV.dev](https://osv.dev/) データベースを活用した実際の依存関係の脆弱性スキャン。`.csproj`（NuGet）、`package.json`（npm）、`requirements.txt`（PyPI）、`Cargo.toml`（Rust）、`Gemfile`（Ruby）、`composer.json`（PHP）、`go.mod`（Go）、`pom.xml`（Maven/Java）、`pubspec.yaml`（Dart/Flutter）から依存関係を自動抽出し、既知の CVE をチェック。重大度、修正バージョン、アドバイザリリンクをレポート。さらにドラフト/公開/クローズワークフロー付きの手動セキュリティアドバイザリ
- **Secret Scanning** — すべてのプッシュを自動スキャンし、漏洩した認証情報（AWS キー、GitHub/GitLab トークン、Slack トークン、秘密鍵、API キー、JWT、接続文字列など）を検出。20 の組み込みパターンと完全な正規表現サポート。オンデマンドのリポジトリ全体スキャン。解決/誤検知ワークフロー付きアラート。API 経由でカスタムパターンを設定可能
- **Dependabot-Style Auto-Update PRs** — 古い依存関係を自動チェックし、更新用の Pull Request を作成。NuGet、npm、PyPI エコシステムをサポート。スケジュール設定可能（毎日/毎週/毎月）、リポジトリごとのオープン PR 上限
- **Repository Insights (Traffic)** — クローン/フェッチ数、ページビュー、ユニーク訪問者、トップリファラー、人気コンテンツパスを追跡。Insights タブに 14 日間のサマリー付きトラフィックチャート。90 日間保持の日次集計。プライバシーのため IP アドレスはハッシュ化
- **ダークモード** — ヘッダーのトグルによるダーク/ライトモードの完全サポート
- **多言語 / i18n** — 全28ページにわたる836のリソースキーによる完全なローカライゼーション。11言語を同梱: 英語、スペイン語、フランス語、ドイツ語、日本語、韓国語、中国語（簡体字）、ポルトガル語、ロシア語、イタリア語、トルコ語。`SharedResource.{locale}.resx` ファイルを作成して言語を追加可能。ヘッダーの言語ピッカーで切り替え
- **Swagger / OpenAPI** — `/swagger` でインタラクティブな API ドキュメントを提供。すべての REST エンドポイントを検索・テスト可能
- **Mermaid ダイアグラム** — Markdown ファイルでの Mermaid ダイアグラム描画（フローチャート、シーケンス図、ガントチャートなど）
- **数式レンダリング** — Markdown 内の LaTeX/KaTeX 数式表現（`$inline$` および `$$display$$` 構文）
- **CSV/TSV ビューア** — CSV および TSV ファイルを生テキストではなく、フォーマット済みのソート可能なテーブルとして表示
- **Jupyter Notebook レンダリング** — `.ipynb` ファイルをコードセル、Markdown、出力、インライン画像を含むフォーマット済みノートブックとして表示
- **リポジトリ移管** — リポジトリの設定からリポジトリの所有権を別のユーザーまたは組織に移管
- **デフォルトブランチ設定** — 設定タブからリポジトリごとにデフォルトブランチを変更
- **Rename Repository** — Settings からリポジトリ名を変更し、すべての参照（Issues、PRs、スター、Webhooks、Secrets など）を自動更新
- **User-Level Secrets** — ユーザーが所有するすべてのリポジトリで共有される暗号化された Secrets。Settings > Secrets から管理
- **Organization-Level Secrets** — 組織内のすべてのリポジトリで共有される暗号化された Secrets。組織の Secrets タブから管理
- **Repository Pinning** — ユーザープロフィールページにお気に入りのリポジトリを最大6つピン留めして素早くアクセス
- **Git Hooks Management** — リポジトリごとのサーバーサイド Git Hooks（pre-receive、update、post-receive、post-update、pre-push）を表示・編集・管理するWeb UI
- **Protected File Patterns** — 特定のファイルへの変更にレビュー承認を必要とするglobパターンによるブランチ保護ルール（例: `*.lock`、`migrations/**`、`.github/workflows/*`）
- **External Issue Tracker** — リポジトリを外部の Issue トラッカー（Jira、Linear など）にリンクするよう設定。カスタム URL パターン対応
- **Federation (NodeInfo/WebFinger)** — NodeInfo 2.0 ディスカバリ、WebFinger、host-meta によるインスタンス間の発見可能性
- **Distributed CI Runners** — 外部ランナーが API 経由で登録し、キューに入ったジョブをポーリングし、結果を報告可能

## 技術スタック

| コンポーネント | 技術 |
|-----------|-----------|
| バックエンド | ASP.NET Core 10.0 |
| フロントエンド | Blazor Server（インタラクティブサーバーサイドレンダリング） |
| データベース | SQLite（デフォルト）または Entity Framework Core 10 経由の PostgreSQL |
| Git エンジン | LibGit2Sharp |
| 認証 | BCrypt パスワードハッシュ、セッションベース認証、PAT トークン、OAuth2（8プロバイダー + プロバイダーモード）、TOTP 2FA、WebAuthn/パスキー、LDAP/AD、SSPI |
| SSH サーバー | 組み込み SSH2 プロトコル実装（ECDH、AES-CTR、HMAC-SHA2） |
| Markdown | Markdig |
| CI/CD | Docker.DotNet、YamlDotNet |
| 監視 | Prometheus メトリクス |

## クイックスタート

### 前提条件

- [Docker](https://docs.docker.com/get-docker/)（推奨）
- またはローカル開発用に [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git

### Docker（推奨）

Docker Hub からプルして実行:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> ポート 2222 はオプションです。Admin > Settings で組み込み SSH サーバーを有効にする場合のみ必要です。

または Docker Compose を使用:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

アプリは **http://localhost:8080** で利用可能になります。

> **デフォルトの認証情報**: `admin` / `admin`
>
> 初回ログイン後、管理ダッシュボードから**すぐにデフォルトパスワードを変更してください**。

### ローカル実行

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

アプリは **http://localhost:5146** で起動します。

### 環境変数

| 変数 | 説明 | デフォルト |
|----------|-------------|---------|
| `Database__Provider` | データベースエンジン: `sqlite` または `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | データベース接続文字列 | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Git リポジトリが保存されるディレクトリ | `/repos` |
| `Git__RequireAuth` | Git HTTP 操作に認証を必須にする | `true` |
| `Git__Users__<username>` | Git HTTP Basic Auth ユーザーのパスワードを設定 | — |
| `RESET_ADMIN_PASSWORD` | 起動時の緊急管理者パスワードリセット | — |
| `Secrets__EncryptionKey` | リポジトリシークレット用のカスタム暗号化キー | DB 接続文字列から導出 |
| `Ssh__DataDir` | SSH データ（ホストキー、authorized_keys）のディレクトリ | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | 生成される authorized_keys ファイルのパス | `<DataDir>/authorized_keys` |

> **注意:** 組み込み SSH サーバーのポートと LDAP 設定は、環境変数ではなく管理ダッシュボード（Admin > Settings）で設定します。これにより、再デプロイなしで変更できます。

## 使い方

### 1. サインイン

アプリを開いて **Sign In** をクリックします。初回インストール時はデフォルトの認証情報（`admin` / `admin`）を使用します。**Admin** ダッシュボードから追加ユーザーを作成するか、Admin > Settings でユーザー登録を有効にします。

### 2. リポジトリの作成

ホームページの緑色の **New** ボタンをクリックし、名前を入力して **Create** をクリックします。サーバー上にベア Git リポジトリが作成され、クローン、プッシュ、Web UI での管理が可能になります。

### 3. クローンとプッシュ

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Git HTTP 認証が有効な場合、`Git__Users__<username>` 環境変数で設定した認証情報の入力を求められます。これらは Web UI のログインとは別のものです。

### 4. IDE からのクローン

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > `http://localhost:8080/git/MyRepo.git` を貼り付け

**Visual Studio**: **Git > Clone Repository** > URL を貼り付け

**JetBrains**: **File > New > Project from Version Control** > URL を貼り付け

### 5. Web エディタ

ブラウザ上で直接ファイルを編集できます:
- リポジトリに移動し、任意のファイルをクリックして **Edit** をクリック
- **Add files > Create new file** でローカルクローンなしにファイルを追加
- **Add files > Upload files/folder** でマシンからアップロード

### 6. コンテナレジストリ

サーバーに直接 Docker/OCI イメージをプッシュ・プルできます:

```bash
# ログイン（Settings > Access Tokens から Personal Access Token を使用）
docker login localhost:8080 -u youruser

# イメージのプッシュ
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# イメージのプル
docker pull localhost:8080/myapp:v1
```

> **注意:** Docker はデフォルトで HTTPS を必要とします。HTTP の場合は、`~/.docker/daemon.json` の Docker の `insecure-registries` にサーバーを追加してください:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. パッケージレジストリ

**NuGet (.NET パッケージ):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (Node.js パッケージ):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (Python パッケージ):**
```bash
# パッケージのインストール
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# twine でアップロード
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

**Maven (Java/JVM パッケージ):**
```xml
<!-- pom.xml にリポジトリを追加 -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- settings.xml に認証情報を追加 -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**汎用（任意のバイナリ）:**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Web UI の `/packages` ですべてのパッケージを閲覧できます。

### 8. Pages（静的サイトホスティング）

リポジトリのブランチから静的 Web サイトを配信:

1. リポジトリの **Settings** タブに移動し、**Pages** を有効化
2. ブランチを設定（デフォルト: `gh-pages`）
3. そのブランチに HTML/CSS/JS をプッシュ
4. `http://localhost:8080/pages/{username}/{repo}/` にアクセス

### 9. プッシュ通知

**Admin > System Settings** で Ntfy または Gotify を設定すると、Issue、PR、コメントが作成された際にスマートフォンやデスクトップにプッシュ通知を受け取れます。ユーザーは **Settings > Notifications** でオプトイン/オプトアウトできます。

### 10. SSH 鍵認証

パスワード不要の Git 操作のために SSH 鍵を使用します。2つのオプションがあります:

#### オプション A: 組み込み SSH サーバー（推奨）

外部の SSH デーモンは不要 — MyPersonalGit が独自の SSH サーバーを実行します:

1. **Admin > Settings** に移動し、**Built-in SSH Server** を有効化
2. SSH ポートを設定（デフォルト: 2222）— システム SSH を使用していない場合は 22 を使用
3. 設定を保存しサーバーを再起動（ポート変更には再起動が必要）
4. **Settings > SSH Keys** に移動し、公開鍵を追加（`~/.ssh/id_ed25519.pub`、`~/.ssh/id_rsa.pub`、または `~/.ssh/id_ecdsa.pub`）
5. SSH 経由でクローン:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

組み込み SSH サーバーは ECDH-SHA2-NISTP256 鍵交換、AES-128/256-CTR 暗号化、HMAC-SHA2-256、および Ed25519、RSA、ECDSA 鍵による公開鍵認証をサポートします。

#### オプション B: システム OpenSSH

システムの SSH デーモンを使用する場合:

1. **Settings > SSH Keys** に移動し、公開鍵を追加
2. MyPersonalGit は登録されたすべての SSH 鍵から `authorized_keys` ファイルを自動的に管理
3. サーバーの OpenSSH が生成された authorized_keys ファイルを使用するよう設定:
   ```
   # /etc/ssh/sshd_config 内
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. SSH 経由でクローン:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

SSH 認証サービスは OpenSSH の `AuthorizedKeysCommand` ディレクティブで使用するために `/api/ssh/authorized-keys` で API も公開しています。

### 11. LDAP / Active Directory 認証

組織の LDAP ディレクトリまたは Active Directory ドメインに対してユーザーを認証:

1. **Admin > Settings** に移動し、**LDAP / Active Directory Authentication** までスクロール
2. LDAP を有効にし、サーバーの詳細を入力:
   - **Server**: LDAP サーバーのホスト名（例: `dc01.corp.local`）
   - **Port**: LDAP は 389、LDAPS は 636
   - **SSL/TLS**: LDAPS の場合は有効化、プレーン接続のアップグレードには StartTLS を使用
3. ユーザー検索用のサービスアカウントを設定:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password**: サービスアカウントのパスワード
4. 検索パラメータを設定:
   - **Search Base DN**: `OU=Users,DC=corp,DC=local`
   - **User Filter**: AD の場合 `(sAMAccountName={0})`、OpenLDAP の場合 `(uid={0})`
5. LDAP 属性をユーザーフィールドにマッピング:
   - **Username**: `sAMAccountName`（AD）または `uid`（OpenLDAP）
   - **Email**: `mail`
   - **Display Name**: `displayName`
6. オプションで **Admin Group DN** を設定 — このグループのメンバーは自動的に管理者に昇格
7. **Test LDAP Connection** をクリックして設定を確認
8. 設定を保存

ユーザーはログインページでドメイン認証情報を使用してサインインできるようになります。初回ログイン時に、ディレクトリから同期された属性でローカルアカウントが自動作成されます。LDAP 認証は Git HTTP 操作（クローン/プッシュ）にも使用されます。

### 12. リポジトリシークレット

CI/CD ワークフローで使用するためにリポジトリに暗号化されたシークレットを追加:

1. リポジトリの **Settings** タブに移動
2. **Secrets** カードまでスクロールし、**Add secret** をクリック
3. 名前（例: `DEPLOY_TOKEN`）と値を入力 — 値は AES-256 で暗号化されます
4. シークレットはすべてのワークフロー実行時に環境変数として自動的に注入されます

ワークフローでのシークレットの参照:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO ログイン

外部 ID プロバイダーでサインイン:

1. **Admin > OAuth / SSO** に移動し、有効にするプロバイダーを設定
2. プロバイダーの開発者コンソールから **Client ID** と **Client Secret** を入力
3. **Enable** にチェック — 両方の認証情報が入力されたプロバイダーのみがログインページに表示されます
4. 各プロバイダーのコールバック URL は管理パネルに表示されます（例: `https://yourserver/oauth/callback/github`）

サポートされるプロバイダー: GitHub、Google、Microsoft、GitLab、Bitbucket、Facebook、Discord、Twitter/X。

ユーザーは **Settings > Linked Accounts** で複数のプロバイダーをアカウントにリンクできます。

### 14. リポジトリのインポート

完全な履歴を含めて外部ソースからリポジトリをインポート:

1. ホームページの **Import** をクリック
2. ソースタイプを選択（Git URL、GitHub、GitLab、または Bitbucket）
3. リポジトリの URL を入力し、プライベートリポジトリの場合はオプションで認証トークンを入力
4. GitHub/GitLab/Bitbucket のインポートでは、オプションで Issue とプルリクエストもインポート可能
5. Import ページでリアルタイムにインポートの進捗を追跡

### 15. フォークとアップストリーム同期

リポジトリをフォークして同期を維持:

1. 任意のリポジトリページで **Fork** ボタンをクリック
2. ユーザー名の下にオリジナルへのリンク付きでフォークが作成されます
3. 「forked from」バッジの横にある **Sync fork** をクリックしてアップストリームから最新の変更をプル

### 16. CI/CD 自動リリース

MyPersonalGit には、main へのプッシュごとに自動でタグ付け、リリース、Docker イメージのプッシュを行う組み込み CI/CD パイプラインが含まれています。ワークフローはプッシュで自動トリガー — 外部 CI サービスは不要です。

**仕組み:**
1. `main` へのプッシュが `.github/workflows/release.yml` を自動トリガー
2. パッチバージョンをバンプ（`v1.15.1` -> `v1.15.2`）し、git タグを作成
3. Docker Hub にログインし、イメージをビルドして `:latest` と `:vX.Y.Z` の両方でプッシュ

**セットアップ:**
1. MyPersonalGit でリポジトリの **Settings > Secrets** に移動
2. Docker Hub アクセストークンで `DOCKERHUB_TOKEN` という名前のシークレットを追加
3. MyPersonalGit コンテナに Docker ソケットがマウントされていることを確認（`-v /var/run/docker.sock:/var/run/docker.sock`）
4. main にプッシュ — ワークフローが自動的にトリガーされます

**GitHub Actions 互換性:**
同じワークフロー YAML は GitHub Actions でもそのまま動作します — 変更は不要です。MyPersonalGit は実行時に `uses:` アクションを同等のシェルコマンドに変換します:

| GitHub Action | MyPersonalGit での変換 |
|---|---|
| `actions/checkout@v4` | リポジトリは既に `/workspace` にクローン済み |
| `actions/setup-dotnet@v4` | 公式インストールスクリプト経由で .NET SDK をインストール |
| `actions/setup-node@v4` | NodeSource 経由で Node.js をインストール |
| `actions/setup-python@v5` | apt/apk 経由で Python をインストール |
| `actions/setup-java@v4` | apt/apk 経由で OpenJDK をインストール |
| `docker/login-action@v3` | stdin パスワード付きの `docker login` |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op（デフォルトビルダーを使用） |
| `softprops/action-gh-release@v2` | データベースに実際の Release エンティティを作成 |
| `${{ secrets.X }}` | `$X` 環境変数 |
| `${{ steps.X.outputs.Y }}` | `$Y` 環境変数 |
| `${{ github.sha }}` | `$GITHUB_SHA` 環境変数 |

**並列ジョブ:**
ジョブはデフォルトで並列実行されます。依存関係を宣言するには `needs:` を使用:
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
`needs:` のないジョブは即座に開始されます。依存先のいずれかが失敗するとジョブはキャンセルされます。

**条件付きステップ:**
`if:` を使用してステップの実行を制御:
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
サポートされる式: `always()`、`success()`（デフォルト）、`failure()`、`cancelled()`、`true`、`false`。

**ステップ出力:**
ステップは `$GITHUB_OUTPUT` を使用して後続のステップに値を渡せます:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**マトリックスビルド:**
`strategy.matrix` を使用してジョブを複数の組み合わせに展開:
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
これにより4つのジョブが作成されます: `test (ubuntu-latest, 1.0)`、`test (ubuntu-latest, 2.0)` など。すべて並列実行されます。

**入力付き手動トリガー (`workflow_dispatch`):**
手動トリガー時に UI にフォームとして表示される型指定された入力を定義:
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
入力値は `INPUT_<NAME>` 環境変数（大文字）として注入されます。

**ジョブタイムアウト:**
ジョブに `timeout-minutes` を設定し、実行時間が長すぎる場合に自動的に失敗:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
デフォルトのタイムアウトは 360分（6時間）で、GitHub Actions と同じです。

**ジョブレベルの条件付き実行:**
`if:` をジョブに使用して条件に基づいてスキップ:
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

**ジョブ出力:**
ジョブは `outputs:` を使用して下流のジョブに値を渡せます:
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

**エラー時の続行:**
ステップが失敗してもジョブを失敗させない:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**パスフィルタリング:**
特定のファイルが変更された場合のみワークフローをトリガー:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # または paths-ignore を使用:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**作業ディレクトリ:**
コマンドの実行場所を設定:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # src/app で実行
      - run: npm test
        working-directory: tests  # デフォルトを上書き
```

**ワークフローの再実行:**
完了、失敗、またはキャンセルされたワークフロー実行で **Re-run** ボタンをクリックし、同じジョブ、ステップ、設定で新しい実行を作成します。

**プルリクエストワークフロー:**
`on: pull_request` のワークフローは、ドラフトでない PR が作成されると自動トリガーされ、ソースブランチに対してチェックを実行します。

**コミットステータスチェック:**
ワークフローはコミットステータス（pending/success/failure）を自動設定するため、PR でビルド結果を確認し、ブランチ保護で必須チェックを強制できます。

**ワークフローのキャンセル:**
Actions UI で実行中またはキューに入っているワークフローの **Cancel** ボタンをクリックして即座に停止できます。

**ステータスバッジ:**
README やその他の場所にビルドステータスバッジを埋め込み:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
ワークフロー名でフィルタリング: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom フィード

標準的な Atom フィードを使用して、任意の RSS リーダーでリポジトリのアクティビティを購読:

```
# リポジトリのコミット
http://localhost:8080/api/feeds/MyRepo/commits.atom

# リポジトリのリリース
http://localhost:8080/api/feeds/MyRepo/releases.atom

# リポジトリのタグ
http://localhost:8080/api/feeds/MyRepo/tags.atom

# ユーザーアクティビティ
http://localhost:8080/api/feeds/users/admin/activity.atom

# グローバルアクティビティ（全リポジトリ）
http://localhost:8080/api/feeds/global/activity.atom
```

パブリックリポジトリには認証不要です。これらの URL を任意のフィードリーダー（Feedly、Miniflux、FreshRSS など）に追加して、変更の通知を受け取れます。

## データベース設定

MyPersonalGit はデフォルトで **SQLite** を使用 — 設定不要、単一ファイルのデータベースで、個人利用や小規模チームに最適です。

大規模なデプロイメント（多数の同時接続ユーザー、高可用性、または既に PostgreSQL を運用中の場合）には、**PostgreSQL** に切り替えることができます:

### PostgreSQL の使用

**Docker Compose**（PostgreSQL には推奨）:
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

**環境変数のみ**（既に PostgreSQL サーバーがある場合）:
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

EF Core のマイグレーションは両方のプロバイダーで起動時に自動実行されます。手動でのスキーマセットアップは不要です。

### 管理ダッシュボードからの切り替え

Web UI から直接データベースプロバイダーを切り替えることもできます:

1. **Admin > Settings** に移動 — **Database** カードが上部にあります
2. プロバイダーのドロップダウンから **PostgreSQL** を選択
3. PostgreSQL の接続文字列を入力（例: `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`）
4. **Save Database Settings** をクリック
5. 変更を反映するためにアプリケーションを再起動

設定は `~/.mypersonalgit/database.json` に保存されます（データベース自体の外部に保存されるため、接続前に読み取り可能です）。

### データベースの選択

| | SQLite | PostgreSQL |
|---|---|---|
| **セットアップ** | 設定不要（デフォルト） | PostgreSQL サーバーが必要 |
| **最適な用途** | 個人利用、小規模チーム、NAS | 50人以上のチーム、高い同時接続性 |
| **バックアップ** | `.db` ファイルをコピー | 標準的な `pg_dump` |
| **同時接続** | シングルライター（ほとんどの用途で問題なし） | 完全なマルチライター |
| **移行** | N/A | プロバイダーを切り替えてアプリを実行（自動マイグレーション） |

## NAS へのデプロイ

MyPersonalGit は Docker 経由で NAS（QNAP、Synology など）でも快適に動作します:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Docker ソケットのマウントはオプションです — CI/CD ワークフローの実行が必要な場合のみ使用します。ポート 2222 は組み込み SSH サーバーを有効にする場合のみ必要です。

## 設定

すべての設定は `appsettings.json`、環境変数、または `/admin` の管理ダッシュボードで設定できます:

- データベースプロバイダー（SQLite または PostgreSQL）
- プロジェクトルートディレクトリ
- 認証要件
- ユーザー登録設定
- 機能トグル（Issues、Wiki、Projects、Actions）
- ユーザーごとの最大リポジトリサイズと数
- メール通知用の SMTP 設定
- プッシュ通知設定（Ntfy/Gotify）
- 組み込み SSH サーバー（有効/無効、ポート）
- LDAP/Active Directory 認証（サーバー、Bind DN、検索ベース、ユーザーフィルター、属性マッピング、管理者グループ）
- OAuth/SSO プロバイダー設定（プロバイダーごとの Client ID/Secret）

## プロジェクト構成

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout、NavMenu
    Pages/           # Blazor ページ（Home、RepoDetails、Issues、PRs、Packages など）
  Controllers/       # REST API エンドポイント（NuGet、npm、Generic、Registry など）
  Data/              # EF Core DbContext、サービス実装
  Models/            # ドメインモデル
  Migrations/        # EF Core マイグレーション
  Services/          # ミドルウェア（認証、Git HTTP バックエンド、Pages、Registry 認証）
    SshServer/       # 組み込み SSH サーバー（SSH2 プロトコル、ECDH、AES-CTR）
  Program.cs         # アプリの起動、DI、ミドルウェアパイプライン
MyPersonalGit.Tests/
  UnitTest1.cs       # InMemory データベースを使用した xUnit テスト
```

## テストの実行

```bash
dotnet test
```

## ライセンス

MIT
