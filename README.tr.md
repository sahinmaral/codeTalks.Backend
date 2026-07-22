# codeTalks.Backend

[![CI](https://github.com/sahinmaral/codeTalks.Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/sahinmaral/codeTalks.Backend/actions/workflows/ci.yml)

🇬🇧 [Read this document in English](README.md)

**codeTalks**, geliştiriciler için tasarlanmış gerçek zamanlı bir sohbet uygulamasının back-end kısmıdır. Firebase gibi hazır bir servis kullanmak yerine, SignalR ile gerçek zamanlı mesajlaşmanın gömülü olduğu, PostgreSQL/Redis/RabbitMQ altyapısına dayanan özel bir .NET API olarak geliştirilmiştir.

Bu repo aynı zamanda kişisel bir alıştırma niteliğinde: bir side projeyi gerçekten production-ready bir standarda taşımak — sadece "çalışıyor" değil, test edilmiş, containerize edilmiş, izlenebilir ve `main`'e her şey ulaşmadan önce CI tarafından denetlenen bir yapı.

## Teknoloji yığını

| Konu | Seçim |
|---|---|
| Runtime | .NET 8 / ASP.NET Core Web API |
| Mimari | Clean Architecture + özel bir CQRS katmanı (`ICommand`/`IQuery` + `IRequestHandler`, özel bir `Dispatcher` ile) — MediatR, lisans değişikliği sonrası kaldırıldı |
| Validasyon | FluentValidation, dispatcher pipeline'ında cross-cutting bir davranış olarak |
| Mapping | Mapster |
| Kimlik doğrulama | ASP.NET Identity + JWT (access + refresh token rotasyonu) |
| Veritabanı | PostgreSQL, EF Core (Npgsql) üzerinden |
| Cache / presence | Redis — bağlantı takibi, okunmamış mesaj sayacı, ayar önbelleği |
| Mesajlaşma | RabbitMQ — kanal mesajlarının alıcılara asenkron dağıtımı |
| Gerçek zamanlı | SignalR — sohbet hub'ı ve bildirim hub'ı |
| Medya | Cloudinary — profil fotoğrafları, kanal küçük resimleri |
| Push bildirimleri | Expo push API |
| Loglama | Serilog — Development dışındaki ortamlarda structured JSON, lokalde okunabilir konsol çıktısı |
| Hata takibi | Sentry — gerçek, beklenmeyen exception'ları raporlar |
| Lokalizasyon | İngilizce + Türkçe, `Microsoft.Extensions.Localization` ile |

## Mimari

Solution, `corePackages` (framework'ten bağımsız yapı taşları — CQRS, repository pattern, security, pagination, cross-cutting concerns) ve `mainPackages` (asıl domain ve application katmanları: `Domain`, `Application`, `Infrastructure`, `Persistence`, `Presentation`, `WebAPI`) olarak ikiye ayrılmıştır; Clean Architecture bağımlılık kurallarına baştan sona uyulmuştur.

## Özellikler

- **Auth** — kayıt, giriş, rotasyonlu JWT refresh
- **Kanallar** — oluşturma/katılma (açık veya istek-onay politikalı)/ayrılma, kanal bazlı roller (Owner/Moderator/User), yönetici işlemleri (katılım isteğini onaylama/reddetme/banlama), keşif listesi, küçük resim (thumbnail)
- **Mesajlar** — kanal mesajı gönderme ve sayfalama; teslimat RabbitMQ → SignalR üzerinden gerçek zamanlı olarak dağıtılır
- **Kullanıcılar** — profil bilgisi & fotoğrafı, çevrimiçi durumu, şifre değişikliği, bildirim & kanal sessize alma ayarları
- **Cihazlar** — push bildirim cihaz token kaydı
- **Bildirimler** — kanal bazlı ve toplam okunmamış sayaçlar, okunduğunda sıfırlama

## Lokalde çalıştırma

En hızlı yol — API dahil her şey container içinde:

```bash
docker compose up --build
```

Bu komut Postgres, Redis, RabbitMQ ve API'yi (`http://localhost:5050` üzerinden erişilebilir) her biri kendi healthcheck'iyle birlikte başlatır.

Alternatif olarak, API'yi kaynak koddan, sadece altyapıyı container'da çalıştırarak da başlatabilirsiniz:

```bash
docker compose up postgres codetalks-redis codetalks-rabbitmq
dotnet run --project src/mainPackages/codeTalks.WebAPI
```

Swagger UI, Development ortamında `/swagger` adresinde mevcuttur.

## Test

```bash
dotnet test tests/codeTalks.Application.UnitTests   # 229 test — handler/validator unit testleri, NSubstitute mock'ları
dotnet test tests/Core.Application.UnitTests        # 9 test
dotnet test tests/Core.Security.UnitTests           # 5 test
dotnet test tests/codeTalks.WebAPI.IntegrationTests # 96 test — gerçek Postgres/RabbitMQ/Redis'e
                                                     # (Testcontainers) karşı tüm HTTP pipeline'ı;
                                                     # çalışan bir Docker daemon gerektirir
```

Integration test paketi, mock'lar yerine throwaway container'lara karşı gerçek ASP.NET Core host'unu — routing, auth, validation, EF Core — ayağa kaldırır; yani production'daki gerçek bağlantıları test eder, onun bir simülasyonunu değil.

## CI/CD

`main`'e yapılan her push ve pull request, GitHub Actions üzerinden tüm test paketini (build → unit testler → integration testler) çalıştırır. `main` branch korumalıdır: değişiklikler bir PR üzerinden geçer ve merge edilmeden önce kontrolün geçmesi gerekir — bu kural adminler için de geçerlidir.

`main`'e yapılan her merge sonrası, ikinci bir job Docker image'ını build edip GitHub Container Registry'ye (`ghcr.io/sahinmaral/codetalks-backend`, `latest` ve commit SHA etiketleriyle) yayınlar.

## Gözlemlenebilirlik (Observability)

- **Health check'ler** — `GET /health/live` (liveness, süreç yanıt verdiği sürece her zaman healthy) ve `GET /health/ready` (readiness — Postgres, Redis ve RabbitMQ'nun gerçekten erişilebilir olduğunu doğrular). Her ikisi de `docker-compose.yml`'deki healthcheck'ler tarafından kullanılır.
- **Structured logging** — Serilog, her HTTP isteği için otomatik olarak tek satır log.
- **Hata takibi** — gerçek, beklenmeyen exception'lar Sentry'e raporlanır.

## İlgili repo

Mobil uygulama kaynak kodu: [codeTalks](https://github.com/sahinmaral/codeTalks)
