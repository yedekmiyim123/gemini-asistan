using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// Otomatik güncelleme yöneticisi
/// </summary>
public class UpdateManager
{
    // version.json konumu (GitHub'da - sadece bu dosya repo'da)
    private const string UPDATE_CHECK_URL = "https://raw.githubusercontent.com/yedekmiyim123/gemini-asistan/main/version.json";
    
    // NOT: DOWNLOAD_URL artık version.json'dan okunuyor!
    // EXE'yi GitHub Releases, Google Drive, Dropbox veya kendi sunucuna yükle
    // version.json'da download_url'i güncelle
    
    // Mevcut versiyon
    private const string CURRENT_VERSION = "9.6.0";
    
    /// <summary>
    /// Güncelleme kontrolü yap
    /// </summary>
    public static async Task<UpdateInfo> CheckForUpdates()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                
                string json = await client.GetStringAsync(UPDATE_CHECK_URL);
                var updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(json);
                
                if (updateInfo != null && IsNewerVersion(updateInfo.Version))
                {
                    updateInfo.IsUpdateAvailable = true;
                    return updateInfo;
                }
                
                return new UpdateInfo { IsUpdateAvailable = false };
            }
        }
        catch
        {
            // İnternet yok veya sunucu erişilemiyor
            return new UpdateInfo { IsUpdateAvailable = false };
        }
    }
    
    /// <summary>
    /// Versiyon karşılaştırma
    /// </summary>
    private static bool IsNewerVersion(string newVersion)
    {
        try
        {
            var current = Version.Parse(CURRENT_VERSION);
            var latest = Version.Parse(newVersion);
            return latest > current;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Güncellemeyi indir ve yükle
    /// </summary>
    public static async Task<bool> DownloadAndInstallUpdate(UpdateInfo updateInfo)
    {
        try
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  GÜNCELLEME İNDİRİLİYOR                      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
            
            string tempFile = Path.Combine(Path.GetTempPath(), "GeminiAsistan_Update.exe");
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string backupExe = currentExe + ".backup";
            
            // İndir
            Console.WriteLine("📥 İndiriliyor...");
            
            // Download URL'i version.json'dan al
            string downloadUrl = updateInfo.DownloadUrl;
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Console.WriteLine("❌ İndirme linki bulunamadı!");
                return false;
            }
            
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                
                var response = await client.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();
                
                byte[] data = await response.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(tempFile, data);
            }
            
            Console.WriteLine("✓ İndirme tamamlandı!");
            Console.WriteLine("\n📦 Güncelleme yükleniyor...");
            
            // Batch script oluştur (kendini güncellemek için)
            string batchScript = $@"@echo off
chcp 65001 >nul
timeout /t 2 /nobreak >nul
echo Eski versiyon yedekleniyor...
if exist ""{currentExe}"" (
    if exist ""{backupExe}"" del ""{backupExe}""
    move /y ""{currentExe}"" ""{backupExe}""
)
echo Yeni versiyon kuruluyor...
move /y ""{tempFile}"" ""{currentExe}""
echo Güncelleme tamamlandı!
echo Program yeniden başlatılıyor...
timeout /t 1 /nobreak >nul
start """" ""{currentExe}""
timeout /t 2 /nobreak >nul
del ""%~f0""
";
            
            string batchFile = Path.Combine(Path.GetTempPath(), "update.bat");
            File.WriteAllText(batchFile, batchScript, Encoding.UTF8);
            
            // Batch'i çalıştır ve programı kapat
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{batchFile}\"",
                CreateNoWindow = false,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });
            
            Console.WriteLine("✓ Güncelleme başlatıldı!");
            Console.WriteLine("Program yeniden başlatılacak...\n");
            Console.WriteLine("Lütfen bekleyin...");
            
            await Task.Delay(1000);
            Environment.Exit(0);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Güncelleme hatası: {ex.Message}");
            Console.WriteLine("Manuel güncelleme için: https://github.com/KULLANICI_ADIN/gemini-asistan/releases\n");
            return false;
        }
    }
    
    /// <summary>
    /// Güncelleme bilgilerini göster
    /// </summary>
    public static void ShowUpdateInfo(UpdateInfo updateInfo)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  YENİ GÜNCELLEME MEVCUT! 🎉                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
        
        Console.WriteLine($"📌 Mevcut Versiyon: {CURRENT_VERSION}");
        Console.WriteLine($"🆕 Yeni Versiyon: {updateInfo.Version}");
        Console.WriteLine($"📅 Tarih: {updateInfo.ReleaseDate}");
        Console.WriteLine();
        
        Console.WriteLine("✨ Yenilikler:");
        if (updateInfo.Features != null)
        {
            foreach (var feature in updateInfo.Features)
            {
                Console.WriteLine($"  • {feature}");
            }
        }
        Console.WriteLine();
        
        Console.WriteLine("🔧 Düzeltmeler:");
        if (updateInfo.BugFixes != null)
        {
            foreach (var fix in updateInfo.BugFixes)
            {
                Console.WriteLine($"  • {fix}");
            }
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Otomatik güncelleme kontrolü (arka planda)
    /// </summary>
    public static async Task AutoCheckForUpdates()
    {
        try
        {
            // Son kontrol zamanını oku
            string lastCheckFile = "last_update_check.txt";
            DateTime lastCheck = DateTime.MinValue;
            
            if (File.Exists(lastCheckFile))
            {
                string lastCheckStr = File.ReadAllText(lastCheckFile);
                DateTime.TryParse(lastCheckStr, out lastCheck);
            }
            
            // 24 saatten eski ise kontrol et
            if ((DateTime.Now - lastCheck).TotalHours < 24)
            {
                return;
            }
            
            // Güncelleme kontrolü
            var updateInfo = await CheckForUpdates();
            
            if (updateInfo.IsUpdateAvailable)
            {
                Console.WriteLine("\n🔔 Yeni güncelleme mevcut!");
                Console.WriteLine($"   Versiyon {updateInfo.Version} yayınlandı.");
                Console.WriteLine("   'güncelle' yazarak güncelleyebilirsiniz.\n");
            }
            
            // Son kontrol zamanını kaydet
            File.WriteAllText(lastCheckFile, DateTime.Now.ToString());
        }
        catch
        {
            // Sessizce başarısız ol
        }
    }
    
    /// <summary>
    /// Manuel güncelleme komutu
    /// </summary>
    public static async Task ManualUpdate()
    {
        Console.WriteLine("\n🔍 Güncelleme kontrol ediliyor...\n");
        
        var updateInfo = await CheckForUpdates();
        
        if (!updateInfo.IsUpdateAvailable)
        {
            Console.WriteLine("✓ Zaten en son versiyonu kullanıyorsunuz!");
            Console.WriteLine($"  Mevcut Versiyon: {CURRENT_VERSION}\n");
            return;
        }
        
        ShowUpdateInfo(updateInfo);
        
        Console.Write("Güncellemek istiyor musunuz? (E/H): ");
        string response = Console.ReadLine()?.ToLower() ?? "";
        
        if (response == "e" || response == "evet" || response == "yes")
        {
            await DownloadAndInstallUpdate(updateInfo);
        }
        else
        {
            Console.WriteLine("\nGüncelleme iptal edildi.\n");
        }
    }
    
    /// <summary>
    /// Yedek dosyayı temizle
    /// </summary>
    public static void CleanupBackup()
    {
        try
        {
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string backupExe = currentExe + ".backup";
            
            if (File.Exists(backupExe))
            {
                File.Delete(backupExe);
            }
        }
        catch
        {
            // Sessizce başarısız ol
        }
    }
}

/// <summary>
/// Güncelleme bilgisi
/// </summary>
public class UpdateInfo
{
    [JsonProperty("version")]
    public string Version { get; set; } = "";
    
    [JsonProperty("release_date")]
    public string ReleaseDate { get; set; } = "";
    
    [JsonProperty("download_url")]
    public string DownloadUrl { get; set; } = "";
    
    [JsonProperty("features")]
    public string[] Features { get; set; } = Array.Empty<string>();
    
    [JsonProperty("bug_fixes")]
    public string[] BugFixes { get; set; } = Array.Empty<string>();
    
    [JsonProperty("is_critical")]
    public bool IsCritical { get; set; } = false;
    
    [JsonIgnore]
    public bool IsUpdateAvailable { get; set; } = false;
}
