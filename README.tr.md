🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

ASP.NET Core ve Blazor Server ile oluşturulmuş, GitHub benzeri web arayüzüne sahip, kendi sunucunuzda barındırabileceğiniz bir Git sunucusu. Depoları görüntüleyin, sorunları, pull request'leri, wiki'leri, projeleri ve daha fazlasını yönetin — hepsi kendi bilgisayarınızdan veya sunucunuzdan.

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
  - [Klonlama ve Gönderme](#3-klonlama-ve-gönderme)
  - [IDE'den Klonlama](#4-ideden-klonlama)
  - [Web Düzenleyici](#5-web-düzenleyicisini-kullanma)
  - [Container Registry](#6-container-registry)
  - [Paket Registry](#7-paket-registry)
  - [Pages (Statik Siteler)](#8-pages-statik-site-barındırma)
  - [Push Bildirimleri](#9-push-bildirimleri)
  - [SSH Anahtar Kimlik Doğrulama](#10-ssh-anahtar-kimlik-doğrulama)
  - [LDAP / Active Directory](#11-ldap--active-directory-kimlik-doğrulama)
  - [Depo Gizli Anahtarları](#12-depo-gizli-anahtarları)
  - [OAuth / SSO Giriş](#13-oauth--sso-giriş)
  - [Depo İçe Aktarma](#14-depo-içe-aktarma)
  - [Fork ve Upstream Senkronizasyonu](#15-fork-ve-upstream-senkronizasyonu)
  - [CI/CD Otomatik Sürüm](#16-cicd-otomatik-sürüm)
  - [RSS/Atom Akışları](#17-rssatom-akışları)
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
- **Depo Yönetimi** — Tam kod tarayıcısı, dosya düzenleyicisi, commit geçmişi, dallar ve etiketlerle Git depolarını oluşturun, görüntüleyin ve silin
- **Depo İçe Aktarma/Taşıma** — GitHub, GitLab, Bitbucket, Gitea/Forgejo/Gogs veya herhangi bir Git URL'sinden isteğe bağlı sorun ve PR aktarımı ile depoları içe aktarın. İlerleme takibiyle arka plan işleme
- **Depo Arşivleme** — Depoları görsel rozetlerle salt okunur olarak işaretleyin; arşivlenmiş depolar için push işlemleri engellenir
- **Git Smart HTTP** — Basic Auth ile HTTP üzerinden klonlama, alma ve gönderme
- **Yerleşik SSH Sunucusu** — Git işlemleri için yerel SSH sunucusu — harici OpenSSH gerekmez. ECDH anahtar değişimi, AES-CTR şifreleme ve açık anahtar kimlik doğrulaması (RSA, ECDSA, Ed25519) desteği
- **SSH Anahtar Kimlik Doğrulama** — Hesabınıza SSH açık anahtarları ekleyin ve otomatik yönetilen `authorized_keys` (veya yerleşik SSH sunucusu) ile SSH üzerinden Git işlemlerini doğrulayın
- **Fork ve Upstream Senkronizasyonu** — Depoları forklayın, tek tıkla upstream ile senkronize edin ve arayüzde fork ilişkilerini görün
- **Git LFS** — İkili dosyaları izlemek için Large File Storage desteği
- **Depo Yansılama** — Harici Git uzak sunuculara/sunuculardan depo yansılama
- **Karşılaştırma Görünümü** — İleri/geri commit sayıları ve tam fark görüntüleme ile dalları karşılaştırın
- **Dil İstatistikleri** — Her depo sayfasında GitHub tarzı dil dağılım çubuğu
- **Dal Koruması** — Zorunlu incelemeler, durum kontrolleri, force-push önleme ve CODEOWNERS onay zorunluluğu için yapılandırılabilir kurallar
- **İmzalı Commit Zorunluluğu** — Birleştirme öncesinde tüm commit'lerin GPG ile imzalanmasını gerektiren dal koruma kuralı
- **Etiket Koruması** — Glob kalıp eşleştirme ve kullanıcı bazlı izin listeleri ile etiketleri silme, zorla güncelleme ve yetkisiz oluşturmadan koruma
- **Commit İmza Doğrulama** — Commitler ve açıklamalı etiketlerde GPG imza doğrulama, arayüzde "Verified" / "Signed" rozetleri
- **Depo Etiketleri** — Depo başına özel renklerle etiket yönetimi; şablonlardan depo oluştururken etiketler otomatik olarak kopyalanır
- **AGit Flow** — Push-to-review iş akışı: `git push origin HEAD:refs/for/main` fork yapmadan veya uzak dal oluşturmadan bir pull request oluşturur. Sonraki push'larda mevcut açık PR'ları günceller
- **Keşfet** — Arama, sıralama ve konu filtreleme ile erişilebilir tüm depoları görüntüleyin
- **Star from Explore** — Her depoyu açmadan Keşfet sayfasından doğrudan depolara yıldız ekleyin ve kaldırın
- **Autolink References** — `#123` ifadesini otomatik olarak sorun bağlantılarına dönüştürün, ayrıca depo başına yapılandırılabilir özel kalıplar (örn. `JIRA-456` → harici URL'ler) desteği
- **Arama** — Depolar, sorunlar, PR'lar ve kod genelinde tam metin arama
- **License Detection** — LICENSE dosyalarını otomatik olarak algılar ve yaygın lisansları (MIT, Apache-2.0, GPL, BSD, ISC, MPL, Unlicense) depo kenar çubuğundaki bir rozetle tanımlar

### İş Birliği
- **Sorunlar ve Pull Request'ler** — Etiketler, çoklu atananlar, bitiş tarihleri ve incelemelerle sorun ve PR oluşturun, yorum yapın, kapatın/yeniden açın. PR'ları merge commit, squash veya rebase stratejileriyle birleştirin. Yan yana fark görünümü ile web tabanlı birleştirme çakışması çözümü
- **Sorun Bağımlılıkları** — Döngüsel bağımlılık tespiti ile sorunlar arasında "engelleyen" ve "engellenen" ilişkilerini tanımlayın
- **Sorun Sabitleme ve Kilitleme** — Önemli sorunları listenin üstüne sabitleyin ve daha fazla yorumu önlemek için konuşmaları kilitleyin
- **Yorum Düzenleme ve Silme** — Sorunlar ve pull request'lerdeki kendi yorumlarınızı "(düzenlendi)" göstergesiyle düzenleyin veya silin
- **@Mention Notifications** — Yorumlarda kullanıcıları @bahsederek onlara doğrudan bildirim gönderin
- **Birleştirme Çakışması Çözümü** — Base/ours/theirs görünümleri, hızlı kabul düğmeleri ve çakışma işaretçisi doğrulaması ile tarayıcıda doğrudan birleştirme çakışmalarını çözün
- **Squash Commit Message** — Bir pull request'i squash-merge yaparken commit mesajını özelleştirin
- **Branch Delete After Merge** — Bir pull request birleştirildikten sonra kaynak dalı otomatik olarak silme seçeneği, varsayılan olarak etkin
- **Tartışmalar** — Kategoriler (Genel, Soru-Cevap, Duyurular, Fikirler, Göster ve Anlat, Anketler), sabitleme/kilitleme, yanıt olarak işaretleme ve oylama ile depo başına GitHub Discussions tarzı zincirleme konuşmalar
- **Kod İnceleme Önerileri** — PR satır içi incelemelerinde "Değişiklik öner" modu, incelemecilerin doğrudan fark içinde kod değişikliği önermesine olanak tanır
- **Image Diff** — Pull request'lerde değiştirilen görsellerin (PNG, JPG, GIF, SVG, WebP) görsel karşılaştırması için opaklık kaydırıcısı ile yan yana görsel karşılaştırma
- **PR'larda File Tree** — Değiştirilen dosyalar arasında kolay gezinme için pull request fark görünümünde daraltılabilir dosya ağacı kenar çubuğu
- **Dosyaları Görüldü Olarak İşaretle** — Her dosya için "Görüldü" onay kutuları ve ilerleme sayacı ile pull request'lerde inceleme ilerlemesini takip etme
- **Diff Söz Dizimi Vurgulama** — Prism.js aracılığıyla pull request ve karşılaştırma diff'lerinde dile duyarlı söz dizimi renklendirmesi
- **Emoji Tepkileri** — Sorunlara, PR'lara, tartışmalara ve yorumlara beğeni/beğenmeme, kalp, gülen, kutlama, şaşkınlık, roket ve göz tepkileri
- **Auto-Merge** — Pull request'lerde otomatik birleştirmeyi etkinleştirerek tüm gerekli durum kontrolleri geçtiğinde ve incelemeler onaylandığında otomatik birleştirme
- **CI Status on PR List** — Pull request listesi her PR başlığının yanında yeşil/kırmızı/sarı CI durum simgeleri gösterir
- **Cherry-Pick / Revert via UI** — Web arayüzünden herhangi bir commit'i başka bir dala cherry-pick yapın veya bir commit'i doğrudan ya da yeni bir pull request olarak geri alın
- **Transfer Issues** — Başlığı, gövdeyi, yorumları, eşleşen etiketleri koruyarak ve orijinali bir transfer notu ile bağlayarak sorunları depolar arasında taşıyın
- **Saved Replies** — Hazır yanıtları kaydedin ve sorunlara veya pull request'lere yorum yaparken hızlıca ekleyin
- **Batch Issue Operations** — Birden fazla sorun seçin ve sorun listesinden toplu olarak kapatın veya yeniden açın
- **CODEOWNERS** — Birleştirme öncesi CODEOWNERS onayı gerektirme seçeneğiyle dosya yollarına göre PR incelemecilerini otomatik atama
- **Depo Şablonları** — Dosyaların, etiketlerin, sorun şablonlarının ve dal koruma kurallarının otomatik kopyalanması ile şablonlardan yeni depolar oluşturun
- **Taslak Sorunlar ve Sorun Şablonları** — Taslak sorunlar (devam eden çalışma) oluşturun ve varsayılan etiketlerle depo başına yeniden kullanılabilir sorun şablonları (hata raporu, özellik isteği) tanımlayın
- **PR Template** — `.github/PULL_REQUEST_TEMPLATE.md` dosyasından pull request açıklamalarını otomatik olarak önceden doldurma
- **Release Editing** — Oluşturulduktan sonra sürüm başlıklarını, açıklamalarını ve taslak/ön sürüm bayraklarını düzenleyin
- **Wiki** — Revizyon geçmişi ile depo başına Markdown tabanlı wiki sayfaları
- **Projeler** — İş organizasyonu için sürükle-bırak kartlarla Kanban panoları
- **Snippet'ler** — Söz dizimi vurgulama ve çoklu dosyalarla kod parçacıkları paylaşın (GitHub Gists gibi)
- **Organizasyonlar ve Takımlar** — Üyeler ve takımlarla organizasyonlar oluşturun, depolara takım izinleri atayın
- **Ayrıntılı İzinler** — Depolar üzerinde hassas erişim kontrolü için beş kademeli izin modeli (Read, Triage, Write, Maintain, Admin)
- **Kilometre Taşları** — İlerleme çubukları ve bitiş tarihleri ile sorun ilerlemesini kilometre taşlarına göre izleyin
- **Commit Yorumları** — İsteğe bağlı dosya/satır referansları ile bireysel commitlere yorum yapın
- **Depo Konuları** — Keşfet sayfasında bulma ve filtreleme için depoları konularla etiketleyin
- **Activity Pulse** — Depo başına haftalık özet sayfası; son 7 günde birleştirilen PR'lar, açılan/kapatılan sorunlar, commitler, en çok katkıda bulunanlar ve aktif dalları gösterir

### Web IDE
- **Tam Donanımlı Kod Editörü** — Monaco Editor tabanlı IDE; 30'dan fazla dil için söz dizimi vurgulama, birden fazla tema, HTML/CSS için Emmet kısaltma genişletme ve yapıştırma sırasında biçimlendirme
- **Dosya Yönetimi** — Dosya iç içe geçirme özellikli hiyerarşik dosya ağacı (`.razor` + `.razor.css` + `.razor.cs` gruplama), arama/filtreleme, sürükle-bırak dosya yükleme ve yeni dosya/klasör/yeniden adlandırma/silme için bağlam menüleri
- **Sekme Yönetimi** — Sürükleyerek yeniden sıralama, sabitlenmiş sekmeler, sağ tıklama bağlam menüsü (Kapat, Diğerlerini Kapat, Sağdakileri Kapat, Kaydedilenleri Kapat) ve taşma için kaydırma oklarıyla çoklu sekme arayüzü
- **Bölünmüş Editör & Diff Görünümü** — Bağımsız kaydırmalı yan yana düzenleme ve commit öncesi değişiklikleri karşılaştırma için diff görünümü
- **Entegre Terminal** — xterm.js tabanlı terminal; birden fazla terminal sekmesi ve WebSocket kabuk erişimi
- **Git Entegrasyonu** — Dal oluşturma, blame görünümü, dosya geçmişi, commit paneli, dosya seçimli kaynak kontrol ve renkli şerit çizgileri ve dal etiketleriyle görsel commit grafiği
- **Birleştirme Çakışması Çözümü** — Satır içi Mevcut Kabul Et / Gelen Kabul Et / Her İkisini Kabul Et düğmeleri; renk kodlu çakışma bölgeleri (yeşil/mavi)
- **Ara ve Değiştir** — Dosya uzantısı filtreleme, satır satır sonuçlar ve tümünü değiştir ile tüm dosyalarda küresel arama
- **Kod Navigasyonu** — Hızlı Aç (Ctrl+P), Komut Paleti (Ctrl+Shift+P), Satıra Git (Ctrl+G), ana hat/sembol paneli ve ekmek kırıntısı navigasyonu
- **CSS Renk Önizlemeleri** — CSS, SCSS ve Less dosyalarında hex/rgb/hsl değerlerinin yanında satır içi renk örnekleri
- **Minimap Vurguları** — Değiştirilen satırlar, eklenen satırlar ve çakışma işaretçileri minimap kenarında renkli işaretçiler olarak gösterilir
- **Markdown & Görüntü Önizleme** — Markdown dosyaları için düzenleme ve işlenmiş önizleme arasında geçiş; yaygın biçimler için satır içi görüntü gösterimi
- **Otomatik Kaydetme** — Yapılandırılabilir gecikme (500ms–5s) ile isteğe bağlı otomatik commit; ayarlardan veya komut paletinden açılıp kapatılabilir
- **Kalıcı Çalışma Alanı** — Açık sekmeler, sabitlenmiş sekmeler, aktif dosya, kenar çubuğu durumu ve panel modunu tarayıcı oturumları arasında hatırlar
- **Yeniden Boyutlandırılabilir Paneller** — Görsel tutamaçlarla kenar çubuğu ve alt paneli sürükleyerek yeniden boyutlandırma
- **Özelleştirilebilir Ayarlar** — Yazı tipi ailesi seçici (8 yazı tipi), yazı tipi boyutu, sekme boyutu, sözcük kaydırma, minimap, satır numaraları, parantez kılavuzları, yapışkan kaydırma, kod katlama, yazı tipi bitişikleri, yapıştırma sırasında biçimlendirme ve boşluk gösterme seçenekleri
- **Zen Modu** — Dikkat dağıtmayan tam ekran düzenleme (çıkmak için Escape)

### CI/CD ve DevOps
- **CI/CD Runner** — `.github/workflows/*.yml` dosyalarında iş akışlarını tanımlayın ve Docker container'larında çalıştırın. Push ve pull request olaylarında otomatik tetikleme
- **GitHub Actions Uyumluluğu** — Aynı iş akışı YAML'ı hem MyPersonalGit'te hem de GitHub Actions'ta çalışır. `uses:` eylemlerini (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) eşdeğer shell komutlarına çevirir
- **`needs:` ile Paralel İşler** — İşler `needs:` ile bağımlılıkları bildirir ve bağımsız olduklarında paralel çalışır. Bağımlı işler ön koşullarını bekler ve bir bağımlılık başarısız olursa otomatik olarak iptal edilir
- **Koşullu Adımlar (`if:`)** — Adımlar `if:` ifadelerini destekler: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. `if: failure()` veya `if: always()` ile temizleme adımları önceki hatalardan sonra bile çalışır
- **Adım Çıktıları (`$GITHUB_OUTPUT`)** — Adımlar `$GITHUB_OUTPUT`'a `key=value` veya `key<<DELIMITER` çok satırlı çiftler yazabilir ve sonraki adımlar bunları ortam değişkenleri olarak alır, `${{ steps.X.outputs.Y }}` söz dizimiyle uyumludur
- **`github` Bağlamı** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW` ve `CI=true` her işe otomatik olarak enjekte edilir
- **Matrix Build'ler** — `strategy.matrix` işleri birden çok değişken kombinasyonuna (örn. İS x sürüm) genişletir. `fail-fast` ve `runs-on`, adım komutları ve adım adlarında `${{ matrix.X }}` değişikliğini destekler
- **`workflow_dispatch` Girdileri** — Tipli girdi parametreleri (string, boolean, choice, number) ile manuel tetikleyiciler. Girdili iş akışlarını tetiklerken UI bir girdi formu gösterir. Değerler `INPUT_*` ortam değişkenleri olarak enjekte edilir
- **İş Zaman Aşımları (`timeout-minutes`)** — Limiti aştıklarında otomatik olarak başarısız olmak için işlerde `timeout-minutes` ayarlayın. Varsayılan: 360 dakika (GitHub Actions ile aynı)
- **İş Düzeyinde `if:`** — Koşullara göre tüm işleri atlayın. `if: always()` olan işler bağımlılıklar başarısız olsa bile çalışır. Atlanan işler çalıştırmayı başarısız yapmaz
- **İş Çıktıları** — İşler, bağımlı `needs:` işlerinin `${{ needs.X.outputs.Y }}` ile tükettiği `outputs:` bildirir. Çıktılar iş tamamlandıktan sonra adım çıktılarından çözümlenir
- **`continue-on-error`** — İşi başarısız yapmadan bireysel adımları başarısız olmasına izin verin. İsteğe bağlı doğrulama veya bildirim adımları için kullanışlıdır
- **`on.push.paths` Filtresi** — Yalnızca belirli dosyalar değiştiğinde iş akışlarını tetikleyin. Glob kalıplarını (`src/**`, `*.ts`) ve hariç tutma için `paths-ignore:` destekler
- **İş Akışlarını Yeniden Çalıştırma** — Başarısız, başarılı veya iptal edilmiş iş akışı çalıştırmalarını Actions arayüzünden tek tıkla yeniden çalıştırın. Aynı yapılandırma ile yeni bir çalıştırma oluşturur
- **`working-directory`** — Komutların nerede çalıştırılacağını kontrol etmek için iş akışı düzeyinde `defaults.run.working-directory` veya adım başına `working-directory:` ayarlayın
- **`defaults.run.shell`** — İş akışı veya adım başına özel shell yapılandırın (`bash`, `sh`, `python3`, vb.)
- **`strategy.max-parallel`** — Eşzamanlı matrix iş yürütmesini sınırlayın
- **Reusable Workflows (`workflow_call`)** — `on: workflow_call` ile iş akışları tanımlayın, diğer iş akışları bunları `uses: ./.github/workflows/build.yml` ile çağırabilir. Tip belirlenmiş girdiler, çıktılar ve gizli anahtarları destekler. Çağrılan iş akışının görevleri çağırana satır içi eklenir
- **Composite Actions** — `.github/actions/{name}/action.yml` içinde `runs: using: composite` ile çok adımlı eylemler tanımlayın. Bileşik eylemlerin adımları yürütme sırasında satır içi genişletilir
- **Environment Deployments** — Koruma kurallarıyla dağıtım ortamlarını yapılandırın (ör., `staging`, `production`): zorunlu incelemeciler, bekleme zamanlayıcıları ve dal kısıtlamaları. `environment:` içeren iş akışı görevleri yürütme öncesi onay gerektirir. Onay/reddetme arayüzü ile tam dağıtım geçmişi
- **`on.workflow_run`** — İş akışı zincirleme: İş akışı A tamamlandığında iş akışı B'yi tetikleyin. İş akışı adına ve `types: [completed]`'e göre filtreleyin
- **Otomatik Sürüm Oluşturma** — `softprops/action-gh-release` etiket, başlık, changelog gövdesi ve ön sürüm/taslak bayraklarıyla gerçek Release varlıkları oluşturur. Kaynak kodu arşivleri (ZIP ve TAR.GZ) indirilebilir varlıklar olarak otomatik eklenir
- **Otomatik Sürüm Pipeline'ı** — Yerleşik iş akışı, main'e her push'ta sürümleri otomatik etiketler, changelog oluşturur ve Docker imajlarını Docker Hub'a gönderir
- **Commit Durum Kontrolleri** — İş akışları pull request'lerde görünür şekilde commitlerde otomatik olarak pending/success/failure durumu ayarlar
- **İş Akışı İptali** — Actions arayüzünden çalışan veya sırada bekleyen iş akışlarını iptal edin
- **Eşzamanlılık Kontrolleri** — Yeni push'lar aynı iş akışının sırada bekleyen çalıştırmalarını otomatik olarak iptal eder
- **İş Akışı Ortam Değişkenleri** — YAML'da iş akışı, iş veya adım düzeyinde `env:` ayarlayın
- **Durum Rozetleri** — İş akışı ve commit durumu için gömülebilir SVG rozetleri (`/api/badge/{repo}/workflow`)
- **Yapıt İndirmeleri** — Actions arayüzünden doğrudan derleme yapıtlarını indirin
- **Gizli Anahtar Yönetimi** — CI/CD iş akışı çalıştırmalarına ortam değişkenleri olarak enjekte edilen şifreli depo gizli anahtarları (AES-256)
- **Webhook'lar** — Depo olaylarında harici hizmetleri tetikleyin
- **Prometheus Metrikleri** — İzleme için yerleşik `/metrics` uç noktası

### Paket ve Container Barındırma (20 registries)
- **Container Registry** — `docker push` ve `docker pull` ile Docker/OCI imajlarını barındırın (OCI Distribution Spec)
- **NuGet Registry** — Tam NuGet v3 API (service index, arama, push, geri yükleme) ile .NET paketlerini barındırın
- **npm Registry** — Standart npm publish/install ile Node.js paketlerini barındırın
- **PyPI Registry** — PEP 503 Simple API, JSON metadata API ve `twine upload` uyumluluğu ile Python paketlerini barındırın
- **Maven Registry** — Standart Maven depo düzeni, `maven-metadata.xml` oluşturma ve `mvn deploy` desteği ile Java/JVM paketlerini barındırın
- **Alpine Registry** — APKINDEX oluşturma ile Alpine Linux `.apk` paketlerini barındırın
- **RPM Registry** — `dnf`/`yum` için `repomd.xml` meta verileriyle RPM paketlerini barındırın
- **Chef Registry** — Chef Supermarket uyumlu API ile Chef cookbook'larını barındırın
- **Genel Paketler** — REST API aracılığıyla isteğe bağlı ikili yapıtları yükleyin ve indirin

### Statik Siteler
- **Pages** — Bir depo dalından doğrudan statik web siteleri sunun (GitHub Pages gibi), `/pages/{owner}/{repo}/` adresinde

### RSS/Atom Akışları
- **Depo Akışları** — Depo başına commitler, sürümler ve etiketler için Atom akışları (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **Kullanıcı Etkinlik Akışı** — Kullanıcı bazlı etkinlik akışı (`/api/feeds/users/{username}/activity.atom`)
- **Global Etkinlik Akışı** — Site geneli etkinlik akışı (`/api/feeds/global/activity.atom`)

### Bildirimler
- **Uygulama İçi Bildirimler** — Bahsetmeler, yorumlar ve depo etkinliği
- **Push Bildirimleri** — Kullanıcı bazlı katılım seçeneği ile gerçek zamanlı mobil/masaüstü uyarıları için Ntfy ve Gotify entegrasyonu

### Kimlik Doğrulama
- **OAuth2 / SSO** — GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord veya Twitter/X ile giriş yapın. Yöneticiler her sağlayıcı için Client ID ve Secret'ı Yönetici panelinde yapılandırır — yalnızca kimlik bilgileri doldurulmuş sağlayıcılar kullanıcılara gösterilir
- **OAuth2 Sağlayıcı** — Diğer uygulamaların "MyPersonalGit ile Giriş Yap" kullanabilmesi için kimlik sağlayıcı olarak çalışın. PKCE ile Authorization Code flow, token yenileme, userinfo uç noktası ve OpenID Connect keşfi (`.well-known/openid-configuration`) uygular
- **LDAP / Active Directory** — Kullanıcıları bir LDAP dizini veya Active Directory etki alanına karşı doğrulayın. Kullanıcılar ilk girişte senkronize edilmiş özniteliklerle (e-posta, görünen ad) otomatik oluşturulur. Gruba dayalı yönetici yükseltme, SSL/TLS ve StartTLS desteği
- **SSPI / Windows Tümleşik Kimlik Doğrulama** — Negotiate/NTLM aracılığıyla Windows etki alanı kullanıcıları için şeffaf Single Sign-On. Etki alanındaki kullanıcılar kimlik bilgisi girmeden otomatik olarak doğrulanır. Admin > Settings'den etkinleştirin (yalnızca Windows)
- **İki Faktörlü Kimlik Doğrulama** — Doğrulayıcı uygulama desteği ve kurtarma kodları ile TOTP tabanlı 2FA
- **WebAuthn / Passkey'ler** — İkinci faktör olarak FIDO2 donanım güvenlik anahtarı ve passkey desteği. YubiKey, platform doğrulayıcıları (Face ID, Windows Hello, Touch ID) ve diğer FIDO2 cihazlarını kaydedin. Klonlanmış anahtar tespiti için imza sayacı doğrulaması
- **Bağlı Hesaplar** — Kullanıcılar Ayarlar'dan hesaplarına birden fazla OAuth sağlayıcı bağlayabilir

### Yönetim
- **Yönetici Paneli** — Sistem ayarları (veritabanı sağlayıcı, TLS/HTTPS, SSH sunucusu, LDAP/AD, alt bilgi sayfaları dahil), kullanıcı yönetimi, denetim günlükleri ve istatistikler; bölüm başına ayrı kartlarda düzenlenmiş
- **Yerleşik TLS/HTTPS** — Yönetim ayarlarından doğrudan HTTPS etkinleştirme; üç sertifika seçeneği: kendinden imzalı (otomatik oluşturulmuş, 2 yıl geçerlilik), PFX/PKCS#12 dosyası veya PEM sertifika+anahtar çifti (ör. Let's Encrypt). Docker port eşleme için yapılandırılabilir dahili/harici portlar, isteğe bağlı HTTP'den HTTPS'ye yönlendirme
- **Özelleştirilebilir Alt Bilgi Sayfaları** — Admin > Settings'den düzenlenebilir Markdown içerikli Kullanım Koşulları, Gizlilik Politikası, Dokümantasyon ve İletişim sayfaları
- **Kullanıcı Profilleri** — Kullanıcı başına katkı ısı haritası, etkinlik akışı ve istatistikler
- **Gravatar Avatars** — Arayüz genelindeki kullanıcı avatarları, kullanıcı adına dayalı Gravatar identicon'ları kullanır, otomatik yedek desteğiyle
- **Kişisel Erişim Belirteçleri** — Yapılandırılabilir kapsamlar ve isteğe bağlı rota düzeyinde kısıtlamalarla (belirteç erişimini belirli API yollarına sınırlamak için `/api/packages/**` gibi glob kalıpları) belirteç tabanlı API kimlik doğrulaması
- **Yedekleme ve Geri Yükleme** — Sunucu verilerini dışa ve içe aktarma
- **Güvenlik Taraması** — [OSV.dev](https://osv.dev/) veritabanı destekli gerçek bağımlılık güvenlik açığı taraması. `.csproj` (NuGet), `package.json` (npm), `requirements.txt` (PyPI), `Cargo.toml` (Rust), `Gemfile` (Ruby), `composer.json` (PHP), `go.mod` (Go), `pom.xml` (Maven/Java) ve `pubspec.yaml` (Dart/Flutter) dosyalarından bağımlılıkları otomatik çıkarır, ardından her birini bilinen CVE'lere karşı kontrol eder. Önem derecesi, düzeltilmiş sürümler ve danışma bağlantıları raporlar. Ayrıca taslak/yayınla/kapat iş akışıyla manuel güvenlik danışmaları
- **Secret Scanning** — Her push'u otomatik olarak sızan kimlik bilgileri (AWS anahtarları, GitHub/GitLab belirteçleri, Slack belirteçleri, özel anahtarlar, API anahtarları, JWT'ler, bağlantı dizeleri ve daha fazlası) için tarar. Tam regex desteği ile 20 yerleşik kalıp. İsteğe bağlı tam depo taraması. Çözme/yanlış pozitif iş akışı ile uyarılar. API aracılığıyla yapılandırılabilir özel kalıplar
- **Dependabot-Style Auto-Update PRs** — Eski bağımlılıkları otomatik olarak kontrol eder ve güncellemek için pull request'ler oluşturur. NuGet, npm ve PyPI ekosistemlerini destekler. Yapılandırılabilir zamanlama (günlük/haftalık/aylık) ve depo başına açık PR limiti
- **Repository Insights (Traffic)** — Klonlama/çekme sayılarını, sayfa görüntülemelerini, benzersiz ziyaretçileri, en iyi yönlendiricileri ve popüler içerik yollarını takip edin. Insights sekmesinde 14 günlük özetlerle trafik grafikleri. 90 gün saklama süresi ile günlük toplama. Gizlilik için IP adresleri karma yapılır
- **Karanlık Mod** — Başlıkta bir geçiş düğmesiyle tam karanlık/aydınlık mod desteği
- **Çoklu Dil / i18n** — 930 kaynak anahtarı ile tüm 30 sayfada tam yerelleştirme. 11 dil ile birlikte gelir: İngilizce, İspanyolca, Fransızca, Almanca, Japonca, Korece, Çince (Basitleştirilmiş), Portekizce, Rusça, İtalyanca ve Türkçe. `SharedResource.{locale}.resx` dosyaları oluşturarak daha fazla dil ekleyin. Başlıktaki dil seçici ile geçiş yapın
- **Swagger / OpenAPI** — `/swagger` adresinde etkileşimli API belgeleri; tüm REST uç noktaları keşfedilebilir ve test edilebilir
- **Open Graph Meta Tags** — Depo, sorun ve PR sayfaları, Slack, Discord ve sosyal medyada zengin bağlantı önizlemeleri için og:title ve og:description içerir
- **Emoji Kısa Kodları** — GitHub tarzı emoji kısa kodları (`:white_check_mark:`, `:rocket:`, vb.) tüm Markdown görünümlerinde gerçek emoji olarak işlenir
- **Mermaid Diyagramları** — Markdown dosyalarında Mermaid diyagram oluşturma (akış şemaları, sıralama diyagramları, Gantt grafikleri vb.)
- **Matematik Oluşturma** — Markdown'da LaTeX/KaTeX matematik ifadeleri (`$inline$` ve `$$display$$` söz dizimi)
- **CSV/TSV Görüntüleyici** — CSV ve TSV dosyaları düz metin yerine biçimlendirilmiş, sıralanabilir tablolar olarak görüntülenir
- **Keyboard Shortcuts** — Kısayol yardım modalı için `?` tuşuna basın. `/` aramaya odaklanır, `g i` Sorunlara, `g p` Pull Request'lere, `g h` Ana Sayfaya, `g n` Bildirimlere gider
- **Health Check Endpoint** — `/health` Docker/Kubernetes izlemesi için veritabanı bağlantı durumunu içeren JSON döndürür
- **Sitemap.xml** — `/sitemap.xml` adresinde arama motoru indekslemesi için tüm genel depoları listeleyen dinamik XML site haritası
- **Line Linking** — Dosya görüntüleyicide satır numaralarına tıklayarak yüklemede satır vurgulama ile paylaşılabilir `#L42` URL'leri oluşturun
- **File Download** — Uygun Content-Disposition başlıkları ile dosya görüntüleyiciden tek tek dosya indirin
- **Jupyter Notebook Görüntüleme** — `.ipynb` dosyaları kod hücreleri, Markdown, çıktılar ve satır içi görsellerle biçimlendirilmiş not defterleri olarak görüntülenir
- **Depo Transferi** — Depo ayarlarından depo sahipliğini başka bir kullanıcıya veya kuruluşa aktarma
- **Varsayılan Dal Yapılandırması** — Ayarlar sekmesinden depo başına varsayılan dalı değiştirme
- **Rename Repository** — Settings üzerinden depo adını değiştirme ve tüm referansların (issues, PRs, yıldızlar, webhooks, secrets vb.) otomatik güncellenmesi
- **User-Level Secrets** — Bir kullanıcının sahip olduğu tüm depolar arasında paylaşılan şifrelenmiş Secrets. Settings > Secrets üzerinden yönetilir
- **Organization-Level Secrets** — Bir organizasyondaki tüm depolar arasında paylaşılan şifrelenmiş Secrets. Organizasyonun Secrets sekmesinden yönetilir
- **Repository Pinning** — Hızlı erişim için kullanıcı profil sayfanıza en fazla 6 favori depoyu sabitleyin
- **Git Hooks Management** — Depo başına sunucu tarafı Git Hooks'ları (pre-receive, update, post-receive, post-update, pre-push) görüntüleme, düzenleme ve yönetme için Web UI
- **Protected File Patterns** — Belirli dosyalardaki değişiklikler için inceleme onayı gerektiren glob kalıplı dal koruma kuralı (örn. `*.lock`, `migrations/**`, `.github/workflows/*`)
- **External Issue Tracker** — Depoları harici bir sorun takipçisine (Jira, Linear, vb.) özel URL kalıplarıyla bağlanacak şekilde yapılandırın
- **Federation (NodeInfo/WebFinger)** — Örnekler arası keşfedilebilirlik için NodeInfo 2.0 keşfi, WebFinger ve host-meta
- **Distributed CI Runners** — Harici runner'lar API aracılığıyla kaydolabilir, kuyruktaki işleri sorgulayabilir ve sonuçları raporlayabilir

## Teknoloji Yığını

| Bileşen | Teknoloji |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (etkileşimli sunucu tarafı işleme) |
| Veritabanı | SQLite (varsayılan) veya Entity Framework Core 10 aracılığıyla PostgreSQL |
| Git Motoru | LibGit2Sharp |
| Kimlik Doğrulama | BCrypt parola karma, oturum tabanlı kimlik doğrulama, PAT belirteçleri, OAuth2 (8 sağlayıcı + sağlayıcı modu), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| SSH Sunucusu | Yerleşik SSH2 protokol uygulaması (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| İzleme | Prometheus metrikleri |

## Hızlı Başlangıç

### Ön Koşullar

- [Docker](https://docs.docker.com/get-docker/) (önerilen)
- Veya yerel geliştirme için [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git

### Docker (Önerilen)

Docker Hub'dan çekin ve çalıştırın:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Port 2222 isteğe bağlıdır — yalnızca Admin > Settings'de yerleşik SSH sunucusunu etkinleştirirseniz gereklidir.

Veya Docker Compose kullanın:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

Uygulama **http://localhost:8080** adresinde kullanılabilir olacaktır.

> **Varsayılan kimlik bilgileri**: `admin` / `admin`
>
> İlk girişten sonra **varsayılan parolayı derhal değiştirin** — Yönetici paneli üzerinden.

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
| `Git__RequireAuth` | Git HTTP işlemleri için kimlik doğrulama gerektirir | `true` |
| `Git__Users__<username>` | Git HTTP Basic Auth kullanıcısı için parola ayarlayın | — |
| `RESET_ADMIN_PASSWORD` | Başlangıçta acil yönetici parola sıfırlama | — |
| `Secrets__EncryptionKey` | Depo gizli anahtarları için özel şifreleme anahtarı | Veritabanı bağlantı dizesinden türetilir |
| `Ssh__DataDir` | SSH verileri (ana bilgisayar anahtarları, authorized_keys) için dizin | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Oluşturulan authorized_keys dosyasının yolu | `<DataDir>/authorized_keys` |

> **Not:** Yerleşik SSH sunucusu portu ve LDAP ayarları ortam değişkenleri yerine Yönetici paneli (Admin > Settings) üzerinden yapılandırılır. Bu, yeniden dağıtım yapmadan değiştirmenize olanak tanır.

## Kullanım

### 1. Giriş Yapma

Uygulamayı açın ve **Sign In**'e tıklayın. Yeni kurulumda varsayılan kimlik bilgilerini (`admin` / `admin`) kullanın. **Admin** paneli üzerinden veya Admin > Settings'de kullanıcı kaydını etkinleştirerek ek kullanıcılar oluşturun.

### 2. Depo Oluşturma

Ana sayfadaki yeşil **New** düğmesine tıklayın, bir ad girin ve **Create**'e tıklayın. Bu, sunucuda klonlayabileceğiniz, push yapabileceğiniz ve web arayüzü üzerinden yönetebileceğiniz boş bir Git deposu oluşturur.

### 3. Klonlama ve Gönderme

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

### 5. Web Düzenleyicisini Kullanma

Dosyaları doğrudan tarayıcıda düzenleyebilirsiniz:
- Bir depoya gidin ve herhangi bir dosyaya tıklayın, ardından **Edit**'e tıklayın
- Yerel bir klon olmadan dosya eklemek için **Add files > Create new file** kullanın
- Bilgisayarınızdan yüklemek için **Add files > Upload files/folder** kullanın

### 6. Container Registry

Docker/OCI imajlarını doğrudan sunucunuza gönderin ve çekin:

```bash
# Giriş yapın (Settings > Access Tokens'dan bir Personal Access Token kullanın)
docker login localhost:8080 -u youruser

# Bir imaj gönderin
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Bir imaj çekin
docker pull localhost:8080/myapp:v1
```

> **Not:** Docker varsayılan olarak HTTPS gerektirir. HTTP için sunucunuzu Docker'ın `~/.docker/daemon.json` dosyasındaki `insecure-registries`'e ekleyin:
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
# Bir paket yükleyin
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# twine ile yükleyin
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
<!-- pom.xml dosyanıza depoyu ekleyin -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- settings.xml dosyasına kimlik bilgilerini ekleyin -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
```

**Genel (herhangi bir ikili dosya):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Web arayüzünde `/packages` adresinden tüm paketlere göz atın.

### 8. Pages (Statik Site Barındırma)

Bir depo dalından statik web siteleri sunun:

1. Deponuzun **Settings** sekmesine gidin ve **Pages**'i etkinleştirin
2. Dalı ayarlayın (varsayılan: `gh-pages`)
3. Bu dala HTML/CSS/JS gönderin
4. `http://localhost:8080/pages/{username}/{repo}/` adresini ziyaret edin

### 9. Push Bildirimleri

Sorunlar, PR'lar veya yorumlar oluşturulduğunda telefonunuzda veya masaüstünüzde push bildirimleri almak için **Admin > System Settings**'de Ntfy veya Gotify yapılandırın. Kullanıcılar **Settings > Notifications**'da katılım/ayrılma seçeneğini ayarlayabilir.

### 10. SSH Anahtar Kimlik Doğrulama

Parolasız Git işlemleri için SSH anahtarlarını kullanın. İki seçenek vardır:

#### Seçenek A: Yerleşik SSH Sunucusu (Önerilen)

Harici SSH daemon gerekmez — MyPersonalGit kendi SSH sunucusunu çalıştırır:

1. **Admin > Settings**'e gidin ve **Built-in SSH Server**'ı etkinleştirin
2. SSH portunu ayarlayın (varsayılan: 2222) — sistem SSH çalışmıyorsa 22 kullanın
3. Ayarları kaydedin ve sunucuyu yeniden başlatın (port değişiklikleri yeniden başlatma gerektirir)
4. **Settings > SSH Keys**'e gidin ve açık anahtarınızı ekleyin (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub` veya `~/.ssh/id_ecdsa.pub`)
5. SSH ile klonlayın:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

Yerleşik SSH sunucusu ECDH-SHA2-NISTP256 anahtar değişimi, AES-128/256-CTR şifreleme, HMAC-SHA2-256 ve Ed25519, RSA ve ECDSA anahtarlarıyla açık anahtar kimlik doğrulamasını destekler.

#### Seçenek B: Sistem OpenSSH

Sisteminizin SSH daemon'ını kullanmayı tercih ediyorsanız:

1. **Settings > SSH Keys**'e gidin ve açık anahtarınızı ekleyin
2. MyPersonalGit, tüm kayıtlı SSH anahtarlarından bir `authorized_keys` dosyasını otomatik olarak yönetir
3. Sunucunuzun OpenSSH'sini oluşturulan authorized_keys dosyasını kullanacak şekilde yapılandırın:
   ```
   # /etc/ssh/sshd_config dosyasında
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. SSH ile klonlayın:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

SSH kimlik doğrulama servisi ayrıca OpenSSH'nin `AuthorizedKeysCommand` yönergesiyle kullanılmak üzere `/api/ssh/authorized-keys` adresinde bir API sunar.

### 11. LDAP / Active Directory Kimlik Doğrulama

Kullanıcıları kuruluşunuzun LDAP dizini veya Active Directory etki alanına karşı doğrulayın:

1. **Admin > Settings**'e gidin ve **LDAP / Active Directory Authentication**'a kaydırın
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
6. İsteğe bağlı olarak bir **Admin Group DN** ayarlayın — bu grubun üyeleri otomatik olarak yöneticiye yükseltilir
7. Ayarları doğrulamak için **Test LDAP Connection**'a tıklayın
8. Ayarları kaydedin

Kullanıcılar artık giriş sayfasında etki alanı kimlik bilgileriyle oturum açabilir. İlk girişte, dizinden senkronize edilmiş özniteliklerle yerel bir hesap otomatik oluşturulur. LDAP kimlik doğrulaması Git HTTP işlemleri (klonlama/push) için de kullanılır.

### 12. Depo Gizli Anahtarları

CI/CD iş akışlarında kullanılmak üzere depolara şifreli gizli anahtarlar ekleyin:

1. Deponuzun **Settings** sekmesine gidin
2. **Secrets** kartına kaydırın ve **Add secret**'a tıklayın
3. Bir ad (örn. `DEPLOY_TOKEN`) ve değer girin — değer AES-256 ile şifrelenir
4. Gizli anahtarlar her iş akışı çalıştırmasına ortam değişkenleri olarak otomatik enjekte edilir

İş akışınızda gizli anahtarlara başvurun:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO Giriş

Harici kimlik sağlayıcılarıyla giriş yapın:

1. **Admin > OAuth / SSO**'ya gidin ve etkinleştirmek istediğiniz sağlayıcıları yapılandırın
2. Sağlayıcının geliştirici konsolundan **Client ID** ve **Client Secret** girin
3. **Enable** işaretleyin — giriş sayfasında yalnızca her iki kimlik bilgisi doldurulmuş sağlayıcılar görünür
4. Her sağlayıcı için geri çağrı URL'si yönetici panelinde gösterilir (örn. `https://yourserver/oauth/callback/github`)

Desteklenen sağlayıcılar: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Kullanıcılar **Settings > Linked Accounts**'da hesaplarına birden fazla sağlayıcı bağlayabilir.

### 14. Depo İçe Aktarma

Tam geçmişle harici kaynaklardan depoları içe aktarın:

1. Ana sayfada **Import**'a tıklayın
2. Bir kaynak türü seçin (Git URL, GitHub, GitLab veya Bitbucket)
3. Depo URL'sini ve isteğe bağlı olarak özel depolar için bir kimlik doğrulama belirteci girin
4. GitHub/GitLab/Bitbucket içe aktarmaları için isteğe bağlı olarak sorunları ve pull request'leri içe aktarın
5. İçe aktarma ilerlemesini Import sayfasında gerçek zamanlı izleyin

### 15. Fork ve Upstream Senkronizasyonu

Bir depoyu forklayın ve senkronize tutun:

1. Herhangi bir depo sayfasındaki **Fork** düğmesine tıklayın
2. Kullanıcı adınız altında orijinale geri bağlantı ile bir fork oluşturulur
3. Upstream'den en son değişiklikleri çekmek için "forked from" rozetinin yanındaki **Sync fork**'a tıklayın

### 16. CI/CD Otomatik Sürüm

MyPersonalGit, main'e her push'ta otomatik olarak etiketleyen, sürüm oluşturan ve Docker imajlarını gönderen yerleşik bir CI/CD pipeline içerir. İş akışları push'ta otomatik tetiklenir — harici CI servisi gerekmez.

**Nasıl çalışır:**
1. `main`'e push `.github/workflows/release.yml`'i otomatik tetikler
2. Yama sürümünü artırır (`v1.15.1` -> `v1.15.2`), bir git etiketi oluşturur
3. Docker Hub'a giriş yapar, imajı derler ve hem `:latest` hem de `:vX.Y.Z` olarak gönderir

**Kurulum:**
1. MyPersonalGit'te deponuzun **Settings > Secrets** bölümüne gidin
2. Docker Hub erişim belirtecinizle `DOCKERHUB_TOKEN` adında bir gizli anahtar ekleyin
3. MyPersonalGit container'ının Docker soketi bağlı olduğundan emin olun (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Main'e push yapın — iş akışı otomatik olarak tetiklenir

**GitHub Actions uyumluluğu:**
Aynı iş akışı YAML'ı GitHub Actions'ta da çalışır — değişiklik gerekmez. MyPersonalGit çalışma zamanında `uses:` eylemlerini eşdeğer shell komutlarına çevirir:

| GitHub Action | MyPersonalGit Çevirisi |
|---|---|
| `actions/checkout@v4` | Depo zaten `/workspace`'e klonlanmış |
| `actions/setup-dotnet@v4` | Resmi kurulum betiği ile .NET SDK yükler |
| `actions/setup-node@v4` | NodeSource aracılığıyla Node.js yükler |
| `actions/setup-python@v5` | apt/apk aracılığıyla Python yükler |
| `actions/setup-java@v4` | apt/apk aracılığıyla OpenJDK yükler |
| `docker/login-action@v3` | stdin parolası ile `docker login` |
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
Adımlar `$GITHUB_OUTPUT` aracılığıyla sonraki adımlara değer iletebilir:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Matrix build'ler:**
`strategy.matrix` ile işleri birden çok kombinasyona yayın:
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
Bu 4 iş oluşturur: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)`, vb. Hepsi paralel çalışır.

**Girdili manuel tetikleyiciler (`workflow_dispatch`):**
Manuel tetiklemede UI'da form olarak gösterilen tipli girdiler tanımlayın:
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
Çok uzun çalışırlarsa otomatik başarısız olmak için işlerde `timeout-minutes` ayarlayın:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Varsayılan zaman aşımı 360 dakikadır (6 saat), GitHub Actions ile aynıdır.

**İş düzeyinde koşullar:**
Koşullara göre işleri atlamak için `if:` kullanın:
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
İşler `outputs:` aracılığıyla bağımlı işlere değer iletebilir:
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

**Hatada devam:**
İşi başarısız yapmadan bir adımın başarısız olmasına izin verin:
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
Komutların nerede çalıştırılacağını ayarlayın:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # src/app'de çalışır
      - run: npm test
        working-directory: tests  # varsayılanı geçersiz kılar
```

**İş akışlarını yeniden çalıştırma:**
Aynı işler, adımlar ve yapılandırma ile yeni bir çalıştırma oluşturmak için tamamlanan, başarısız olan veya iptal edilen herhangi bir iş akışı çalıştırmasında **Re-run** düğmesine tıklayın.

**Pull request iş akışları:**
`on: pull_request` olan iş akışları, taslak olmayan bir PR oluşturulduğunda otomatik tetiklenir ve kaynak dala karşı kontroller çalıştırır.

**Commit durum kontrolleri:**
İş akışları otomatik olarak commit durumlarını (pending/success/failure) ayarlar, böylece PR'larda derleme sonuçlarını görebilir ve dal koruması aracılığıyla zorunlu kontrolleri uygulayabilirsiniz.

**İş akışı iptali:**
Hemen durdurmak için Actions arayüzünde çalışan veya sırada bekleyen herhangi bir iş akışında **Cancel** düğmesine tıklayın.

**Durum rozetleri:**
README'nize veya herhangi bir yere derleme durumu rozetleri ekleyin:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
İş akışı adına göre filtreleyin: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom Akışları

Herhangi bir RSS okuyucuda standart Atom akışlarını kullanarak depo etkinliğine abone olun:

```
# Depo commitleri
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Depo sürümleri
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Depo etiketleri
http://localhost:8080/api/feeds/MyRepo/tags.atom

# Kullanıcı etkinliği
http://localhost:8080/api/feeds/users/admin/activity.atom

# Global etkinlik (tüm depolar)
http://localhost:8080/api/feeds/global/activity.atom
```

Herkese açık depolar için kimlik doğrulama gerekmez. Değişikliklerden haberdar olmak için bu URL'leri herhangi bir akış okuyucuya (Feedly, Miniflux, FreshRSS, vb.) ekleyin.

## Veritabanı Yapılandırması

MyPersonalGit varsayılan olarak **SQLite** kullanır — sıfır yapılandırma, tek dosyalı veritabanı, kişisel kullanım ve küçük takımlar için mükemmeldir.

Daha büyük dağıtımlar için (çok sayıda eş zamanlı kullanıcı, yüksek kullanılabilirlik veya zaten PostgreSQL çalıştırıyorsanız) **PostgreSQL**'e geçebilirsiniz:

### PostgreSQL Kullanımı

**Docker Compose** (PostgreSQL için önerilir):
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

EF Core migration'ları her iki sağlayıcı için de başlangıçta otomatik çalışır. Manuel şema kurulumu gerekmez.

### Yönetici Panelinden Geçiş

Veritabanı sağlayıcısını doğrudan web arayüzünden de değiştirebilirsiniz:

1. **Admin > Settings**'e gidin — **Database** kartı en üsttedir
2. Sağlayıcı açılır menüsünden **PostgreSQL**'i seçin
3. PostgreSQL bağlantı dizenizi girin (örn. `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. **Save Database Settings**'e tıklayın
5. Değişikliğin etkili olması için uygulamayı yeniden başlatın

Yapılandırma `~/.mypersonalgit/database.json` dosyasına kaydedilir (veritabanının dışında, böylece bağlanmadan önce okunabilir).

### Veritabanı Seçimi

| | SQLite | PostgreSQL |
|---|---|---|
| **Kurulum** | Sıfır yapılandırma (varsayılan) | Bir PostgreSQL sunucusu gerektirir |
| **En iyi kullanım** | Kişisel kullanım, küçük takımlar, NAS | 50+ kişilik takımlar, yüksek eş zamanlılık |
| **Yedekleme** | `.db` dosyasını kopyalayın | Standart `pg_dump` |
| **Eş zamanlılık** | Tek yazıcı (çoğu kullanım için yeterli) | Tam çoklu yazıcı |
| **Geçiş** | Yok | Sağlayıcıyı değiştirin + uygulamayı çalıştırın (otomatik geçiş) |

## NAS'a Dağıtım

MyPersonalGit, Docker aracılığıyla bir NAS'ta (QNAP, Synology, vb.) harika çalışır:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

Docker soket bağlaması isteğe bağlıdır — yalnızca CI/CD iş akışı yürütmesi istiyorsanız gereklidir. Port 2222 yalnızca yerleşik SSH sunucusunu etkinleştirirseniz gereklidir.

## Yapılandırma

Tüm ayarlar `appsettings.json` dosyasında, ortam değişkenleri aracılığıyla veya `/admin` adresindeki Yönetici paneli üzerinden yapılandırılabilir:

- Veritabanı sağlayıcısı (SQLite veya PostgreSQL)
- Proje kök dizini
- Kimlik doğrulama gereksinimleri
- Kullanıcı kayıt ayarları
- Özellik geçişleri (Issues, Wiki, Projects, Actions)
- Kullanıcı başına maksimum depo boyutu ve sayısı
- E-posta bildirimleri için SMTP ayarları
- Push bildirim ayarları (Ntfy/Gotify)
- Yerleşik SSH sunucusu (etkinleştirme/devre dışı bırakma, port)
- LDAP/Active Directory kimlik doğrulaması (sunucu, Bind DN, arama tabanı, kullanıcı filtresi, öznitelik eşleme, yönetici grubu)
- OAuth/SSO sağlayıcı yapılandırması (sağlayıcı başına Client ID/Secret)

## Proje Yapısı

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor sayfaları (Home, RepoDetails, Issues, PRs, Packages, vb.)
  Controllers/       # REST API uç noktaları (NuGet, npm, Generic, Registry, vb.)
  Data/              # EF Core DbContext, servis uygulamaları
  Models/            # Alan modelleri
  Migrations/        # EF Core migration'ları
  Services/          # Middleware (kimlik doğrulama, Git HTTP backend, Pages, Registry auth)
    SshServer/       # Yerleşik SSH sunucusu (SSH2 protokolü, ECDH, AES-CTR)
  Program.cs         # Uygulama başlatma, DI, middleware pipeline
MyPersonalGit.Tests/
  UnitTest1.cs       # InMemory veritabanı ile xUnit testleri
```

## Testleri Çalıştırma

```bash
dotnet test
```

## Lisans

MIT
