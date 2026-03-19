🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

ASP.NET Core ve Blazor Server ile oluşturulmuş, GitHub benzeri web arayüzüne sahip kendi sunucunuzda barındırılan bir Git sunucusu. Depoları görüntüleyin, sorunları, pull request'leri, wikileri, projeleri ve daha fazlasını yönetin — hepsi kendi makinenizde veya sunucunuzda.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)


---

## İçindekiler

- [Özellikler](#özellikler)
- [Teknoloji Yığını](#teknoloji-yığını)
- [Hızlı Başlangıç](#hızlı-başlangıç)
  - [Docker (Önerilen)](#docker-önerilen)
  - [Yerel Çalıştırma](#yerel-çalıştırma)
  - [Ortam Değişkenleri](#ortam-değişkenleri)
- [Kullanım](#kullanım)
  - [Giriş Yapma](#1-giriş-yapma)
  - [Depo Oluşturma](#2-depo-oluşturma)
  - [Klonlama ve Push](#3-klonlama-ve-push)
  - [IDE'den Klonlama](#4-ideden-klonlama)
  - [Web Düzenleyici](#5-web-düzenleyiciyi-kullanma)
  - [Container Registry](#6-container-registry)
  - [Paket Registry](#7-paket-registry)
  - [Pages (Statik Siteler)](#8-pages-statik-site-barındırma)
  - [Push Bildirimleri](#9-push-bildirimleri)
  - [SSH Anahtar Doğrulama](#10-ssh-anahtar-doğrulama)
  - [LDAP / Active Directory](#11-ldap--active-directory-doğrulama)
  - [Depo Gizli Anahtarları](#12-depo-gizli-anahtarları)
  - [OAuth / SSO Girişi](#13-oauth--sso-girişi)
  - [Depo İçe Aktarma](#14-depo-içe-aktarma)
  - [Fork ve Upstream Senkronizasyonu](#15-fork-ve-upstream-senkronizasyonu)
  - [CI/CD Otomatik Sürüm](#16-cicd-otomatik-sürüm)
  - [RSS/Atom Beslemeleri](#17-rssatom-beslemeleri)
- [Veritabanı Yapılandırması](#veritabanı-yapılandırması)
  - [PostgreSQL Kullanımı](#postgresql-kullanımı)
  - [Yönetici Panelinden Geçiş](#yönetici-panelinden-geçiş)
  - [Veritabanı Seçimi](#veritabanı-seçimi)
- [NAS'a Dağıtım](#nasa-dağıtım)
- [Yapılandırma](#yapılandırma)
- [Proje Yapısı](#proje-yapısı)
- [Testleri Çalıştırma](#testleri-çalıştırma)
- [Lisans](#lisans)

---

## Özellikler

### Kod ve Depolar
- **Depo Yönetimi** — Tam kod tarayıcısı, dosya düzenleyici, commit geçmişi, dallar ve etiketler ile Git depolarını oluşturun, görüntüleyin ve silin
- **Depo İçe Aktarma/Taşıma** — GitHub, GitLab, Bitbucket veya herhangi bir Git URL'den isteğe bağlı sorun ve PR içe aktarma ile depoları içe aktarın. İlerleme takibi ile arka plan işleme
- **Depo Arşivleme** — Depoları görsel rozetlerle salt okunur olarak işaretleyin; arşivlenmiş depolar için push işlemleri engellenir
- **Git Smart HTTP** — Basic Auth ile HTTP üzerinden klonlama, fetch ve push
- **Yerleşik SSH Sunucusu** — Git işlemleri için yerel SSH sunucusu — harici OpenSSH gerekmez. ECDH anahtar değişimi, AES-CTR şifreleme ve açık anahtar doğrulama (RSA, ECDSA, Ed25519) destekler
- **SSH Anahtar Doğrulama** — Hesabınıza SSH açık anahtarları ekleyin ve otomatik yönetilen `authorized_keys` ile (veya yerleşik SSH sunucusu ile) SSH üzerinden Git işlemlerini doğrulayın
- **Fork ve Upstream Senkronizasyonu** — Depoları forklayın, tek tıkla upstream ile senkronize edin ve arayüzde fork ilişkilerini görün
- **Git LFS** — İkili dosyaları izlemek için Large File Storage desteği
- **Depo Yansılama** — Harici Git uzak sunucularına/sunucularından depo yansılama
- **Karşılaştırma Görünümü** — İleri/geri commit sayıları ve tam diff oluşturma ile dalları karşılaştırın
- **Dil İstatistikleri** — Her depo sayfasında GitHub tarzı dil dağılımı çubuğu
- **Dal Koruma** — Zorunlu incelemeler, durum kontrolleri, force-push önleme ve CODEOWNERS onay zorunluluğu için yapılandırılabilir kurallar
- **Etiket Koruma** — Glob desen eşleştirme ve kullanıcı bazlı izin listeleri ile etiketleri silme, zorla güncelleme ve yetkisiz oluşturmaya karşı koruyun
- **Commit İmza Doğrulama** — Commit'ler ve açıklamalı etiketlerde GPG imza doğrulaması ile arayüzde "Verified" / "Signed" rozetleri
- **Depo Etiketleri** — Depo başına özel renklerle etiketleri yönetin; şablonlardan depo oluştururken etiketler otomatik olarak kopyalanır
- **AGit Flow** — Push-to-review iş akışı: `git push origin HEAD:refs/for/main` fork oluşturmadan veya uzak dallar oluşturmadan bir pull request oluşturur. Sonraki push'larda mevcut açık PR'ler güncellenir
- **Keşfet** — Arama, sıralama ve konu filtreleme ile erişilebilir tüm depoları görüntüleyin
- **Arama** — Depolar, sorunlar, PR'ler ve kod genelinde tam metin arama

### İş Birliği
- **Sorunlar ve Pull Request'ler** — Etiketler, çoklu atananlar, son tarihler ve incelemeler ile sorunları ve PR'leri oluşturun, yorum yapın, kapatın/yeniden açın. PR'leri merge commit, squash veya rebase stratejileri ile birleştirin. Yan yana diff görünümü ile web tabanlı birleştirme çakışması çözümü
- **Sorun Bağımlılıkları** — Döngüsel bağımlılık algılama ile sorunlar arasında "tarafından engellenen" ve "engelleyen" ilişkileri tanımlayın
- **Sorun Sabitleme ve Kilitleme** — Önemli sorunları listenin en üstüne sabitleyin ve daha fazla yorumu önlemek için konuşmaları kilitleyin
- **Yorum Düzenleme ve Silme** — Sorunlar ve pull request'lerdeki kendi yorumlarınızı "(düzenlendi)" göstergesi ile düzenleyin veya silin
- **Birleştirme Çakışması Çözümü** — Base/ours/theirs görünümleri, hızlı kabul düğmeleri ve çakışma işaretçisi doğrulama ile görsel düzenleyicide birleştirme çakışmalarını doğrudan tarayıcıda çözün
- **Tartışmalar** — Kategoriler (Genel, Soru-Cevap, Duyurular, Fikirler, Göster ve Anlat, Anketler), sabitleme/kilitleme, yanıt olarak işaretleme ve oylama ile depo başına GitHub Discussions tarzı dizili konuşmalar
- **Kod İnceleme Önerileri** — PR satır içi incelemelerinde "Değişiklik öner" modu, incelemecilerin diff'te doğrudan kod değişiklikleri önermesine olanak tanır
- **Emoji Tepkileri** — Sorunlara, PR'lere, tartışmalara ve yorumlara beğeni/beğenmeme, kalp, gülme, kutlama, şaşkınlık, roket ve göz tepkileri
- **CODEOWNERS** — Birleştirme öncesi CODEOWNERS onayı gerektiren isteğe bağlı zorunluluk ile dosya yollarına göre PR incelemecilerini otomatik atama
- **Depo Şablonları** — Dosyaların, etiketlerin, sorun şablonlarının ve dal koruma kurallarının otomatik kopyalanması ile şablonlardan yeni depolar oluşturun
- **Taslak Sorunlar ve Sorun Şablonları** — Taslak sorunlar (devam eden çalışma) oluşturun ve depo başına varsayılan etiketlerle yeniden kullanılabilir sorun şablonları (hata raporu, özellik isteği) tanımlayın
- **Wiki** — Revizyon geçmişi ile depo başına Markdown tabanlı wiki sayfaları
- **Projeler** — İşleri düzenlemek için sürükle-bırak kartları ile Kanban panoları
- **Kod Parçacıkları** — Söz dizimi vurgulama ve çoklu dosya ile kod parçacıkları paylaşın (GitHub Gists benzeri)
- **Organizasyonlar ve Takımlar** — Üyeler ve takımlarla organizasyonlar oluşturun, depolara takım izinleri atayın
- **Ayrıntılı İzinler** — Depolarda hassas erişim kontrolü için beş seviyeli izin modeli (Read, Triage, Write, Maintain, Admin)
- **Kilometre Taşları** — İlerleme çubukları ve son tarihlerle sorun ilerlemesini kilometre taşlarına göre takip edin
- **Commit Yorumları** — İsteğe bağlı dosya/satır referansları ile bireysel commit'lere yorum yapın
- **Depo Konuları** — Keşfet sayfasında keşif ve filtreleme için depoları konularla etiketleyin

### CI/CD ve DevOps
- **CI/CD Runner** — `.github/workflows/*.yml` dosyalarında iş akışları tanımlayın ve Docker container'larında çalıştırın. Push ve pull request olaylarında otomatik tetikleme
- **GitHub Actions Uyumluluğu** — Aynı iş akışı YAML'ı hem MyPersonalGit'te hem de GitHub Actions'da çalışır. `uses:` eylemlerini (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) eşdeğer shell komutlarına çevirir
- **`needs:` ile Paralel İşler** — İşler `needs:` ile bağımlılıkları bildirir ve bağımsız olduklarında paralel çalışır. Bağımlı işler ön koşullarını bekler ve bir bağımlılık başarısız olduğunda otomatik olarak iptal edilir
- **Koşullu Adımlar (`if:`)** — Adımlar `if:` ifadelerini destekler: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. `if: failure()` veya `if: always()` ile temizlik adımları önceki hatalardan sonra da çalışır
- **Adım Çıktıları (`$GITHUB_OUTPUT`)** — Adımlar `$GITHUB_OUTPUT`'a `key=value` veya çok satırlı `key<<DELIMITER` çiftleri yazabilir ve sonraki adımlar bunları ortam değişkenleri olarak alır, `${{ steps.X.outputs.Y }}` söz dizimi ile uyumlu
- **`github` Bağlamı** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` ve `CI=true` her işe otomatik olarak enjekte edilir
- **Matris Derlemeler** — `strategy.matrix` işleri birden fazla değişken kombinasyonuna (örn. İS x sürüm) genişletir. `fail-fast` ve `runs-on`, adım komutları ve adım adlarında `${{ matrix.X }}` değişikliğini destekler
- **`workflow_dispatch` Girdileri** — Tipli girdi parametreleri (string, boolean, choice, number) ile manuel tetikleyiciler. Girdi içeren iş akışlarını manuel tetiklerken UI bir girdi formu gösterir. Değerler `INPUT_*` ortam değişkenleri olarak enjekte edilir
- **İş Zaman Aşımları (`timeout-minutes`)** — Limiti aşan işleri otomatik olarak başarısız kılmak için işlere `timeout-minutes` ayarlayın. Varsayılan: 360 dakika (GitHub Actions ile aynı)
- **İş Seviyesi `if:`** — Koşullara göre tüm işleri atlayın. `if: always()` olan işler bağımlılıklar başarısız olsa bile çalışır. Atlanan işler çalıştırmayı başarısız kılmaz
- **İş Çıktıları** — İşler, aşağı akış `needs:` işlerinin `${{ needs.X.outputs.Y }}` ile tükettiği `outputs:` bildirir. Çıktılar, iş tamamlandıktan sonra adım çıktılarından çözümlenir
- **`continue-on-error`** — İşi başarısız kılmadan bireysel adımların başarısız olmasına izin verin. İsteğe bağlı doğrulama veya bildirim adımları için kullanışlıdır
- **`on.push.paths` Filtresi** — Yalnızca belirli dosyalar değiştiğinde iş akışlarını tetikleyin. Glob desenleri (`src/**`, `*.ts`) ve istisnalar için `paths-ignore:` destekler
- **İş Akışlarını Yeniden Çalıştırma** — Başarısız, başarılı veya iptal edilmiş iş akışı çalıştırmalarını Actions UI'dan tek tıkla yeniden çalıştırın. Aynı yapılandırmayla yeni bir çalıştırma oluşturur
- **`working-directory`** — Komutların nerede çalışacağını kontrol etmek için iş akışı düzeyinde `defaults.run.working-directory` veya adım başına `working-directory:` ayarlayın
- **`defaults.run.shell`** — İş akışı veya adım başına özel kabuk yapılandırması (`bash`, `sh`, `python3` vb.)
- **`strategy.max-parallel`** — Eşzamanlı matris iş yürütmesini sınırlama
- **`on.workflow_run`** — İş akışı zincirleme: İş akışı A tamamlandığında iş akışı B'yi tetikleyin. İş akışı adı ve `types: [completed]` ile filtreleme
- **Otomatik Sürüm Oluşturma** — `softprops/action-gh-release` etiket, başlık, changelog gövdesi ve pre-release/draft bayrakları ile gerçek Release varlıkları oluşturur. Kaynak kod arşivleri (ZIP ve TAR.GZ) indirilebilir varlıklar olarak otomatik eklenir
- **Otomatik Sürüm Hattı** — Yerleşik iş akışı, main'e her push'ta otomatik olarak sürüm etiketler, changelog oluşturur ve Docker imajlarını Docker Hub'a gönderir
- **Commit Durum Kontrolleri** — İş akışları commit'lerde otomatik olarak pending/success/failure durumu ayarlar, pull request'lerde görünür
- **İş Akışı İptali** — Actions UI'dan çalışan veya sıradaki iş akışlarını iptal edin
- **Eşzamanlılık Kontrolleri** — Yeni push'lar aynı iş akışının sıradaki çalıştırmalarını otomatik olarak iptal eder
- **İş Akışı Ortam Değişkenleri** — YAML'da iş akışı, iş veya adım düzeyinde `env:` ayarlama
- **Durum Rozetleri** — İş akışı ve commit durumu için gömülebilir SVG rozetler (`/api/badge/{repo}/workflow`)
- **Artefakt İndirme** — Derleme artefaktlarını doğrudan Actions UI'dan indirin
- **Gizli Anahtar Yönetimi** — CI/CD iş akışı çalıştırmalarına ortam değişkenleri olarak enjekte edilen şifreli depo gizli anahtarları (AES-256)
- **Webhook'lar** — Depo olaylarında harici servisleri tetikleme
- **Prometheus Metrikleri** — İzleme için yerleşik `/metrics` uç noktası

### Paket ve Container Barındırma
- **Container Registry** — `docker push` ve `docker pull` ile Docker/OCI imajları barındırma (OCI Distribution Spec)
- **NuGet Registry** — Tam NuGet v3 API (servis indeksi, arama, push, restore) ile .NET paketleri barındırma
- **npm Registry** — Standart npm publish/install ile Node.js paketleri barındırma
- **PyPI Registry** — PEP 503 Simple API, JSON metadata API ve `twine upload` uyumluluğu ile Python paketleri barındırma
- **Maven Registry** — Standart Maven depo düzeni, `maven-metadata.xml` oluşturma ve `mvn deploy` desteği ile Java/JVM paketleri barındırma
- **Genel Paketler** — REST API ile rastgele ikili artefaktları yükleme ve indirme

### Statik Siteler
- **Pages** — Bir depo dalından doğrudan statik web siteleri sunma (GitHub Pages benzeri) `/pages/{owner}/{repo}/` adresinde

### RSS/Atom Beslemeleri
- **Depo Beslemeleri** — Depo başına commit'ler, sürümler ve etiketler için Atom beslemeleri (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Kullanıcı Aktivite Beslemesi** — Kullanıcı başına aktivite beslemesi (`/api/feeds/users/{username}/activity.atom`)
- **Genel Aktivite Beslemesi** — Site genelinde aktivite beslemesi (`/api/feeds/global/activity.atom`)

### Bildirimler
- **Uygulama İçi Bildirimler** — Bahsetmeler, yorumlar ve depo aktivitesi
- **Push Bildirimleri** — Kullanıcı bazlı katılım ile gerçek zamanlı mobil/masaüstü uyarıları için Ntfy ve Gotify entegrasyonu

### Kimlik Doğrulama
- **OAuth2 / SSO** — GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord veya Twitter/X ile giriş yapın. Yöneticiler, Yönetici panelinde sağlayıcı başına Client ID ve Secret yapılandırır — yalnızca kimlik bilgileri doldurulmuş sağlayıcılar kullanıcılara gösterilir
- **OAuth2 Sağlayıcısı** — Diğer uygulamaların "MyPersonalGit ile Giriş Yap" kullanabilmesi için kimlik sağlayıcısı olarak çalışın. PKCE ile Authorization Code akışı, token yenileme, userinfo uç noktası ve OpenID Connect keşfi (`.well-known/openid-configuration`) uygular
- **LDAP / Active Directory** — Kullanıcıları bir LDAP dizini veya Active Directory etki alanına karşı doğrulayın. Kullanıcılar ilk girişte senkronize edilmiş özniteliklerle (e-posta, görünen ad) otomatik olarak oluşturulur. Grup tabanlı yönetici terfi, SSL/TLS ve StartTLS destekler
- **SSPI / Windows Entegre Kimlik Doğrulama** — Negotiate/NTLM ile Windows etki alanı kullanıcıları için şeffaf Single Sign-On. Etki alanındaki kullanıcılar kimlik bilgisi girmeden otomatik olarak doğrulanır. Admin > Settings'den etkinleştirin (yalnızca Windows)
- **İki Faktörlü Kimlik Doğrulama** — Kimlik doğrulama uygulaması desteği ve kurtarma kodları ile TOTP tabanlı 2FA
- **WebAuthn / Passkeys** — İkinci faktör olarak FIDO2 donanım güvenlik anahtarı ve passkey desteği. YubiKey'leri, platform doğrulayıcılarını (Face ID, Windows Hello, Touch ID) ve diğer FIDO2 cihazlarını kaydedin. Klonlanmış anahtar algılama için imza sayacı doğrulama
- **Bağlı Hesaplar** — Kullanıcılar Ayarlar'dan hesaplarına birden fazla OAuth sağlayıcısı bağlayabilir

### Yönetim
- **Yönetici Paneli** — Sistem ayarları (veritabanı sağlayıcısı, SSH sunucusu, LDAP/AD, alt bilgi sayfaları dahil), kullanıcı yönetimi, denetim günlükleri ve istatistikler
- **Özelleştirilebilir Alt Bilgi Sayfaları** — Admin > Settings'den düzenlenebilir Markdown içerikli Kullanım Koşulları, Gizlilik Politikası, Dokümantasyon ve İletişim sayfaları
- **Kullanıcı Profilleri** — Kullanıcı başına katkı ısı haritası, aktivite beslemesi ve istatistikler
- **Kişisel Erişim Token'ları** — Yapılandırılabilir kapsamlar ve isteğe bağlı rota düzeyinde kısıtlamalar (belirli API yollarına token erişimini sınırlamak için `/api/packages/**` gibi glob desenleri) ile token tabanlı API kimlik doğrulama
- **Yedekleme ve Geri Yükleme** — Sunucu verilerini dışa ve içe aktarma
- **Güvenlik Taraması** — [OSV.dev](https://osv.dev/) veritabanı tarafından desteklenen gerçek bağımlılık güvenlik açığı taraması. `.csproj` (NuGet), `package.json` (npm) ve `requirements.txt` (PyPI) dosyalarından bağımlılıkları otomatik olarak çıkarır, ardından bilinen CVE'lere karşı kontrol eder. Önem derecesi, düzeltilmiş sürümler ve danışma bağlantıları rapor eder. Ayrıca taslak/yayınlama/kapatma iş akışı ile manuel güvenlik danışmaları
- **Karanlık Mod** — Başlıkta geçiş düğmesi ile tam karanlık/açık mod desteği
- **Çoklu Dil / i18n** — 676 kaynak anahtarı ile 27 sayfanın tamamında yerelleştirme. 11 dil ile birlikte gelir: İngilizce, İspanyolca, Fransızca, Almanca, Japonca, Korece, Çince (Basitleştirilmiş), Portekizce, Rusça, İtalyanca ve Türkçe. `SharedResource.{locale}.resx` dosyaları oluşturarak daha fazlasını ekleyin

## Teknoloji Yığını

| Bileşen | Teknoloji |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (etkileşimli sunucu tarafı oluşturma) |
| Veritabanı | SQLite (varsayılan) veya Entity Framework Core 10 ile PostgreSQL |
| Git Motoru | LibGit2Sharp |
| Kimlik Doğrulama | BCrypt parola karma, oturum tabanlı doğrulama, PAT token'lar, OAuth2 (8 sağlayıcı + sağlayıcı modu), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| SSH Sunucusu | Yerleşik SSH2 protokol uygulaması (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| İzleme | Prometheus metrikleri |

## Hızlı Başlangıç

### Ön Koşullar

- [Docker](https://docs.docker.com/get-docker/) (önerilen)
- Veya yerel geliştirme için [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git

### Docker (Önerilen)

Docker Hub'dan çekip çalıştırın:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Port 2222 isteğe bağlıdır — yalnızca yerleşik SSH sunucusunu Admin > Settings'de etkinleştirirseniz gereklidir.

Veya Docker Compose kullanın:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

Uygulama **http://localhost:8080** adresinde kullanılabilir olacaktır.

> **Varsayılan kimlik bilgileri**: `admin` / `admin`
>
> İlk girişten sonra **varsayılan parolayı hemen değiştirin**, Yönetici panelinden.

### Yerel Çalıştırma

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

Uygulama **http://localhost:5146** adresinde başlar.

### Ortam Değişkenleri

| Değişken | Açıklama | Varsayılan |
|----------|-------------|---------|
| `Database__Provider` | Veritabanı motoru: `sqlite` veya `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Veritabanı bağlantı dizesi | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Git depolarının saklandığı dizin | `/repos` |
| `Git__RequireAuth` | Git HTTP işlemleri için kimlik doğrulama gerektir | `true` |
| `Git__Users__<username>` | Git HTTP Basic Auth kullanıcısı için parola ayarla | — |
| `RESET_ADMIN_PASSWORD` | Başlangıçta acil yönetici parolası sıfırlama | — |
| `Secrets__EncryptionKey` | Depo gizli anahtarları için özel şifreleme anahtarı | Veritabanı bağlantı dizesinden türetilir |
| `Ssh__DataDir` | SSH verileri dizini (host anahtarları, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Oluşturulan authorized_keys dosyasının yolu | `<DataDir>/authorized_keys` |

> **Not:** Yerleşik SSH sunucu portu ve LDAP ayarları, ortam değişkenleri ile değil Yönetici paneli (Admin > Settings) üzerinden yapılandırılır. Bu, yeniden dağıtım yapmadan değiştirmenize olanak tanır.

## Kullanım

### 1. Giriş Yapma

Uygulamayı açın ve **Sign In**'e tıklayın. Yeni bir kurulumda varsayılan kimlik bilgilerini (`admin` / `admin`) kullanın. **Admin** paneli üzerinden veya Admin > Settings'de kullanıcı kaydını etkinleştirerek ek kullanıcılar oluşturun.

### 2. Depo Oluşturma

Ana sayfadaki yeşil **New** düğmesine tıklayın, bir ad girin ve **Create**'e tıklayın. Bu, sunucuda klonlayabileceğiniz, push yapabileceğiniz ve web arayüzü üzerinden yönetebileceğiniz çıplak bir Git deposu oluşturur.

### 3. Klonlama ve Push

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

Git HTTP kimlik doğrulaması etkinse, `Git__Users__<username>` ortam değişkenleri ile yapılandırılmış kimlik bilgileri istenecektir. Bunlar web arayüzü girişinden farklıdır.

### 4. IDE'den Klonlama

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > `http://localhost:8080/git/MyRepo.git` yapıştırın

**Visual Studio**: **Git > Clone Repository** > URL'yi yapıştırın

**JetBrains**: **File > New > Project from Version Control** > URL'yi yapıştırın

### 5. Web Düzenleyiciyi Kullanma

Dosyaları doğrudan tarayıcıda düzenleyebilirsiniz:
- Bir depoya gidin ve herhangi bir dosyaya tıklayın, ardından **Edit**'e tıklayın
- Yerel klonlama olmadan dosya eklemek için **Add files > Create new file** kullanın
- Makinenizden yüklemek için **Add files > Upload files/folder** kullanın

### 6. Container Registry

Docker/OCI imajlarını doğrudan sunucunuza push ve pull yapın:

```bash
# Giriş yapın (Settings > Access Tokens'dan bir Kişisel Erişim Token'ı kullanın)
docker login localhost:8080 -u youruser

# İmaj push edin
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# İmaj pull edin
docker pull localhost:8080/myapp:v1
```

> **Not:** Docker varsayılan olarak HTTPS gerektirir. HTTP için sunucunuzu Docker'ın `~/.docker/daemon.json` dosyasındaki `insecure-registries` listesine ekleyin:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Paket Registry

**NuGet (.NET paketleri):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (Node.js paketleri):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**PyPI (Python paketleri):**
```bash
# Paket yükleme
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# twine ile yükleme
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

**Maven (Java/JVM paketleri):**
```xml
<!-- pom.xml dosyanızda depoyu ekleyin -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- settings.xml dosyasında kimlik bilgilerini ekleyin -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Genel (herhangi bir ikili):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Tüm paketleri web arayüzünde `/packages` adresinden görüntüleyin.

### 8. Pages (Statik Site Barındırma)

Bir depo dalından statik web siteleri sunun:

1. Deponuzun **Settings** sekmesine gidin ve **Pages**'i etkinleştirin
2. Dalı ayarlayın (varsayılan: `gh-pages`)
3. HTML/CSS/JS dosyalarını bu dala push edin
4. `http://localhost:8080/pages/{username}/{repo}/` adresini ziyaret edin

### 9. Push Bildirimleri

Sorunlar, PR'ler veya yorumlar oluşturulduğunda telefonunuzda veya masaüstünüzde push bildirimleri almak için **Admin > System Settings**'de Ntfy veya Gotify'ı yapılandırın. Kullanıcılar **Settings > Notifications**'da katılabilir/ayrılabilir.

### 10. SSH Anahtar Doğrulama

Parolasız Git işlemleri için SSH anahtarları kullanın. İki seçenek vardır:

#### Seçenek A: Yerleşik SSH Sunucusu (Önerilen)

Harici SSH daemon'u gerekmez — MyPersonalGit kendi SSH sunucusunu çalıştırır:

1. **Admin > Settings**'e gidin ve **Built-in SSH Server**'ı etkinleştirin
2. SSH portunu ayarlayın (varsayılan: 2222) — sistem SSH çalışmıyorsa 22 kullanın
3. Ayarları kaydedin ve sunucuyu yeniden başlatın (port değişiklikleri yeniden başlatma gerektirir)
4. **Settings > SSH Keys**'e gidin ve açık anahtarınızı ekleyin (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` veya `~/.ssh/id_ecdsa.pub`)
5. SSH ile klonlayın:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Yerleşik SSH sunucusu ECDH-SHA2-NISTP256 anahtar değişimi, AES-128/256-CTR şifreleme, HMAC-SHA2-256 ve Ed25519, RSA ve ECDSA anahtarları ile açık anahtar doğrulamayı destekler.

#### Seçenek B: Sistem OpenSSH

Sisteminizin SSH daemon'unu kullanmayı tercih ediyorsanız:

1. **Settings > SSH Keys**'e gidin ve açık anahtarınızı ekleyin
2. MyPersonalGit, tüm kayıtlı SSH anahtarlarından otomatik olarak bir `authorized_keys` dosyası oluşturur
3. Sunucunuzun OpenSSH'sini oluşturulan authorized_keys dosyasını kullanacak şekilde yapılandırın:
   ```
   # /etc/ssh/sshd_config dosyasında
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. SSH ile klonlayın:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

SSH doğrulama servisi ayrıca OpenSSH'nin `AuthorizedKeysCommand` direktifi ile kullanılmak üzere `/api/ssh/authorized-keys` adresinde bir API sunar.

### 11. LDAP / Active Directory Doğrulama

Kullanıcıları kuruluşunuzun LDAP dizini veya Active Directory etki alanına karşı doğrulayın:

1. **Admin > Settings**'e gidin ve **LDAP / Active Directory Authentication** bölümüne kaydırın
2. LDAP'ı etkinleştirin ve sunucu bilgilerinizi doldurun:
   - **Server**: LDAP sunucu ana bilgisayar adınız (örn. `dc01.corp.local`)
   - **Port**: LDAP için 389, LDAPS için 636
   - **SSL/TLS**: LDAPS için etkinleştirin veya düz bağlantıyı yükseltmek için StartTLS kullanın
3. Kullanıcı aramak için bir servis hesabı yapılandırın:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password**: Servis hesabı parolası
4. Arama parametrelerini ayarlayın:
   - **Search Base DN**: `OU=Users,DC=corp,DC=local`
   - **User Filter**: AD için `(sAMAccountName={0})`, OpenLDAP için `(uid={0})`
5. LDAP özniteliklerini kullanıcı alanlarına eşleyin:
   - **Username**: `sAMAccountName` (AD) veya `uid` (OpenLDAP)
   - **Email**: `mail`
   - **Display Name**: `displayName`
6. İsteğe bağlı olarak bir **Admin Group DN** ayarlayın — bu grubun üyeleri otomatik olarak yönetici yapılır
7. Ayarları doğrulamak için **Test LDAP Connection**'a tıklayın
8. Ayarları kaydedin

Kullanıcılar artık giriş sayfasında etki alanı kimlik bilgileriyle giriş yapabilir. İlk girişte, dizinden senkronize edilmiş özniteliklerle otomatik olarak yerel bir hesap oluşturulur. LDAP doğrulaması ayrıca Git HTTP işlemleri (clone/push) için de kullanılır.

### 12. Depo Gizli Anahtarları

CI/CD iş akışlarında kullanılmak üzere depolara şifreli gizli anahtarlar ekleyin:

1. Deponuzun **Settings** sekmesine gidin
2. **Secrets** kartına kaydırın ve **Add secret**'a tıklayın
3. Bir ad (örn. `DEPLOY_TOKEN`) ve değer girin — değer AES-256 ile şifrelenir
4. Gizli anahtarlar her iş akışı çalıştırmasına ortam değişkenleri olarak otomatik enjekte edilir

İş akışınızda gizli anahtarlara başvurma:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO Girişi

Harici kimlik sağlayıcıları ile giriş yapın:

1. **Admin > OAuth / SSO**'ya gidin ve etkinleştirmek istediğiniz sağlayıcıları yapılandırın
2. Sağlayıcının geliştirici konsolundan **Client ID** ve **Client Secret** girin
3. **Enable**'ı işaretleyin — yalnızca her iki kimlik bilgisi doldurulmuş sağlayıcılar giriş sayfasında görünecektir
4. Her sağlayıcı için geri çağırma URL'si yönetici panelinde gösterilir (örn. `https://yourserver/oauth/callback/github`)

Desteklenen sağlayıcılar: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Kullanıcılar **Settings > Linked Accounts**'ta hesaplarına birden fazla sağlayıcı bağlayabilir.

### 14. Depo İçe Aktarma

Tam geçmişle harici kaynaklardan depoları içe aktarın:

1. Ana sayfada **Import**'a tıklayın
2. Bir kaynak türü seçin (Git URL, GitHub, GitLab veya Bitbucket)
3. Depo URL'sini ve özel depolar için isteğe bağlı olarak bir kimlik doğrulama token'ı girin
4. GitHub/GitLab/Bitbucket içe aktarmaları için isteğe bağlı olarak sorunları ve pull request'leri içe aktarın
5. İçe aktarma ilerlemesini Import sayfasında gerçek zamanlı olarak takip edin

### 15. Fork ve Upstream Senkronizasyonu

Bir depoyu forklayın ve güncel tutun:

1. Herhangi bir depo sayfasında **Fork** düğmesine tıklayın
2. Kullanıcı adınız altında orijinale bağlantılı bir fork oluşturulur
3. Upstream'den en son değişiklikleri almak için "forked from" rozetinin yanındaki **Sync fork**'a tıklayın

### 16. CI/CD Otomatik Sürüm

MyPersonalGit, main'e her push'ta otomatik etiketleme, sürüm oluşturma ve Docker imajı gönderme yapan yerleşik bir CI/CD hattı içerir. İş akışları push'ta otomatik tetiklenir — harici CI servisi gerekmez.

**Nasıl çalışır:**
1. `main`'e push, `.github/workflows/release.yml` dosyasını otomatik tetikler
2. Yama sürümünü artırır (`v1.15.1` -> `v1.15.2`), bir git etiketi oluşturur
3. Docker Hub'a giriş yapar, imajı derler ve hem `:latest` hem de `:vX.Y.Z` olarak push eder

**Kurulum:**
1. MyPersonalGit'te deponuzun **Settings > Secrets** bölümüne gidin
2. `DOCKERHUB_TOKEN` adında Docker Hub erişim token'ınız ile bir gizli anahtar ekleyin
3. MyPersonalGit container'ının Docker soketinin monte edildiğinden emin olun (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Main'e push yapın — iş akışı otomatik olarak tetiklenir

**GitHub Actions uyumluluğu:**
Aynı iş akışı YAML'ı GitHub Actions'da da çalışır — değişiklik gerekmez. MyPersonalGit, `uses:` eylemlerini çalışma zamanında eşdeğer shell komutlarına çevirir:

| GitHub Action | MyPersonalGit Çevirisi |
|---|---|
| `actions/checkout@v4` | Depo zaten `/workspace`'e klonlanmış |
| `actions/setup-dotnet@v4` | Resmi kurulum betiği ile .NET SDK yükler |
| `actions/setup-node@v4` | NodeSource ile Node.js yükler |
| `actions/setup-python@v5` | apt/apk ile Python yükler |
| `actions/setup-java@v4` | apt/apk ile OpenJDK yükler |
| `docker/login-action@v3` | stdin parola ile `docker login` |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | İşlem yok (varsayılan builder kullanılır) |
| `softprops/action-gh-release@v2` | Veritabanında gerçek bir Release varlığı oluşturur |
| `${{ secrets.X }}` | `$X` ortam değişkeni |
| `${{ steps.X.outputs.Y }}` | `$Y` ortam değişkeni |
| `${{ github.sha }}` | `$GITHUB_SHA` ortam değişkeni |

**Paralel işler:**
İşler varsayılan olarak paralel çalışır. Bağımlılıkları bildirmek için `needs:` kullanın:
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
`needs:` olmayan işler hemen başlar. Bağımlılıklarından herhangi biri başarısız olursa iş iptal edilir.

**Koşullu adımlar:**
Adımların ne zaman çalışacağını kontrol etmek için `if:` kullanın:
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
Desteklenen ifadeler: `always()`, `success()` (varsayılan), `failure()`, `cancelled()`, `true`, `false`.

**Adım çıktıları:**
Adımlar `$GITHUB_OUTPUT` ile sonraki adımlara değer iletebilir:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Matris derlemeler:**
`strategy.matrix` kullanarak işleri birden fazla kombinasyona genişletin:
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
Bu 4 iş oluşturur: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)` vb. Hepsi paralel çalışır.

**Girdili manuel tetikleyiciler (`workflow_dispatch`):**
Manuel tetiklerken UI'da form olarak gösterilen tipli girdiler tanımlayın:
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
Girdi değerleri `INPUT_<NAME>` ortam değişkenleri olarak enjekte edilir (büyük harf).

**İş zaman aşımları:**
Çok uzun süren işleri otomatik başarısız kılmak için `timeout-minutes` ayarlayın:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Varsayılan zaman aşımı 360 dakikadır (6 saat), GitHub Actions ile aynı.

**İş seviyesi koşullar:**
Koşullara göre işleri atlamak için işlerde `if:` kullanın:
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

**İş çıktıları:**
İşler `outputs:` ile aşağı akış işlerine değer iletebilir:
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

**Hata durumunda devam:**
Bir adımın işi başarısız kılmadan hata vermesine izin verin:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Yol filtreleme:**
Yalnızca belirli dosyalar değiştiğinde iş akışlarını tetikleyin:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # veya paths-ignore kullanın:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Çalışma dizini:**
Komutların nerede çalışacağını ayarlama:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # src/app içinde çalışır
      - run: npm test
        working-directory: tests  # varsayılanı geçersiz kılar
```

**İş akışlarını yeniden çalıştırma:**
Aynı işler, adımlar ve yapılandırma ile yeni bir çalıştırma oluşturmak için tamamlanmış, başarısız veya iptal edilmiş herhangi bir iş akışı çalıştırmasında **Re-run** düğmesine tıklayın.

**Pull request iş akışları:**
`on: pull_request` olan iş akışları, taslak olmayan bir PR oluşturulduğunda otomatik tetiklenir ve kaynak dal üzerinde kontroller çalıştırır.

**Commit durum kontrolleri:**
İş akışları otomatik olarak commit durumları (pending/success/failure) ayarlar, böylece PR'lerde derleme sonuçlarını görebilir ve dal koruma ile zorunlu kontroller uygulayabilirsiniz.

**İş akışı iptali:**
Actions UI'da çalışan veya sıradaki herhangi bir iş akışında **Cancel** düğmesine tıklayarak hemen durdurun.

**Durum rozetleri:**
README'nize veya herhangi bir yere derleme durum rozetleri gömün:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
İş akışı adına göre filtreleme: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom Beslemeleri

Herhangi bir RSS okuyucuda standart Atom beslemeleri ile depo aktivitesine abone olun:

```
# Depo commit'leri
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Depo sürümleri
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Depo etiketleri
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Kullanıcı aktivitesi
http://localhost:8080/api/feeds/users/admin/activity.atom

# Genel aktivite (tüm depolar)
http://localhost:8080/api/feeds/global/activity.atom
```

Genel depolar için kimlik doğrulama gerekmez. Değişikliklerden haberdar olmak için bu URL'leri herhangi bir besleme okuyucuya (Feedly, Miniflux, FreshRSS vb.) ekleyin.

## Veritabanı Yapılandırması

MyPersonalGit varsayılan olarak **SQLite** kullanır — sıfır yapılandırma, tek dosya veritabanı, kişisel kullanım ve küçük takımlar için mükemmel.

Daha büyük dağıtımlar (çok sayıda eşzamanlı kullanıcı, yüksek erişilebilirlik veya zaten PostgreSQL çalıştırıyorsanız) için **PostgreSQL**'e geçebilirsiniz:

### PostgreSQL Kullanımı

**Docker Compose** (PostgreSQL için önerilen):
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

**Yalnızca ortam değişkenleri** (zaten bir PostgreSQL sunucunuz varsa):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

EF Core migration'ları her iki sağlayıcı için de başlangıçta otomatik olarak çalışır. Manuel şema kurulumu gerekmez.

### Yönetici Panelinden Geçiş

Veritabanı sağlayıcılarını doğrudan web arayüzünden de değiştirebilirsiniz:

1. **Admin > Settings**'e gidin — **Database** kartı en üsttedir
2. Sağlayıcı açılır listesinden **PostgreSQL**'i seçin
3. PostgreSQL bağlantı dizenizi girin (örn. `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. **Save Database Settings**'e tıklayın
5. Değişikliğin etkili olması için uygulamayı yeniden başlatın

Yapılandırma `~/.mypersonalgit/database.json` dosyasına kaydedilir (veritabanının dışında, böylece bağlanmadan önce okunabilir).

### Veritabanı Seçimi

| | SQLite | PostgreSQL |
|---|---|---|
| **Kurulum** | Sıfır yapılandırma (varsayılan) | PostgreSQL sunucusu gerektirir |
| **En uygun** | Kişisel kullanım, küçük takımlar, NAS | 50+ kişilik takımlar, yüksek eşzamanlılık |
| **Yedekleme** | `.db` dosyasını kopyalama | Standart `pg_dump` |
| **Eşzamanlılık** | Tek yazıcı (çoğu kullanım için yeterli) | Tam çok yazıcılı |
| **Geçiş** | Yok | Sağlayıcı değiştir + uygulamayı çalıştır (otomatik geçiş) |

## NAS'a Dağıtım

MyPersonalGit, Docker ile NAS'ta (QNAP, Synology vb.) harika çalışır:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Docker soket montajı isteğe bağlıdır — yalnızca CI/CD iş akışı yürütme istiyorsanız gereklidir. Port 2222 yalnızca yerleşik SSH sunucusunu etkinleştirirseniz gerekir.

## Yapılandırma

Tüm ayarlar `appsettings.json` dosyasında, ortam değişkenleri ile veya `/admin` adresindeki Yönetici panelinden yapılandırılabilir:

- Veritabanı sağlayıcısı (SQLite veya PostgreSQL)
- Proje kök dizini
- Kimlik doğrulama gereksinimleri
- Kullanıcı kayıt ayarları
- Özellik anahtarları (Issues, Wiki, Projects, Actions)
- Kullanıcı başına maksimum depo boyutu ve sayısı
- E-posta bildirimleri için SMTP ayarları
- Push bildirim ayarları (Ntfy/Gotify)
- Yerleşik SSH sunucusu (etkinleştirme/devre dışı bırakma, port)
- LDAP/Active Directory kimlik doğrulama (sunucu, Bind DN, arama tabanı, kullanıcı filtresi, öznitelik eşleme, yönetici grubu)
- OAuth/SSO sağlayıcı yapılandırması (sağlayıcı başına Client ID/Secret)

## Proje Yapısı

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor sayfaları (Home, RepoDetails, Issues, PRs, Packages vb.)
  Controllers/       # REST API uç noktaları (NuGet, npm, Generic, Registry vb.)
  Data/              # EF Core DbContext, servis uygulamaları
  Models/            # Alan modelleri
  Migrations/        # EF Core migration'ları
  Services/          # Middleware (kimlik doğrulama, Git HTTP backend, Pages, Registry auth)
    SshServer/       # Yerleşik SSH sunucusu (SSH2 protokolü, ECDH, AES-CTR)
  Program.cs         # Uygulama başlatma, DI, middleware hattı
MyPersonalGit.Tests/
  UnitTest1.cs       # InMemory veritabanı ile xUnit testleri
```

## Testleri Çalıştırma

```bash
dotnet test
```

## Lisans

MIT
