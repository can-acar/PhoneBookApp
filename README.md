# Telefon Rehberi Uygulaması (PhoneBookApp)

Modern mikroservis mimarisiyle geliştirilmiş telefon rehberi yönetim sistemi. Kişilerin eklenmesi, düzenlenmesi, silinmesi ve konum bazlı istatistiksel raporların oluşturulması için tasarlanmıştır.

## Sistem Mimarisi

### Mikroservisler
- **ContactService**: Kişi ve iletişim bilgilerinin yönetimi (PostgreSQL)
- **ReportService**: Asenkron rapor oluşturma ve istatistik hesaplama (MongoDB)  
- **NotificationService**: Rapor tamamlanma bildirimleri (MongoDB)

### Teknoloji Stack'i
- **Framework**: .NET 9.0
- **Veritabanı**: PostgreSQL, MongoDB, Redis
- **Mesajlaşma**: Apache Kafka + Zookeeper
- **Konteynerleştirme**: Docker + Docker Compose
- **Mimari**: Clean Architecture, CQRS, Domain-Driven Design

## Özellikler

### Kişi Yönetimi
- Kişi oluşturma, düzenleme, silme
- İletişim bilgisi ekleme/kaldırma (Telefon, E-mail, Konum)
- Kişi listeleme ve detay görüntüleme
- Konum bazlı kişi filtreleme

### Rapor Sistemi
- Konum bazlı istatistiksel raporlar
- Asenkron rapor oluşturma
- Rapor durumu takibi
- Konum başına kişi ve telefon sayısı

### Bildirim Sistemi
- Rapor tamamlanma bildirimleri
- E-mail ve SMS desteği
- Çoklu bildirim sağlayıcısı

## Kurulum ve Çalıştırma

### Gereksinimler
- Docker Desktop
- .NET 9.0 SDK (geliştirme için)
- Git

### Hızlı Başlangıç

```bash
# Depoyu klonlayın
git clone <repository-url>
cd PhoneBookApp

# Tüm servisleri başlatın
./run.sh

# Veya sadece altyapı servislerini başlatmak için
./run.sh --infra-only
```

### Docker ile Çalıştırma

```bash
# İlk kez çalıştırıyorsanız (build ile)
./run.sh --build

# Tüm servisleri durdur
./run.sh --down

# Servis loglarını görüntüle
./run.sh --logs
./run.sh --logs contactservice.api
```

### Manuel Kurulum

```bash
# Solution'ı build edin
dotnet build PhoneBookApp.sln

# Testleri çalıştırın
dotnet test

# ContactService veritabanını güncelleyin
cd ContactService.Infrastructure
dotnet ef database update
```

## Servis URL'leri

Yerel geliştirme ortamında:

- **ContactService API**: http://localhost:7001
- **ReportService API**: http://localhost:7002  
- **NotificationService API**: http://localhost:7003
- **Kafka UI**: http://localhost:8080

### Service Workflows

#### Report Service Workflow

```
👤 User → API Gateway → 📊 Report Service ←→ 📈 REST ←→ ⚙️ Contact Service
                              ↓                               ↓
                              ↓                         PostgreSQL/MongoDB
                              ↓
                        📨 Kafka → 🔔 Notification Service
                              ↓                       ↓
                      Report Completed    (📧 Email + 📱 SMS + 🌐 WebSocket)
                           ↓
                     MongoDB (Report Data)
```

#### Contact Service Workflow

```
👤 User → API Gateway → 🔄 Cache → ⚙️ Contact Service → 📨 Kafka → OutBox
                           ↑          ↓	                 ↓
                        Cache       Contact Created/   ⚠️ Dead Letter
                       Updates     Updated/Deleted        Queue
                                        ↓
                                 PostgreSQL
```

## API Kullanımı

### Kişi İşlemleri

```bash
# Tüm kişileri listele
GET http://localhost:7001/api/v1/contacts

# Yeni kişi ekle
POST http://localhost:7001/api/v1/contacts
{
  "firstName": "Ahmet",
  "lastName": "Yılmaz", 
  "company": "ABC Şirketi"
}

# Kişiye iletişim bilgisi ekle
POST http://localhost:7001/api/v1/contacts/{id}/contact-info
{
  "infoType": "PhoneNumber",
  "content": "+905551234567"
}

# Konum bazlı kişi listele
GET http://localhost:7001/api/v1/contacts/location/İstanbul
```

### Rapor İşlemleri

```bash
# Rapor talep et
POST http://localhost:7002/api/reports
{
  "location": "İstanbul"
}

# Rapor durumunu kontrol et
GET http://localhost:7002/api/reports/{reportId}

# Tüm raporları listele
GET http://localhost:7002/api/reports
```

## Geliştirme

### Proje Yapısı

```
PhoneBookApp/
├── ContactService/
│   ├── ContactService.Api/           # HTTP endpoints
│   ├── ContactService.ApplicationService/ # Business logic
│   ├── ContactService.Domain/        # Domain entities
│   ├── ContactService.Infrastructure/ # Data access
│   └── ContactService.Tests/         # Unit tests
├── ReportService/
├── NotificationService/
├── Shared.CrossCutting/              # Ortak utilities
└── compose.yaml                      # Docker orchestration
```

### Veritabanı

**PostgreSQL (ContactService)**
- Contacts tablosu: Temel kişi bilgileri
- ContactInfos tablosu: İletişim bilgileri
- ContactHistories tablosu: Değişiklik geçmişi
- OutboxEvents tablosu: Mesaj güvenilirliği

**MongoDB (ReportService, NotificationService)**
- Reports collection: Rapor verileri
- Notifications collection: Bildirim kayıtları

### Test Etme

```bash
# Tüm testleri çalıştır
dotnet test

# Sadece ContactService testleri
dotnet test ContactService.Tests/

# Coverage raporu oluştur
dotnet test --collect:"XPlat Code Coverage"
```

### Kafka Konuları

- `contact-events`: Kişi değişiklik olayları
- `report-requests`: Rapor talepleri
- `report-completed`: Rapor tamamlanma bildirimleri
- `notifications`: Bildirim mesajları
- `notification-errors`: Bildirim hataları

## Monitoring

### Health Check Endpoints
- ContactService: http://localhost:7001/health
- ReportService: http://localhost:7002/health  
- NotificationService: http://localhost:7003/health

### Loglar
- Structured logging (Serilog)
- Correlation ID tracking
- Service-specific log files

## Katkıda Bulunma

1. Branch oluşturun (`git checkout -b feature/yeni-ozellik`)
2. Değişikliklerinizi commit edin (`git commit -am 'Yeni özellik: ...'`)
3. Branch'i push edin (`git push origin feature/yeni-ozellik`)
4. Pull Request oluşturun

### Test Gereksinimler
- Minimum %60 kod coverage
- Unit testler geçmeli
- Integration testler çalışmalı

## Sorun Giderme

### Yaygın Problemler

**Docker servisleri başlamıyor:**
```bash
docker system prune -f
./run.sh --build
```

**Veritabanı bağlantı hatası:**
```bash
# PostgreSQL container'ının sağlık durumunu kontrol edin
docker compose ps postgres
```

**Kafka bağlantı problemi:**
```bash
# Kafka topic'lerinin oluşup oluşmadığını kontrol edin
./run.sh --logs kafka-setup
```

### Log İnceleme

```bash
# Tüm servislerin logları
./run.sh --logs

# Sadece ContactService logları  
./run.sh --logs contactservice.api

# Kafka UI üzerinden mesaj akışı
# http://localhost:8080
```

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır.