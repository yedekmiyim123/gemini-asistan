# 🔄 OTOMATİK GÜNCELLEME SİSTEMİ

## 📋 GENEL BAKIŞ

Kullanıcılar artık yeni versiyonları **otomatik olarak** indirebilir ve yükleyebilir!

---

## ✨ ÖZELLİKLER

### 1. Otomatik Kontrol
- ✅ Program her açıldığında güncelleme kontrolü yapar (24 saatte bir)
- ✅ Arka planda çalışır, kullanıcıyı rahatsız etmez
- ✅ İnternet yoksa sessizce başarısız olur

### 2. Manuel Güncelleme
- ✅ Kullanıcı `güncelle` yazarak manuel kontrol edebilir
- ✅ Yeni özellikler ve düzeltmeler gösterilir
- ✅ Kullanıcı onayı ile güncelleme yapılır

### 3. Güvenli Güncelleme
- ✅ Eski versiyon yedeklenir (.backup)
- ✅ Hata olursa geri dönülebilir
- ✅ Program otomatik yeniden başlar

---

## 🚀 KULLANICI TARAFINDA

### Otomatik Kontrol
```
=== Gemini Yapay Zeka Asistanı ===
✓ 10 önceki mesaj yüklendi
✓ Kullanıcı profili yüklendi
✓ Gelişmiş AI özellikleri aktif

🔔 Yeni güncelleme mevcut!
   Versiyon 9.1 yayınlandı.
   'güncelle' yazarak güncelleyebilirsiniz.

Sen: 
```

### Manuel Güncelleme
```
Sen: güncelle

🔍 Güncelleme kontrol ediliyor...

╔══════════════════════════════════════════════════════════════╗
║                  YENİ GÜNCELLEME MEVCUT! 🎉                  ║
╚══════════════════════════════════════════════════════════════╝

📌 Mevcut Versiyon: 9.0
🆕 Yeni Versiyon: 9.1
📅 Tarih: 2025-11-20

✨ Yenilikler:
  • Yeni özellik 1
  • Yeni özellik 2

🔧 Düzeltmeler:
  • Bug fix 1
  • Bug fix 2

Güncellemek istiyor musunuz? (E/H): e

╔══════════════════════════════════════════════════════════════╗
║                  GÜNCELLEME İNDİRİLİYOR                      ║
╚══════════════════════════════════════════════════════════════╝

📥 İndiriliyor...
✓ İndirme tamamlandı!

📦 Güncelleme yükleniyor...
✓ Güncelleme başlatıldı!
Program yeniden başlatılacak...
```

---

## 🛠️ GELİŞTİRİCİ TARAFINDA

### 1. Yeni Versiyon Hazırlama

#### Adım 1: Kodu Güncelle
```csharp
// UpdateManager.cs içinde
private const string CURRENT_VERSION = "9.1"; // Versiyon numarasını artır
```

#### Adım 2: version.json Güncelle
```json
{
  "version": "9.1",
  "release_date": "2025-11-20",
  "download_url": "https://github.com/KULLANICI_ADIN/gemini-asistan/releases/download/v9.1/GeminiAsistan.exe",
  "features": [
    "Yeni özellik 1",
    "Yeni özellik 2"
  ],
  "bug_fixes": [
    "Bug fix 1",
    "Bug fix 2"
  ],
  "is_critical": false
}
```

#### Adım 3: Build
```bash
BUILD_VE_KORUMA.bat
```

#### Adım 4: ConfuserEx Uygula
```
1. ConfuserEx'i aç
2. EXE'yi yükle
3. Protect
```

---

### 2. GitHub'a Yükleme

#### Yöntem A: Manuel (Kolay)

1. **GitHub'da Release Oluştur**
   ```
   https://github.com/KULLANICI_ADIN/gemini-asistan/releases/new
   ```

2. **Tag Oluştur**
   ```
   Tag: v9.1
   Title: Gemini Asistan v9.1
   ```

3. **Açıklama Yaz**
   ```markdown
   ## Yenilikler
   - Yeni özellik 1
   - Yeni özellik 2
   
   ## Düzeltmeler
   - Bug fix 1
   - Bug fix 2
   ```

4. **EXE'yi Yükle**
   ```
   Confused\GeminiAsistan.exe
   ```

5. **Publish Release**

6. **version.json'u Güncelle**
   ```bash
   git add version.json
   git commit -m "Update to v9.1"
   git push
   ```

#### Yöntem B: Otomatik (GitHub Actions)

1. **Tag Oluştur ve Push Et**
   ```bash
   git tag v9.1
   git push origin v9.1
   ```

2. **GitHub Actions Otomatik Çalışır**
   - Build yapar
   - Release oluşturur
   - EXE'yi yükler

3. **version.json'u Güncelle**
   ```bash
   git add version.json
   git commit -m "Update to v9.1"
   git push
   ```

---

### 3. Kendi Sunucu Kullanma

GitHub yerine kendi sunucunu kullanabilirsin:

#### Adım 1: Sunucuya Yükle
```bash
# FTP veya SSH ile
scp GeminiAsistan.exe user@sunucu.com:/var/www/downloads/
scp version.json user@sunucu.com:/var/www/api/
```

