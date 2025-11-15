using System;
using System.Text;

/// <summary>
/// API anahtarını şifrelemek için kullan
/// Bu dosyayı sadece geliştirme sırasında kullan, dağıtma!
/// NOT: Bu dosyayı çalıştırmak için Program.cs'deki Main'i geçici olarak kapat
/// </summary>
class EncryptApiKey
{
    // Main metodunu geçici olarak kapalı tut (build hatası olmasın)
    // Kullanmak için bu satırı aç, Program.cs'deki Main'i kapat
    static void MainDisabled(string[] args)
    {
        Console.WriteLine("=== API ANAHTAR ŞİFRELEME ARACI ===\n");

        // GERÇEK API ANAHTARINI BURAYA YAZ
        string apiKey = "BURAYA_API_ANAHTARINI_YAZ";

        Console.WriteLine("Orijinal API Anahtarı:");
        Console.WriteLine(apiKey);
        Console.WriteLine();

        // XOR ile şifrele
        string encryptedXOR = EncryptXOR(apiKey);
        Console.WriteLine("XOR Şifreli (SecurityHelper'da kullan):");
        Console.WriteLine(encryptedXOR);
        Console.WriteLine();

        // Test: Şifreyi çöz
        string decryptedXOR = DecryptXOR(encryptedXOR);
        Console.WriteLine("XOR Çözülmüş (Test):");
        Console.WriteLine(decryptedXOR);
        Console.WriteLine();

        // Doğrulama
        if (apiKey == decryptedXOR)
        {
            Console.WriteLine("✓ Şifreleme başarılı!");
            Console.WriteLine("\nŞimdi SecurityHelper.cs dosyasında şu satırı güncelle:");
            Console.WriteLine($"string encrypted = \"{encryptedXOR}\";");
        }
        else
        {
            Console.WriteLine("❌ Şifreleme başarısız!");
        }

        Console.WriteLine("\n\nÖNEMLİ UYARILAR:");
        Console.WriteLine("1. Bu dosyayı (EncryptApiKey.cs) dağıtma!");
        Console.WriteLine("2. SecurityHelper.cs'deki XOR_KEY'i değiştir!");
        Console.WriteLine("3. Şifreli anahtarı SecurityHelper.cs'e kopyala!");
        Console.WriteLine("4. Bu dosyayı sil veya gizle!");

        Console.WriteLine("\n\nDevam etmek için bir tuşa bas...");
        Console.ReadKey();
    }

    private static readonly byte[] XOR_KEY = { 0x4B, 0x65, 0x79, 0x31, 0x32, 0x33, 0x34, 0x35 };

    static string EncryptXOR(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= XOR_KEY[i % XOR_KEY.Length];
        }
        
        return Convert.ToBase64String(data);
    }

    static string DecryptXOR(string encrypted)
    {
        byte[] data = Convert.FromBase64String(encrypted);
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= XOR_KEY[i % XOR_KEY.Length];
        }
        
        return Encoding.UTF8.GetString(data);
    }
}
