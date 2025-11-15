using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Güvenlik ve şifreleme yardımcı sınıfı
/// </summary>
public static class SecurityHelper
{
    // XOR anahtarı (değiştir!)
    private static readonly byte[] XOR_KEY = { 0x4B, 0x65, 0x79, 0x31, 0x32, 0x33, 0x34, 0x35 };
    
    // AES anahtarı (değiştir!)
    private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("MySecretKey12345"); // 16 byte
    private static readonly byte[] AES_IV = Encoding.UTF8.GetBytes("MySecretIV123456"); // 16 byte

    /// <summary>
    /// API anahtarını şifreli olarak sakla ve çöz
    /// </summary>
    public static string GetApiKey()
    {
        // Şifreli API anahtarı (gerçek anahtarınızı şifreleyin)
        string encrypted = "CiwDUGFKd3l8ECFgWAdFbXMhSENgV1JdJjwSQ3ZkXgUvDj1VC15T";
        
        try
        {
            string key = DecryptXOR(encrypted);
            // Yeni satır ve boşluk karakterlerini temizle
            return key?.Trim().Replace("\r", "").Replace("\n", "") ?? "";
        }
        catch
        {
            // Hata durumunda çevre değişkeninden dene
            string key = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
            // Yeni satır ve boşluk karakterlerini temizle
            return key?.Trim().Replace("\r", "").Replace("\n", "") ?? "";
        }
    }

    /// <summary>
    /// XOR şifreleme/çözme (basit ama etkili)
    /// </summary>
    public static string EncryptXOR(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= XOR_KEY[i % XOR_KEY.Length];
        }
        
        return Convert.ToBase64String(data);
    }

    /// <summary>
    /// XOR şifre çözme
    /// </summary>
    public static string DecryptXOR(string encrypted)
    {
        byte[] data = Convert.FromBase64String(encrypted);
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= XOR_KEY[i % XOR_KEY.Length];
        }
        
        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    /// AES şifreleme (daha güçlü)
    /// </summary>
    public static string EncryptAES(string text)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = AES_KEY;
            aes.IV = AES_IV;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(text);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }

    /// <summary>
    /// AES şifre çözme
    /// </summary>
    public static string DecryptAES(string encrypted)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = AES_KEY;
            aes.IV = AES_IV;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(encrypted)))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Anti-Debug: Debugger kontrolü
    /// </summary>
    public static bool IsDebuggerAttached()
    {
        return Debugger.IsAttached;
    }

    /// <summary>
    /// Anti-Decompiler: Bilinen decompiler'ları kontrol et
    /// </summary>
    public static bool IsDecompilerRunning()
    {
        string[] decompilers = 
        {
            "dnspy", "ilspy", "dotpeek", "reflector", 
            "justdecompile", "ida", "ollydbg", "x64dbg"
        };

        try
        {
            var processes = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLower())
                .ToList();

            return decompilers.Any(d => processes.Any(p => p.Contains(d)));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Anti-VM: Sanal makine kontrolü
    /// </summary>
    public static bool IsRunningInVM()
    {
        try
        {
            // VMware, VirtualBox, Hyper-V kontrolü
            string[] vmIndicators = 
            {
                "vmware", "virtualbox", "vbox", "qemu", 
                "virtual", "hyper-v", "parallels"
            };

            var processes = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLower())
                .ToList();

            return vmIndicators.Any(vm => processes.Any(p => p.Contains(vm)));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Güvenlik kontrollerini çalıştır
    /// </summary>
    public static bool RunSecurityChecks()
    {
        // Debugger kontrolü
        if (IsDebuggerAttached())
        {
            Console.WriteLine("⚠️ Debugger tespit edildi!");
            return false;
        }

        // Decompiler kontrolü
        if (IsDecompilerRunning())
        {
            Console.WriteLine("⚠️ Decompiler tespit edildi!");
            return false;
        }

        // VM kontrolü (opsiyonel - bazı kullanıcılar VM'de çalıştırabilir)
        // if (IsRunningInVM())
        // {
        //     Console.WriteLine("⚠️ Sanal makine tespit edildi!");
        //     return false;
        // }

        return true;
    }

    /// <summary>
    /// Lisans kontrolü (basit örnek)
    /// </summary>
    public static bool CheckLicense()
    {
        try
        {
            string licenseFile = "license.dat";
            
            if (!File.Exists(licenseFile))
            {
                return false;
            }

            string encrypted = File.ReadAllText(licenseFile);
            string license = DecryptAES(encrypted);
            
            // Lisans formatı: "USERNAME:EXPIRY_DATE:HASH"
            var parts = license.Split(':');
            
            if (parts.Length != 3)
            {
                return false;
            }

            DateTime expiryDate = DateTime.Parse(parts[1]);
            
            if (DateTime.Now > expiryDate)
            {
                Console.WriteLine("⚠️ Lisansınız süresi dolmuş!");
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lisans oluştur (sadece geliştirici için)
    /// </summary>
    public static void CreateLicense(string username, DateTime expiryDate)
    {
        string hash = GenerateHash(username + expiryDate.ToString());
        string license = $"{username}:{expiryDate:yyyy-MM-dd}:{hash}";
        string encrypted = EncryptAES(license);
        
        File.WriteAllText("license.dat", encrypted);
        Console.WriteLine($"✓ Lisans oluşturuldu: {username} - {expiryDate:yyyy-MM-dd}");
    }

    /// <summary>
    /// Hash oluştur
    /// </summary>
    private static string GenerateHash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes).Substring(0, 16);
        }
    }

    /// <summary>
    /// Makine ID'si al (donanım bazlı)
    /// </summary>
    public static string GetMachineId()
    {
        try
        {
            string cpuId = GetCpuId();
            string diskId = GetDiskId();
            string combined = cpuId + diskId;
            
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(bytes).Substring(0, 16);
            }
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    private static string GetCpuId()
    {
        try
        {
            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU";
        }
        catch
        {
            return "CPU";
        }
    }

    private static string GetDiskId()
    {
        try
        {
            DriveInfo drive = DriveInfo.GetDrives().FirstOrDefault();
            return drive?.Name ?? "DISK";
        }
        catch
        {
            return "DISK";
        }
    }
}
