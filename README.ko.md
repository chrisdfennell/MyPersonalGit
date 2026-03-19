🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

ASP.NET Core와 Blazor Server로 구축된 GitHub 스타일의 웹 인터페이스를 갖춘 셀프 호스팅 Git 서버입니다. 저장소 탐색, 이슈, 풀 리퀘스트, 위키, 프로젝트 등을 자신의 머신이나 서버에서 모두 관리할 수 있습니다.

![MyPersonalGit 스크린샷](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)

---

## 목차

- [기능](#기능)
- [기술 스택](#기술-스택)
- [빠른 시작](#빠른-시작)
  - [Docker (권장)](#docker-권장)
  - [로컬 실행](#로컬-실행)
  - [환경 변수](#환경-변수)
- [사용법](#사용법)
  - [로그인](#1-로그인)
  - [저장소 생성](#2-저장소-생성)
  - [클론 및 푸시](#3-클론-및-푸시)
  - [IDE에서 클론](#4-ide에서-클론)
  - [웹 에디터](#5-웹-에디터)
  - [컨테이너 레지스트리](#6-컨테이너-레지스트리)
  - [패키지 레지스트리](#7-패키지-레지스트리)
  - [Pages (정적 사이트)](#8-pages정적-사이트-호스팅)
  - [푸시 알림](#9-푸시-알림)
  - [SSH 키 인증](#10-ssh-키-인증)
  - [LDAP / Active Directory](#11-ldap--active-directory-인증)
  - [저장소 시크릿](#12-저장소-시크릿)
  - [OAuth / SSO 로그인](#13-oauth--sso-로그인)
  - [저장소 가져오기](#14-저장소-가져오기)
  - [포크 및 업스트림 동기화](#15-포크-및-업스트림-동기화)
  - [CI/CD 자동 릴리스](#16-cicd-자동-릴리스)
  - [RSS/Atom 피드](#17-rssatom-피드)
- [데이터베이스 설정](#데이터베이스-설정)
  - [PostgreSQL 사용](#postgresql-사용)
  - [관리 대시보드에서 전환](#관리-대시보드에서-전환)
  - [데이터베이스 선택](#데이터베이스-선택)
- [NAS에 배포](#nas에-배포)
- [설정](#설정)
- [프로젝트 구조](#프로젝트-구조)
- [테스트 실행](#테스트-실행)
- [라이선스](#라이선스)

---

## 기능

### 코드 및 저장소
- **저장소 관리** — 코드 브라우저, 파일 에디터, 커밋 히스토리, 브랜치, 태그를 갖춘 Git 저장소 생성, 탐색, 삭제
- **저장소 가져오기/마이그레이션** — GitHub, GitLab, Bitbucket 또는 임의의 Git URL에서 저장소 가져오기. 이슈 및 PR 가져오기도 선택적으로 가능. 진행률 추적이 포함된 백그라운드 처리
- **저장소 아카이브** — 저장소를 읽기 전용으로 표시(시각적 배지 포함). 아카이브된 저장소에 대한 푸시가 차단됩니다
- **Git Smart HTTP** — Basic Auth를 통한 HTTP 경유 클론, 페치, 푸시
- **내장 SSH 서버** — Git 작업을 위한 네이티브 SSH 서버 — 외부 OpenSSH 불필요. ECDH 키 교환, AES-CTR 암호화, 공개키 인증(RSA, ECDSA, Ed25519) 지원
- **SSH 키 인증** — 계정에 SSH 공개키를 추가하고 자동 관리되는 `authorized_keys`(또는 내장 SSH 서버)를 통해 SSH 경유 Git 작업을 인증
- **포크 및 업스트림 동기화** — 저장소를 포크하고 원클릭으로 업스트림과 동기화. UI에서 포크 관계를 확인 가능
- **Git LFS** — 바이너리 파일 추적을 위한 Large File Storage 지원
- **저장소 미러링** — 외부 Git 리모트와의 양방향 미러링
- **비교 뷰** — 선행/후행 커밋 수와 전체 diff 렌더링을 통한 브랜치 비교
- **언어 통계** — 각 저장소 페이지에 GitHub 스타일의 언어 분석 바
- **브랜치 보호** — 필수 리뷰, 상태 검사, 강제 푸시 방지, CODEOWNERS 승인 적용을 위한 구성 가능한 규칙
- **태그 보호** — 글로브 패턴 매칭과 사용자별 허용 목록을 통한 태그 삭제, 강제 업데이트, 무단 생성으로부터의 보호
- **커밋 서명 검증** — 커밋 및 주석이 달린 태그의 GPG 서명 검증. UI에 "Verified" / "Signed" 배지 표시
- **저장소 레이블** — 저장소별 사용자 정의 색상의 레이블 관리. 템플릿에서 저장소를 생성할 때 레이블이 자동으로 복사됩니다
- **AGit Flow** — 푸시-투-리뷰 워크플로우: `git push origin HEAD:refs/for/main`으로 포크나 원격 브랜치 생성 없이 풀 리퀘스트를 생성. 이후 푸시에서 기존 오픈 PR을 업데이트
- **탐색** — 검색, 정렬, 토픽 필터링을 통해 접근 가능한 모든 저장소 탐색
- **검색** — 저장소, 이슈, PR, 코드 전체에 대한 전문 검색

### 협업
- **이슈 및 풀 리퀘스트** — 레이블, 다중 담당자, 마감일, 리뷰가 포함된 이슈와 PR의 생성, 댓글, 닫기/재개. 머지 커밋, 스쿼시, 리베이스 전략으로 PR 머지. 사이드 바이 사이드 diff 뷰를 통한 웹 기반 머지 충돌 해결
- **이슈 종속성** — 순환 종속성 감지가 포함된 이슈 간 "차단됨" 및 "차단 중" 관계 정의
- **이슈 고정 및 잠금** — 중요한 이슈를 목록 상단에 고정하고 대화를 잠가 추가 댓글을 방지
- **댓글 편집 및 삭제** — 이슈와 풀 리퀘스트에서 자신의 댓글을 편집 또는 삭제("(edited)" 표시 포함)
- **머지 충돌 해결** — 베이스/ours/theirs 뷰, 빠른 수락 버튼, 충돌 마커 유효성 검사를 갖춘 비주얼 에디터로 브라우저에서 직접 머지 충돌 해결
- **디스커션** — GitHub Discussions 스타일의 저장소별 스레드 형식 대화. 카테고리(General, Q&A, Announcements, Ideas, Show & Tell, Polls), 고정/잠금, 답변으로 표시, 업보트 지원
- **코드 리뷰 제안** — PR 인라인 리뷰에서 "변경 제안" 모드. 리뷰어가 diff에서 직접 코드 대체를 제안 가능
- **리액션 이모지** — 이슈, PR, 디스커션, 댓글에 대한 좋아요/싫어요, 하트, 웃음, 축하, 혼란, 로켓, 눈 리액션
- **CODEOWNERS** — 파일 경로 기반 PR 리뷰어 자동 할당. 머지 전 CODEOWNERS 승인을 필수로 하는 선택적 강제
- **저장소 템플릿** — 파일, 레이블, 이슈 템플릿, 브랜치 보호 규칙의 자동 복사를 통한 템플릿 기반 새 저장소 생성
- **초안 이슈 및 이슈 템플릿** — 초안 이슈(작업 진행 중) 생성 및 저장소별 기본 레이블이 포함된 재사용 가능한 이슈 템플릿(버그 리포트, 기능 요청) 정의
- **위키** — 수정 이력이 포함된 저장소별 Markdown 기반 위키 페이지
- **프로젝트** — 작업 정리를 위한 드래그 앤 드롭 카드가 있는 칸반 보드
- **스니펫** — 구문 강조와 다중 파일을 지원하는 코드 스니펫 공유(GitHub Gists와 유사)
- **조직 및 팀** — 멤버와 팀을 포함한 조직 생성, 저장소에 팀 권한 할당
- **세분화된 권한** — 저장소에 대한 세밀한 접근 제어를 위한 5단계 권한 모델(Read, Triage, Write, Maintain, Admin)
- **마일스톤** — 프로그레스 바와 마감일을 통한 마일스톤 기반 이슈 진행률 추적
- **커밋 댓글** — 선택적 파일/줄 참조가 포함된 개별 커밋에 대한 댓글
- **저장소 토픽** — 탐색 페이지에서의 발견과 필터링을 위한 저장소 토픽 태그 지정

### CI/CD 및 DevOps
- **CI/CD 러너** — `.github/workflows/*.yml`에서 워크플로우를 정의하고 Docker 컨테이너에서 실행. 푸시 및 풀 리퀘스트 이벤트에서 자동 트리거
- **GitHub Actions 호환** — 동일한 워크플로우 YAML이 MyPersonalGit과 GitHub Actions 양쪽에서 동작. `uses:` 액션(`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`)을 동등한 셸 명령으로 변환
- **`needs:`를 통한 병렬 작업** — 작업은 `needs:`로 종속성을 선언하고 독립적인 경우 병렬로 실행. 종속 작업은 전제 조건을 대기하며, 종속 대상이 실패하면 자동으로 취소
- **조건부 스텝 (`if:`)** — 스텝은 `if:` 표현식 지원: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. `if: failure()` 또는 `if: always()`의 정리 스텝은 이전 실패 후에도 실행
- **스텝 출력 (`$GITHUB_OUTPUT`)** — 스텝은 `$GITHUB_OUTPUT`에 `key=value` 또는 `key<<DELIMITER` 다중행 쌍을 작성 가능. 후속 스텝은 `${{ steps.X.outputs.Y }}` 구문과 호환되는 환경 변수로 수신
- **`github` 컨텍스트** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW`, `CI=true`가 모든 작업에 자동 주입
- **매트릭스 빌드** — `strategy.matrix`가 여러 변수 조합(예: OS x 버전)으로 작업을 확장. `fail-fast`와 `${{ matrix.X }}`의 `runs-on`, 스텝 명령, 스텝 이름에서의 치환 지원
- **`workflow_dispatch` 인풋** — 타입이 지정된 입력 매개변수(string, boolean, choice, number)를 통한 수동 트리거. 인풋이 있는 워크플로우를 트리거할 때 UI에 입력 폼 표시. 값은 `INPUT_*` 환경 변수로 주입
- **작업 타임아웃 (`timeout-minutes`)** — 작업에 `timeout-minutes`를 설정하여 제한 시간 초과 시 자동으로 실패 처리. 기본값: 360분(GitHub Actions와 동일)
- **작업 수준 `if:`** — 조건에 따라 전체 작업을 건너뛰기. `if: always()` 작업은 종속 대상이 실패해도 실행. 건너뛴 작업은 실행을 실패로 만들지 않음
- **작업 출력** — 작업은 `outputs:`를 선언하고 하위 `needs:` 작업이 `${{ needs.X.outputs.Y }}`를 통해 소비. 출력은 작업 완료 후 스텝 출력에서 해결
- **`continue-on-error`** — 작업을 실패시키지 않고 개별 스텝의 실패를 허용으로 표시. 선택적 유효성 검사나 알림 스텝에 유용
- **`on.push.paths` 필터** — 특정 파일이 변경된 경우에만 워크플로우를 트리거. 글로브 패턴(`src/**`, `*.ts`)과 제외를 위한 `paths-ignore:` 지원
- **워크플로우 재실행** — Actions UI에서 원클릭으로 실패, 성공 또는 취소된 워크플로우 실행을 재실행. 동일한 구성으로 새 실행 생성
- **`working-directory`** — 워크플로우 수준의 `defaults.run.working-directory` 또는 스텝별 `working-directory:`로 명령 실행 위치 제어
- **`defaults.run.shell`** — 워크플로우 또는 스텝별로 커스텀 셸 설정(`bash`, `sh`, `python3` 등)
- **`strategy.max-parallel`** — 동시 매트릭스 작업 실행 수 제한
- **`on.workflow_run`** — 워크플로우 체이닝: 워크플로우 A 완료 시 워크플로우 B 트리거. 워크플로우 이름과 `types: [completed]`로 필터
- **자동 릴리스 생성** — `softprops/action-gh-release`가 태그, 제목, 체인지로그 본문, 프리릴리스/드래프트 플래그를 포함한 실제 Release 엔티티를 생성. 소스 코드 아카이브(ZIP 및 TAR.GZ)가 다운로드 가능한 에셋으로 자동 첨부
- **자동 릴리스 파이프라인** — main에 대한 모든 푸시에서 버전 자동 태그, 체인지로그 생성, Docker Hub로 Docker 이미지 푸시를 수행하는 내장 워크플로우
- **커밋 상태 검사** — 워크플로우가 커밋에 pending/success/failure 상태를 자동 설정. 풀 리퀘스트에서 확인 가능
- **워크플로우 취소** — Actions UI에서 실행 중이거나 대기열에 있는 워크플로우 취소
- **동시성 제어** — 새로운 푸시가 동일한 워크플로우의 대기열에 있는 실행을 자동으로 취소
- **워크플로우 환경 변수** — YAML의 워크플로우, 작업 또는 스텝 수준에서 `env:` 설정
- **상태 배지** — 워크플로우 및 커밋 상태용 임베드 가능한 SVG 배지(`/api/badge/{repo}/workflow`)
- **아티팩트 다운로드** — Actions UI에서 빌드 아티팩트 직접 다운로드
- **시크릿 관리** — 암호화된 저장소 시크릿(AES-256)을 CI/CD 워크플로우 실행에 환경 변수로 주입
- **Webhooks** — 저장소 이벤트 시 외부 서비스 트리거
- **Prometheus 메트릭** — 모니터링을 위한 내장 `/metrics` 엔드포인트

### 패키지 및 컨테이너 호스팅
- **컨테이너 레지스트리** — `docker push`와 `docker pull`을 통한 Docker/OCI 이미지 호스팅(OCI Distribution Spec)
- **NuGet 레지스트리** — 완전한 NuGet v3 API(서비스 인덱스, 검색, 푸시, 복원)를 통한 .NET 패키지 호스팅
- **npm 레지스트리** — 표준 npm publish/install을 통한 Node.js 패키지 호스팅
- **PyPI 레지스트리** — PEP 503 Simple API, JSON 메타데이터 API, `twine upload` 호환 Python 패키지 호스팅
- **Maven 레지스트리** — 표준 Maven 저장소 레이아웃, `maven-metadata.xml` 생성, `mvn deploy` 지원을 통한 Java/JVM 패키지 호스팅
- **범용 패키지** — REST API를 통한 임의의 바이너리 아티팩트 업로드 및 다운로드

### 정적 사이트
- **Pages** — 저장소 브랜치에서 직접 정적 웹사이트 서비스(GitHub Pages와 유사). `/pages/{owner}/{repo}/`에서 접근 가능

### RSS/Atom 피드
- **저장소 피드** — 저장소별 커밋, 릴리스, 태그에 대한 Atom 피드(`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **사용자 활동 피드** — 사용자별 활동 피드(`/api/feeds/users/{username}/activity.atom`)
- **전역 활동 피드** — 사이트 전체 활동 피드(`/api/feeds/global/activity.atom`)

### 알림
- **앱 내 알림** — 멘션, 댓글, 저장소 활동
- **푸시 알림** — Ntfy 및 Gotify 통합을 통한 실시간 모바일/데스크톱 알림(사용자별 옵트인)

### 인증
- **OAuth2 / SSO** — GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord 또는 Twitter/X로 로그인. 관리자가 관리 대시보드에서 각 프로바이더의 Client ID와 Secret을 설정 — 자격 증명이 입력된 프로바이더만 사용자에게 표시
- **OAuth2 프로바이더** — 다른 앱이 "MyPersonalGit으로 로그인"을 사용할 수 있도록 아이덴티티 프로바이더로 동작. PKCE가 포함된 Authorization Code 플로우, 토큰 갱신, userinfo 엔드포인트, OpenID Connect 디스커버리(`.well-known/openid-configuration`) 구현
- **LDAP / Active Directory** — LDAP 디렉토리 또는 Active Directory 도메인에 대한 사용자 인증. 첫 로그인 시 동기화된 속성(이메일, 표시 이름)으로 사용자 자동 프로비저닝. 그룹 기반 관리자 승격, SSL/TLS, StartTLS 지원
- **SSPI / Windows 통합 인증** — Negotiate/NTLM을 통한 Windows 도메인 사용자의 투명한 싱글 사인온. 도메인 사용자는 자격 증명 입력 없이 자동으로 인증. Admin > Settings에서 활성화(Windows 전용)
- **2단계 인증** — 인증 앱 지원과 복구 코드가 포함된 TOTP 기반 2FA
- **WebAuthn / 패스키** — 2단계로서의 FIDO2 하드웨어 보안 키 및 패스키 지원. YubiKey, 플랫폼 인증기(Face ID, Windows Hello, Touch ID) 및 기타 FIDO2 디바이스 등록. 복제 키 감지를 위한 서명 카운트 검증
- **연결된 계정** — 설정에서 여러 OAuth 프로바이더를 계정에 연결 가능

### 관리
- **관리 대시보드** — 시스템 설정(데이터베이스 프로바이더, SSH 서버, LDAP/AD, 푸터 페이지 포함), 사용자 관리, 감사 로그, 통계
- **사용자 정의 가능한 푸터 페이지** — Admin > Settings에서 Markdown 콘텐츠를 편집할 수 있는 이용 약관, 개인정보 보호정책, 문서, 연락처 페이지
- **사용자 프로필** — 사용자별 기여 히트맵, 활동 피드, 통계
- **개인 액세스 토큰** — 구성 가능한 스코프와 선택적 라우트 수준 제한(`/api/packages/**`와 같은 글로브 패턴으로 특정 API 경로에 대한 토큰 접근 제한)을 갖춘 토큰 기반 API 인증
- **백업 및 복원** — 서버 데이터 내보내기 및 가져오기
- **보안 스캐닝** — [OSV.dev](https://osv.dev/) 데이터베이스를 기반으로 한 실제 종속성 취약점 스캐닝. `.csproj`(NuGet), `package.json`(npm), `requirements.txt`(PyPI)에서 종속성을 자동 추출하고 알려진 CVE와 대조. 심각도, 수정 버전, 권고 링크를 보고. 또한 초안/게시/닫기 워크플로우가 포함된 수동 보안 권고
- **다크 모드** — 헤더의 토글을 통한 완전한 다크/라이트 모드 지원
- **다국어 / i18n** — 전체 27페이지, 676개의 리소스 키에 걸친 완전한 현지화. 11개 언어 기본 제공: 영어, 스페인어, 프랑스어, 독일어, 일본어, 한국어, 중국어(간체), 포르투갈어, 러시아어, 이탈리아어, 터키어. `SharedResource.{locale}.resx` 파일을 만들어 추가 가능

## 기술 스택

| 컴포넌트 | 기술 |
|-----------|-----------|
| 백엔드 | ASP.NET Core 10.0 |
| 프런트엔드 | Blazor Server (인터랙티브 서버 사이드 렌더링) |
| 데이터베이스 | SQLite (기본) 또는 Entity Framework Core 10을 통한 PostgreSQL |
| Git 엔진 | LibGit2Sharp |
| 인증 | BCrypt 패스워드 해싱, 세션 기반 인증, PAT 토큰, OAuth2 (8개 프로바이더 + 프로바이더 모드), TOTP 2FA, WebAuthn/패스키, LDAP/AD, SSPI |
| SSH 서버 | 내장 SSH2 프로토콜 구현 (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| 모니터링 | Prometheus 메트릭 |

## 빠른 시작

### 사전 요구 사항

- [Docker](https://docs.docker.com/get-docker/) (권장)
- 또는 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git (로컬 개발용)

### Docker (권장)

Docker Hub에서 풀하여 실행:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> 포트 2222는 선택 사항입니다 — Admin > Settings에서 내장 SSH 서버를 활성화한 경우에만 필요합니다.

또는 Docker Compose를 사용:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

앱은 **http://localhost:8080**에서 사용할 수 있습니다.

> **기본 자격 증명**: `admin` / `admin`
>
> 첫 로그인 후 관리 대시보드에서 **즉시 기본 비밀번호를 변경하세요**.

### 로컬 실행

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

앱은 **http://localhost:5146**에서 시작됩니다.

### 환경 변수

| 변수 | 설명 | 기본값 |
|----------|-------------|---------|
| `Database__Provider` | 데이터베이스 엔진: `sqlite` 또는 `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | 데이터베이스 연결 문자열 | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Git 저장소 저장 디렉토리 | `/repos` |
| `Git__RequireAuth` | Git HTTP 작업에 인증 필요 | `true` |
| `Git__Users__<username>` | Git HTTP Basic Auth 사용자의 비밀번호 설정 | — |
| `RESET_ADMIN_PASSWORD` | 시작 시 긴급 관리자 비밀번호 재설정 | — |
| `Secrets__EncryptionKey` | 저장소 시크릿용 커스텀 암호화 키 | DB 연결 문자열에서 파생 |
| `Ssh__DataDir` | SSH 데이터(호스트 키, authorized_keys) 디렉토리 | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | 생성된 authorized_keys 파일 경로 | `<DataDir>/authorized_keys` |

> **참고:** 내장 SSH 서버 포트와 LDAP 설정은 환경 변수가 아닌 관리 대시보드(Admin > Settings)에서 구성됩니다. 이를 통해 재배포 없이 변경할 수 있습니다.

## 사용법

### 1. 로그인

앱을 열고 **Sign In**을 클릭합니다. 신규 설치 시 기본 자격 증명(`admin` / `admin`)을 사용하세요. 추가 사용자는 **Admin** 대시보드에서 또는 Admin > Settings에서 사용자 등록을 활성화하여 생성할 수 있습니다.

### 2. 저장소 생성

홈페이지의 녹색 **New** 버튼을 클릭하고 이름을 입력한 후 **Create**를 클릭합니다. 이렇게 하면 클론, 푸시, 웹 UI를 통한 관리가 가능한 베어 Git 저장소가 서버에 생성됩니다.

### 3. 클론 및 푸시

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Git HTTP 인증이 활성화된 경우 `Git__Users__<username>` 환경 변수로 설정된 자격 증명을 입력하라는 메시지가 표시됩니다. 이는 웹 UI 로그인과 별개입니다.

### 4. IDE에서 클론

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > `http://localhost:8080/git/MyRepo.git` 붙여넣기

**Visual Studio**: **Git > Clone Repository** > URL 붙여넣기

**JetBrains**: **File > New > Project from Version Control** > URL 붙여넣기

### 5. 웹 에디터

브라우저에서 직접 파일을 편집할 수 있습니다:
- 저장소로 이동하여 파일을 클릭한 후 **Edit**을 클릭
- **Add files > Create new file**로 로컬 클론 없이 파일 추가
- **Add files > Upload files/folder**로 머신에서 업로드

### 6. 컨테이너 레지스트리

Docker/OCI 이미지를 서버에 직접 푸시 및 풀:

```bash
# 로그인 (Settings > Access Tokens의 개인 액세스 토큰 사용)
docker login localhost:8080 -u youruser

# 이미지 푸시
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# 이미지 풀
docker pull localhost:8080/myapp:v1
```

> **참고:** Docker는 기본적으로 HTTPS를 요구합니다. HTTP의 경우 Docker의 `~/.docker/daemon.json`에 있는 `insecure-registries`에 서버를 추가하세요:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. 패키지 레지스트리

**NuGet (.NET 패키지):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (Node.js 패키지):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (Python 패키지):**
```bash
# 패키지 설치
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# twine으로 업로드
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

**Maven (Java/JVM 패키지):**
```xml
<!-- pom.xml에 저장소 추가 -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- settings.xml에 자격 증명 추가 -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**범용 (임의의 바이너리):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

웹 UI의 `/packages`에서 모든 패키지를 탐색할 수 있습니다.

### 8. Pages (정적 사이트 호스팅)

저장소 브랜치에서 정적 웹사이트 서비스:

1. 저장소의 **Settings** 탭에서 **Pages**를 활성화
2. 브랜치를 설정 (기본값: `gh-pages`)
3. 해당 브랜치에 HTML/CSS/JS를 푸시
4. `http://localhost:8080/pages/{username}/{repo}/` 방문

### 9. 푸시 알림

**Admin > System Settings**에서 Ntfy 또는 Gotify를 설정하면 이슈, PR, 댓글 생성 시 휴대폰이나 데스크톱에서 푸시 알림을 받을 수 있습니다. 사용자는 **Settings > Notifications**에서 옵트인/아웃할 수 있습니다.

### 10. SSH 키 인증

비밀번호 없는 Git 작업을 위해 SSH 키를 사용합니다. 두 가지 옵션이 있습니다:

#### 옵션 A: 내장 SSH 서버 (권장)

외부 SSH 데몬 불필요 — MyPersonalGit이 자체 SSH 서버를 실행합니다:

1. **Admin > Settings**에서 **Built-in SSH Server**를 활성화
2. SSH 포트 설정 (기본값: 2222) — 시스템 SSH를 실행하지 않는 경우 22 사용
3. 설정을 저장하고 서버를 재시작 (포트 변경 시 재시작 필요)
4. **Settings > SSH Keys**에서 공개키 추가 (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` 또는 `~/.ssh/id_ecdsa.pub`)
5. SSH로 클론:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

내장 SSH 서버는 ECDH-SHA2-NISTP256 키 교환, AES-128/256-CTR 암호화, HMAC-SHA2-256, Ed25519, RSA, ECDSA 키를 통한 공개키 인증을 지원합니다.

#### 옵션 B: 시스템 OpenSSH

시스템의 SSH 데몬을 사용하는 경우:

1. **Settings > SSH Keys**에서 공개키 추가
2. MyPersonalGit이 등록된 모든 SSH 키로부터 `authorized_keys` 파일을 자동 관리
3. 서버의 OpenSSH를 생성된 authorized_keys 파일을 사용하도록 구성:
   ```
   # /etc/ssh/sshd_config 내
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. SSH로 클론:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

SSH 인증 서비스는 OpenSSH의 `AuthorizedKeysCommand` 지시문용으로 `/api/ssh/authorized-keys`에 API를 노출합니다.

### 11. LDAP / Active Directory 인증

조직의 LDAP 디렉토리 또는 Active Directory 도메인에 대해 사용자를 인증:

1. **Admin > Settings**에서 **LDAP / Active Directory Authentication**으로 스크롤
2. LDAP를 활성화하고 서버 세부 정보를 입력:
   - **Server**: LDAP 서버 호스트명 (예: `dc01.corp.local`)
   - **Port**: LDAP는 389, LDAPS는 636
   - **SSL/TLS**: LDAPS의 경우 활성화, 일반 연결 업그레이드에는 StartTLS 사용
3. 사용자 검색을 위한 서비스 계정 구성:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password**: 서비스 계정 비밀번호
4. 검색 매개변수 설정:
   - **Search Base DN**: `OU=Users,DC=corp,DC=local`
   - **User Filter**: AD의 경우 `(sAMAccountName={0})`, OpenLDAP의 경우 `(uid={0})`
5. LDAP 속성을 사용자 필드에 매핑:
   - **Username**: `sAMAccountName` (AD) 또는 `uid` (OpenLDAP)
   - **Email**: `mail`
   - **Display Name**: `displayName`
6. 선택적으로 **Admin Group DN** 설정 — 이 그룹의 멤버는 자동으로 관리자로 승격
7. **Test LDAP Connection**을 클릭하여 설정 확인
8. 설정 저장

이제 사용자는 로그인 페이지에서 도메인 자격 증명으로 로그인할 수 있습니다. 첫 로그인 시 디렉토리에서 동기화된 속성을 가진 로컬 계정이 자동 생성됩니다. LDAP 인증은 Git HTTP 작업(클론/푸시)에도 사용됩니다.

### 12. 저장소 시크릿

CI/CD 워크플로우에서 사용할 암호화된 시크릿을 저장소에 추가:

1. 저장소의 **Settings** 탭으로 이동
2. **Secrets** 카드까지 스크롤하고 **Add secret** 클릭
3. 이름(예: `DEPLOY_TOKEN`)과 값을 입력 — 값은 AES-256으로 암호화됩니다
4. 시크릿은 모든 워크플로우 실행에 환경 변수로 자동 주입됩니다

워크플로우에서 시크릿 참조:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO 로그인

외부 아이덴티티 프로바이더로 로그인:

1. **Admin > OAuth / SSO**에서 사용할 프로바이더를 구성
2. 프로바이더의 개발자 콘솔에서 **Client ID**와 **Client Secret**을 입력
3. **Enable**을 체크 — 두 자격 증명이 모두 입력된 프로바이더만 로그인 페이지에 표시
4. 각 프로바이더의 콜백 URL은 관리 패널에 표시 (예: `https://yourserver/oauth/callback/github`)

지원되는 프로바이더: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

사용자는 **Settings > Linked Accounts**에서 여러 프로바이더를 계정에 연결할 수 있습니다.

### 14. 저장소 가져오기

전체 히스토리를 포함한 외부 소스에서 저장소 가져오기:

1. 홈페이지에서 **Import** 클릭
2. 소스 유형 선택 (Git URL, GitHub, GitLab 또는 Bitbucket)
3. 저장소 URL을 입력하고 선택적으로 프라이빗 저장소용 인증 토큰 입력
4. GitHub/GitLab/Bitbucket 가져오기의 경우 선택적으로 이슈와 풀 리퀘스트 가져오기
5. Import 페이지에서 실시간으로 가져오기 진행률 추적

### 15. 포크 및 업스트림 동기화

저장소를 포크하고 동기화 유지:

1. 저장소 페이지의 **Fork** 버튼 클릭
2. 원본에 대한 링크가 포함된 포크가 사용자 이름 아래에 생성
3. "forked from" 배지 옆의 **Sync fork**를 클릭하여 업스트림에서 최신 변경 사항 풀

### 16. CI/CD 자동 릴리스

MyPersonalGit에는 main에 대한 모든 푸시에서 자동 태그, 릴리스, Docker 이미지 푸시를 수행하는 내장 CI/CD 파이프라인이 포함되어 있습니다. 워크플로우는 푸시 시 자동 트리거되며 외부 CI 서비스가 필요 없습니다.

**작동 방식:**
1. `main`에 대한 푸시가 `.github/workflows/release.yml`을 자동 트리거
2. 패치 버전을 범프(`v1.15.1` -> `v1.15.2`)하고 git 태그를 생성
3. Docker Hub에 로그인하고 이미지를 빌드하여 `:latest`와 `:vX.Y.Z` 모두로 푸시

**설정:**
1. MyPersonalGit에서 저장소의 **Settings > Secrets**로 이동
2. Docker Hub 액세스 토큰을 `DOCKERHUB_TOKEN`이라는 이름의 시크릿으로 추가
3. MyPersonalGit 컨테이너에 Docker 소켓이 마운트되어 있는지 확인 (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. main에 푸시 — 워크플로우가 자동으로 트리거됩니다

**GitHub Actions 호환성:**
동일한 워크플로우 YAML은 GitHub Actions에서도 그대로 동작합니다 — 변경이 필요 없습니다. MyPersonalGit은 런타임에 `uses:` 액션을 동등한 셸 명령으로 변환합니다:

| GitHub Action | MyPersonalGit 변환 |
|---|---|
| `actions/checkout@v4` | 저장소가 이미 `/workspace`에 클론됨 |
| `actions/setup-dotnet@v4` | 공식 설치 스크립트를 통해 .NET SDK 설치 |
| `actions/setup-node@v4` | NodeSource를 통해 Node.js 설치 |
| `actions/setup-python@v5` | apt/apk를 통해 Python 설치 |
| `actions/setup-java@v4` | apt/apk를 통해 OpenJDK 설치 |
| `docker/login-action@v3` | stdin 비밀번호로 `docker login` |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op (기본 빌더 사용) |
| `softprops/action-gh-release@v2` | 데이터베이스에 실제 Release 엔티티 생성 |
| `${{ secrets.X }}` | `$X` 환경 변수 |
| `${{ steps.X.outputs.Y }}` | `$Y` 환경 변수 |
| `${{ github.sha }}` | `$GITHUB_SHA` 환경 변수 |

**병렬 작업:**
작업은 기본적으로 병렬로 실행됩니다. `needs:`로 종속성을 선언:
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
`needs:`가 없는 작업은 즉시 시작됩니다. 종속 대상 중 하나라도 실패하면 작업이 취소됩니다.

**조건부 스텝:**
`if:`로 스텝 실행 시점을 제어:
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
지원되는 표현식: `always()`, `success()` (기본값), `failure()`, `cancelled()`, `true`, `false`.

**스텝 출력:**
스텝은 `$GITHUB_OUTPUT`을 통해 후속 스텝에 값을 전달할 수 있습니다:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**매트릭스 빌드:**
`strategy.matrix`를 사용하여 여러 조합으로 작업을 확장:
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
이렇게 하면 4개의 작업이 생성됩니다: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)` 등. 모두 병렬로 실행됩니다.

**인풋을 포함한 수동 트리거 (`workflow_dispatch`):**
수동 트리거 시 UI에 폼으로 표시되는 타입 지정 인풋을 정의:
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
인풋 값은 `INPUT_<NAME>` 환경 변수(대문자)로 주입됩니다.

**작업 타임아웃:**
작업에 `timeout-minutes`를 설정하여 실행 시간이 너무 긴 경우 자동으로 실패 처리:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
기본 타임아웃은 360분(6시간)으로 GitHub Actions와 동일합니다.

**작업 수준 조건부 실행:**
작업에 `if:`를 사용하여 조건에 따라 건너뛰기:
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

**작업 출력:**
작업은 `outputs:`를 통해 하위 작업에 값을 전달할 수 있습니다:
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

**오류 시 계속:**
작업을 실패시키지 않고 스텝의 실패를 허용:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**경로 필터링:**
특정 파일이 변경된 경우에만 워크플로우를 트리거:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # 또는 paths-ignore 사용:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**작업 디렉토리:**
명령 실행 위치 설정:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # src/app에서 실행
      - run: npm test
        working-directory: tests  # 기본값 오버라이드
```

**워크플로우 재실행:**
완료, 실패 또는 취소된 워크플로우 실행의 **Re-run** 버튼을 클릭하여 동일한 작업, 스텝, 구성으로 새 실행을 생성합니다.

**풀 리퀘스트 워크플로우:**
`on: pull_request` 워크플로우는 비 드래프트 PR이 생성될 때 자동 트리거되어 소스 브랜치에 대한 검사를 실행합니다.

**커밋 상태 검사:**
워크플로우가 커밋 상태(pending/success/failure)를 자동으로 설정하여 PR에서 빌드 결과를 확인하고 브랜치 보호에서 필수 검사를 강제할 수 있습니다.

**워크플로우 취소:**
Actions UI에서 실행 중이거나 대기열에 있는 워크플로우의 **Cancel** 버튼을 클릭하여 즉시 중지합니다.

**상태 배지:**
README나 다른 곳에 빌드 상태 배지를 임베드:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
워크플로우 이름으로 필터: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom 피드

RSS 리더에서 표준 Atom 피드를 사용하여 저장소 활동을 구독:

```
# 저장소 커밋
http://localhost:8080/api/feeds/MyRepo/commits.atom

# 저장소 릴리스
http://localhost:8080/api/feeds/MyRepo/releases.atom

# 저장소 태그
http://localhost:8080/api/feeds/MyRepo/tags.atom

# 사용자 활동
http://localhost:8080/api/feeds/users/admin/activity.atom

# 전역 활동 (모든 저장소)
http://localhost:8080/api/feeds/global/activity.atom
```

공개 저장소에는 인증이 필요 없습니다. 이 URL을 아무 피드 리더(Feedly, Miniflux, FreshRSS 등)에 추가하여 변경 알림을 받으세요.

## 데이터베이스 설정

MyPersonalGit은 기본적으로 **SQLite**를 사용합니다 — 설정 불필요, 단일 파일 데이터베이스로 개인 사용 및 소규모 팀에 최적입니다.

대규모 배포(많은 동시 사용자, 고가용성, 또는 이미 PostgreSQL을 운영 중인 경우)에는 **PostgreSQL**로 전환할 수 있습니다:

### PostgreSQL 사용

**Docker Compose** (PostgreSQL에 권장):
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

**환경 변수만** (이미 PostgreSQL 서버가 있는 경우):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

EF Core 마이그레이션은 두 프로바이더 모두 시작 시 자동으로 실행됩니다. 수동 스키마 설정이 필요 없습니다.

### 관리 대시보드에서 전환

웹 UI에서 직접 데이터베이스 프로바이더를 전환할 수도 있습니다:

1. **Admin > Settings**로 이동 — **Database** 카드가 상단에 있습니다
2. 프로바이더 드롭다운에서 **PostgreSQL** 선택
3. PostgreSQL 연결 문자열 입력 (예: `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. **Save Database Settings** 클릭
5. 변경을 적용하려면 애플리케이션 재시작

설정은 `~/.mypersonalgit/database.json`(데이터베이스 자체 외부)에 저장되므로 연결 전에 읽을 수 있습니다.

### 데이터베이스 선택

| | SQLite | PostgreSQL |
|---|---|---|
| **설정** | 설정 불필요 (기본값) | PostgreSQL 서버 필요 |
| **최적 용도** | 개인 사용, 소규모 팀, NAS | 50명 이상의 팀, 높은 동시성 |
| **백업** | `.db` 파일 복사 | 표준 `pg_dump` |
| **동시성** | 단일 라이터 (대부분의 사용에 충분) | 완전한 멀티 라이터 |
| **마이그레이션** | N/A | 프로바이더를 전환하고 앱을 실행 (자동 마이그레이션) |

## NAS에 배포

MyPersonalGit은 Docker를 통해 NAS(QNAP, Synology 등)에서 훌륭하게 동작합니다:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Docker 소켓 마운트는 선택 사항입니다 — CI/CD 워크플로우 실행이 필요한 경우에만 필요합니다. 포트 2222는 내장 SSH 서버를 활성화한 경우에만 필요합니다.

## 설정

모든 설정은 `appsettings.json`, 환경 변수, 또는 `/admin`의 관리 대시보드에서 구성할 수 있습니다:

- 데이터베이스 프로바이더 (SQLite 또는 PostgreSQL)
- 프로젝트 루트 디렉토리
- 인증 요구 사항
- 사용자 등록 설정
- 기능 토글 (Issues, Wiki, Projects, Actions)
- 사용자별 최대 저장소 크기 및 수
- 이메일 알림을 위한 SMTP 설정
- 푸시 알림 설정 (Ntfy/Gotify)
- 내장 SSH 서버 (활성화/비활성화, 포트)
- LDAP/Active Directory 인증 (서버, Bind DN, 검색 베이스, 사용자 필터, 속성 매핑, 관리자 그룹)
- OAuth/SSO 프로바이더 설정 (프로바이더별 Client ID/Secret)

## 프로젝트 구조

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor 페이지 (Home, RepoDetails, Issues, PRs, Packages 등)
  Controllers/       # REST API 엔드포인트 (NuGet, npm, Generic, Registry 등)
  Data/              # EF Core DbContext, 서비스 구현
  Models/            # 도메인 모델
  Migrations/        # EF Core 마이그레이션
  Services/          # 미들웨어 (인증, Git HTTP 백엔드, Pages, Registry 인증)
    SshServer/       # 내장 SSH 서버 (SSH2 프로토콜, ECDH, AES-CTR)
  Program.cs         # 앱 시작, DI, 미들웨어 파이프라인
MyPersonalGit.Tests/
  UnitTest1.cs       # InMemory 데이터베이스를 사용한 xUnit 테스트
```

## 테스트 실행

```bash
dotnet test
```

## 라이선스

MIT
