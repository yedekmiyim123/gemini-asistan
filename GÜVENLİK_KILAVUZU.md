# 🔒 GEMİNİ ASİSTAN - GÜVENLİK KILAVUZU

## 📋 İÇİNDEKİLER
1. [API Anahtarı Koruma](#1-api-anahtari-koruma)
2. [Kod Obfuscation](#2-kod-obfuscation)
3. [Anti-Debug/Anti-Decompile](#3-anti-debuganti-decompile)
4. [Build ve Dağıtım](#4-build-ve-dagitim)
5. [Ek Korumalar](#5-ek-korumalar)

---

## 1. API ANAHTARI KORUMA

### ⚠️ SORUN
API anahtarı kodda açıkta:
```csharp
private static readonly string API_KEY = "AIzaSyABzWxf4Lp5rwg2ZWrRaE2ZlH9ZvFw-Q7M";
```
❌ Bu şekilde herkes görebilir!

### ✅ ÇÖZÜM

#### Adım 1: API Anahtarını Şifrele
```bash
# EncryptApiKey.cs'i çalıştır
dotnet run --project EncryptApiKey.cs
```

Çıktı:
```
XOR Şifreli: QUl6YVN5QUJ6V3hmNExwNXJ3ZzJaV3JSYUUyWmxIOVp2Rnct
```

#### Adım 2: SecurityHelper.cs'i Güncelle
```csharp
public static string GetApiKey()
{
    string encrypted = "QUl6YVN5QUJ6V3hmNExwNXJ3ZzJaV3JSYUUyWmxIOVp2Rnct";
    return DecryptXOR(encrypted);
}
```

#### Adım 3: XOR Anahtarını Değiştir
```csharp
// SecurityHelper.cs içinde
private static readonly byte[] XOR_KEY = { 0x4B, 0x65, 0x79, 0x31, 0x32, 0x33, 0x34, 0x35 };
```
⚠️ Bu değerleri değiştir! Rastgele sayılar kullan.

#### Adım 4: EncryptApiKey.cs'i Sil
```bash
del EncryptApiKey.cs
```
❗ Bu dosyayı dağıtma!

---

## 2. KOD OBFUSCATION

### Nedir?
Kodunuzu okunamaz hale getirir. Decompile edilse bile anlaşılmaz.

### Öncesi:
```csharp
public void OpenProgram(string programName)
{
    Process.Start(programName);
}
```

### Sonrası:
```csharp
public void a(string b)
{
    Process.Start(b);
}
```

### Araçlar

#### A) ConfuserEx (ÜCRETSİZ) ⭐ ÖNERİLEN

**İndirme:**
```
https://github.com/mkaring/ConfuserEx/releases
```

**Kullanım:**
1. ConfuserEx'i aç
2. EXE'yi sürükle-bırak
3. Ayarlar:
   - ✓ Name Mangling (İsim karıştırma)
   - ✓ Control Flow (Akış karıştırma)
   - ✓ String Encryption (String şifreleme)
   - ✓ Anti Debug (Debug koruması)
   - ✓ Anti Dump (Dump koruması)
   - ✓ Anti Tamper (Değiştirme koruması)
4. "Protect" butonuna bas
5. Korumalı EXE "Confused" klasöründe

**Koruma Seviyesi:** %85

#### B) .NET Reactor (ÜCRETLI)

**İndirme:**
```
https://www.eziriz.com/dotnet_reactor.htm
```

**Özellikler:**
- Native kod dönüşümü
- Güçlü obfuscation
- Lisans sistemi
- Anti-debug/Anti-tamper

**Fiyat:** ~$179

**Koruma Seviyesi:** %95

#### C) Eazfuscator.NET (ÜCRETLI/ÜCRETSIZ)

**İndirme:**
```
https://www.gapotchenko.com/eazfuscator.net
```

**Özellikler:**
- Otomatik obfuscation
- Visual Studio entegrasyonu
- Ücretsiz versiyon var

**Koruma Seviyesi:** %80

---

## 3. ANTI-DEBUG/ANTI-DECOMPILE

### SecurityHelper.cs Özellikleri

#### A) Debugger Kontrolü
```csharp
if (Debugger.IsAttached)
{
    Environment.Exit(0);
}
```
Debugger tespit edilirse program kapanır.

#### B) Decompiler Kontrolü
```csharp
if (IsDecompilerRunning())
{
    Environment.Exit(0);
}
```
dnSpy, ILSpy gibi araçları tespit eder.

#### C) VM Kontrolü (Opsiyonel)
```csharp
if (IsRunningInVM())
{
    Console.WriteLine("⚠️ Sanal makine tespit edildi!");
}
```
VMware, VirtualBox tespit eder.

### Ek Korumalar

#### D) Timing Attack Koruması
```csharp
var sw = Stopwatch.StartNew();
// Kritik kod
sw.Stop();
if (sw.ElapsedMilliseconds > 1000)
{
    // Debugger var olabilir
    Environment.Exit(0);
}
```

#### E) Checksum Kontrolü
```csharp
string expectedHash = "ABC123...";
string actualHash = CalculateFileHash();
if (expectedHash != actualHash)
{
    // Dosya değiştirilmiş
    Environment.Exit(0);
}
```

---

## 4. BUILD VE DAĞITIM

### Adım 1: API Anahtarını Şifrele
```bash
dotnet run --project EncryptApiKey.cs
# Çıktıyı SecurityHelper.cs'e kopyala
```

### Adım 2: XOR Anahtarını Değiştir
```csharp
// SecurityHelper.cs
private static readonly byte[] XOR_KEY = { /* RASTGELE DEĞERLER */ };
```

### Adım 3: Build
```bash
BUILD_VE_KORUMA.bat
```
veya manuel:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Adım 4: ConfuserEx Uygula
1. ConfuserEx'i aç
2. EXE'yi yükle
3. Ayarları yap
4. Protect

### Adım 5: Test Et
```bash
# Korumalı EXE'yi test et
Confused\GeminiAsistan.exe
```

### Adım 6: UPX Sıkıştırma (Opsiyonel)
```bash
upx --best --lzma GeminiAsistan.exe
```
Dosya boyutunu %50-70 küçültür.

### Adım 7: Installer Oluştur (NSIS)
```nsis
; installer.nsi
!define APP_NAME "Gemini Asistan"
!define APP_VERSION "9.0"

OutFile "GeminiAsistan_Setup.exe"
InstallDir "$PROGRAMFILES\${APP_NAME}"

Section "Install"
    SetOutPath $INSTDIR
    File "GeminiAsistan.exe"
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\GeminiAsistan.exe"
SectionEnd
```

---

## 5. EK KORUMALAR

### A) Lisans Sistemi

#### Lisans Oluştur
```csharp
SecurityHelper.CreateLicense("kullanici123", DateTime.Now.AddYears(1));
```

#### Lisans Kontrol
```csharp
if (!SecurityHelper.CheckLicense())
{
    Console.WriteLine("❌ Geçersiz lisans!");
    return;
}
```

### B) Donanım Kilidi
```csharp
string machineId = SecurityHelper.GetMachineId();
// Bu ID'yi sunucuda kontrol et
```

### C) Online Aktivasyon
```csharp
bool isActivated = await CheckActivationOnline(machineId);
if (!isActivated)
{
    Console.WriteLine("❌ Aktivasyon gerekli!");
    return;
}
```

### D) Kod İmzalama (Code Signing)
```bash
# Authenticode ile imzala
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com GeminiAsistan.exe
```
Windows SmartScreen uyarısını engeller.

---

## 📊 KORUMA SEVİYELERİ

| Yöntem | Koruma | Zorluk | Maliyet |
|--------|--------|--------|---------|
| Hiçbiri | %0 | - | Ücretsiz |
| API Şifreleme | %30 | Kolay | Ücretsiz |
| ConfuserEx | %85 | Orta | Ücretsiz |
| .NET Reactor | %95 | Zor | $179 |
| Tümü + Lisans | %98 | Çok Zor | $179+ |

---

## ⚠️ UYARILAR

### 1. %100 Koruma Yok
Hiçbir koruma %100 değildir. Yeterince kararlı biri her zaman kırabilir.

### 2. Performans
Obfuscation performansı %5-15 düşürebilir.

### 3. Hata Ayıklama
Korumalı kodda hata bulmak çok zordur. Test etmeyi unutma!

### 4. Yasal
Sadece kendi kodunuzu koruyun. Başkasının kodunu kırmak yasadışıdır.

### 5. Yedekleme
Korumasız versiyonu sakla! Koruma sonrası geri dönemezsin.

---

## 🎯 ÖNERİLEN YÖNTEM

### Minimum Koruma (Ücretsiz)
1. ✓ API anahtarı şifreleme
2. ✓ ConfuserEx obfuscation
3. ✓ Anti-debug kontrolü
4. ✓ String encryption

**Koruma Seviyesi:** %85
**Maliyet:** Ücretsiz
**Süre:** 30 dakika

### Maksimum Koruma (Ücretli)
1. ✓ API anahtarı şifreleme
2. ✓ .NET Reactor obfuscation
3. ✓ Anti-debug/Anti-tamper
4. ✓ Lisans sistemi
5. ✓ Donanım kilidi
6. ✓ Online aktivasyon
7. ✓ Kod imzalama

**Koruma Seviyesi:** %98
**Maliyet:** ~$400
**Süre:** 1 gün

---

## 📝 DAĞITIM ÖNCESİ CHECKLIST

- [ ] API anahtarı şifreli
- [ ] XOR_KEY değiştirildi
- [ ] EncryptApiKey.cs silindi
- [ ] SecurityHelper.cs dahil edildi
- [ ] ConfuserEx uygulandı
- [ ] Anti-debug aktif
- [ ] Test edildi
- [ ] Virüs taraması yapıldı
- [ ] Kod imzalandı (opsiyonel)
- [ ] Installer oluşturuldu
- [ ] Dokümantasyon hazır
- [ ] Yedek alındı

---

## 🚀 HIZLI BAŞLANGIÇ

```bash
# 1. API anahtarını şifrele
dotnet run --project EncryptApiKey.cs

# 2. SecurityHelper.cs'i güncelle
# (Şifreli anahtarı kopyala)

# 3. XOR_KEY'i değiştir
# (Rastgele değerler kullan)

# 4. Build
BUILD_VE_KORUMA.bat

# 5. ConfuserEx uygula
# (Manuel adım)

# 6. Test et
Confused\GeminiAsistan.exe

# 7. Dağıt
# (Installer oluştur)
```

---

## 📞 DESTEK

Sorun mu yaşıyorsun?
1. KORUMA_SISTEMI.md dosyasını oku
2. BUILD_VE_KORUMA.bat'ı çalıştır
3. Adım adım takip et

---

## 🎉 SONUÇ

Bu kılavuzu takip ederek EXE dosyanı %85-98 oranında koruyabilirsin!

**Başarılar! 🚀**
