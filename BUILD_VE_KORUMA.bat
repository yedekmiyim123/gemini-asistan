@echo off
chcp 65001 >nul
echo ╔══════════════════════════════════════════════════════════════╗
echo ║          GEMİNİ ASİSTAN - BUILD VE KORUMA                    ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

echo [1/6] Proje temizleniyor...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo ✓ Temizleme tamamlandı
echo.

echo [2/6] Proje derleniyor (Release mode)...
dotnet build -c Release
if errorlevel 1 (
    echo ❌ Derleme başarısız!
    pause
    exit /b 1
)
echo ✓ Derleme tamamlandı
echo.

echo [3/6] EXE dosyası oluşturuluyor...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
if errorlevel 1 (
    echo ❌ Publish başarısız!
    pause
    exit /b 1
)
echo ✓ EXE oluşturuldu
echo.

echo [4/6] Gereksiz dosyalar temizleniyor...
cd bin\Release\net6.0\win-x64\publish
del *.pdb 2>nul
del *.xml 2>nul
cd ..\..\..\..\..\
echo ✓ Temizlik tamamlandı
echo.

echo [5/6] ConfuserEx ile koruma uygulanıyor...
echo.
echo ⚠️ MANUEL ADIM:
echo 1. ConfuserEx'i aç (https://github.com/mkaring/ConfuserEx)
echo 2. bin\Release\net6.0\win-x64\publish\GeminiAsistan.exe dosyasını sürükle
echo 3. Ayarlar:
echo    - Name Mangling: ✓
echo    - Control Flow: ✓
echo    - String Encryption: ✓
echo    - Anti Debug: ✓
echo    - Anti Dump: ✓
echo    - Anti Tamper: ✓
echo 4. "Protect" butonuna bas
echo 5. Korumalı dosya "Confused" klasöründe olacak
echo.
echo Devam etmek için bir tuşa bas...
pause >nul
echo.

echo [6/6] Son kontroller...
echo.
echo ✓ Build tamamlandı!
echo.
echo 📁 Dosya konumu:
echo    bin\Release\net6.0\win-x64\publish\GeminiAsistan.exe
echo.
echo 🔒 Koruma adımları:
echo    1. ConfuserEx ile obfuscate et
echo    2. UPX ile sıkıştır (opsiyonel)
echo    3. Installer oluştur (NSIS)
echo.
echo 📦 Dağıtım öncesi kontrol:
echo    [ ] API anahtarı şifreli mi?
echo    [ ] EncryptApiKey.cs silindi mi?
echo    [ ] SecurityHelper.cs XOR_KEY değiştirildi mi?
echo    [ ] ConfuserEx uygulandı mı?
echo    [ ] Test edildi mi?
echo    [ ] Virüs taraması yapıldı mı?
echo.
echo Devam etmek için bir tuşa bas...
pause