#### Adım 2: UpdateManager.cs'i Güncelle
```csharp
private const string UPDATE_CHECK_URL = "https://sunucu.com/api/version.json";
private const string DOWNLOAD_URL = "https://sunucu.com/downloads/GeminiAsistan.exe";
```

#### Adım 3: CORS Ayarla (Nginx)
```nginx
location /api {
    add_header Access-Control-Allow-Origin *;
}
```

---

## 📊 GÜNCELLEME AKIŞI

```
[Kullanıcı] Program Açar
     ↓
[Program] Son kontrol 24 saat önce mi?
     ↓ Evet
[Program] version.json'u kontrol et
     ↓
[Sunucu] version.json döndür
     ↓
[Program] Versiyon karşılaştır
     ↓ Yeni versiyon var
[Program] Kullanıcıya bildir
     ↓
[Kullanıcı] "güncelle" yazar
     ↓
[Program] Yeni özellikleri göster
     ↓
[Kullanıcı] Onaylar (E)
     ↓
[Program] EXE'yi indir
     ↓
[Program] Eski versiyonu yedekle
     ↓
[Program] Yeni versiyonu kur
     ↓
[Program] Kendini yeniden başlat
     ↓
[Kullanıcı] Güncel versiyonu kullanır
```

---

## 🔒 GÜVENLİK

### 1. HTTPS Kullan
```csharp
private const string UPDATE_CHECK_URL = "https://..."; // HTTP değil!
```

### 2. Checksum Kontrolü (Opsiyonel)
```csharp
// version.json'a ekle
{
  "version": "9.1",
  "checksum": "SHA256_HASH_BURAYA"
}

// İndirdikten sonra kontrol et
string downloadedHash = CalculateSHA256(tempFile);
if (downloadedHash != updateInfo.Checksum)
{
    throw new Exception("Dosya bütünlüğü bozuk!");
}
```

### 3. Kod İmzalama
```bash
# İndirilen EXE'yi imzala
signtool verify /pa GeminiAsistan.exe
```

---

## ⚠️ SORUN GİDERME

### Sorun: Güncelleme kontrolü çalışmıyor
**Çözüm:**
1. İnternet bağlantısını kontrol et
2. UPDATE_CHECK_URL'i kontrol et
3. version.json'un erişilebilir olduğunu kontrol et

### Sorun: İndirme başarısız
**Çözüm:**
1. DOWNLOAD_URL'i kontrol et
2. Dosya boyutunu kontrol et
3. Firewall/Antivirus kontrol et

### Sorun: Güncelleme sonrası program açılmıyor
**Çözüm:**
1. .backup dosyasını kullan
2. Manuel olarak eski versiyonu geri yükle
3. Yeni versiyonu test et

---

## 📝 version.json ŞABLONU

```json
{
  "version": "9.1",
  "release_date": "2025-11-20",
  "download_url": "https://github.com/USER/repo/releases/download/v9.1/GeminiAsistan.exe",
  "features": [
    "Yeni özellik 1",
    "Yeni özellik 2",
    "Yeni özellik 3"
  ],
  "bug_fixes": [
    "Bug fix 1",
    "Bug fix 2"
  ],
  "is_critical": false,
  "min_version": "9.0",
  "changelog_url": "https://github.com/USER/repo/releases/tag/v9.1"
}
```

---

## 🎯 EN İYİ PRATİKLER

### 1. Versiyon Numaralandırma
```
Major.Minor.Patch
9.0.0 → İlk release
9.1.0 → Yeni özellikler
9.1.1 → Bug fix
10.0.0 → Büyük değişiklikler
```

### 2. Changelog Tutma
```markdown
# Changelog

## [9.1.0] - 2025-11-20
### Eklenenler
- Yeni özellik 1
- Yeni özellik 2

### Düzeltilenler
- Bug fix 1
- Bug fix 2

### Değişenler
- Performans iyileştirmesi
```

### 3. Test Etme
```
1. Eski versiyonu çalıştır
2. "güncelle" yaz
3. Güncellemeyi onayla
4. Yeni versiyonun çalıştığını kontrol et
5. Özellikleri test et
```

---

## 🚀 HIZLI BAŞLANGIÇ

### Yeni Versiyon Yayınlama (5 Dakika)

```bash
# 1. Versiyon numarasını artır
# UpdateManager.cs: CURRENT_VERSION = "9.1"

# 2. Build
BUILD_VE_KORUMA.bat

# 3. ConfuserEx uygula

# 4. GitHub'da release oluştur
# https://github.com/USER/repo/releases/new

# 5. EXE'yi yükle

# 6. version.json güncelle
git add version.json
git commit -m "Update to v9.1"
git push

# 7. Test et
# Program aç → "güncelle" yaz → Kontrol et
```

---

## 📞 DESTEK

Sorun mu yaşıyorsun?
1. GÜNCELLEME_SİSTEMİ.md'yi oku
2. version.json'u kontrol et
3. URL'leri kontrol et

---

## 🎉 SONUÇ

Artık kullanıcılar tek tıkla güncelleyebilir! 🚀

**Avantajlar:**
- ✅ Kullanıcı dostu
- ✅ Otomatik kontrol
- ✅ Güvenli güncelleme
- ✅ Geri dönülebilir
- ✅ Kolay yönetim

**Başarılar! 🎊**
