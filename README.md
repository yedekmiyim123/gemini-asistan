# Gemini Yapay Zeka Asistanı

Bu C# konsol uygulaması Google Gemini API'yi kullanarak yapay zeka asistanı sağlar.

## Özellikler

- ✅ Konuşma geçmişini hatırlar
- ✅ Bağlam farkındalığı ile cevap verir
- ✅ Hızlı ve yerel çalışır
- ✅ Türkçe karakter desteği
- ✅ Kelime tahminleri ve akıllı yanıtlar

## Kurulum

1. .NET 8.0 SDK'nın yüklü olduğundan emin olun
2. Projeyi derleyin:
```bash
dotnet restore
dotnet build
```

## Çalıştırma

```bash
dotnet run
```

## Kullanım

- Herhangi bir soru veya komut yazın
- `temizle` - Konuşma geçmişini sıfırlar
- `çıkış` veya `exit` - Programdan çıkar

## Örnek Kullanım

```
Sen: Merhaba, nasılsın?
Asistan: Merhaba! Ben bir yapay zeka asistanıyım...

Sen: Bana Python hakkında bilgi ver
Asistan: Python, yüksek seviyeli bir programlama dilidir...
```

API anahtarı kodda tanımlıdır ve güvenli bir şekilde saklanmalıdır.
