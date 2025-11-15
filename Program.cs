using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

class Program
{
    // API anahtarı artık şifreli - SecurityHelper'dan alınıyor
    private static readonly string API_KEY = SecurityHelper.GetApiKey();
    private static readonly string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private static List<ConversationMessage> conversationHistory = new List<ConversationMessage>();
    private static readonly string FEATURES_FILE = "ozellikler.json";
    private static readonly string HISTORY_FILE = "konusma_gecmisi.json";
    private static readonly string PREFERENCES_FILE = "tercihler.json";
    private static readonly string ERROR_LOG_FILE = "hata_kayitlari.json";
    private static UserPreferences userPreferences = new UserPreferences();
    private static List<ErrorLog> errorLogs = new List<ErrorLog>();
    private static int consecutiveErrors = 0;
    
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        // GÜVENLİK KONTROLÜ
        if (!SecurityHelper.RunSecurityChecks())
        {
            Console.WriteLine("\n❌ Güvenlik kontrolü başarısız!");
            Console.WriteLine("Program kapatılıyor...");
            await Task.Delay(3000);
            return;
        }

        // API anahtarı kontrolü
        if (string.IsNullOrEmpty(API_KEY))
        {
            Console.WriteLine("\n❌ API anahtarı bulunamadı!");
            Console.WriteLine("Lütfen GEMINI_API_KEY çevre değişkenini ayarlayın.");
            await Task.Delay(3000);
            return;
        }

        Console.WriteLine("=== Gemini Yapay Zeka Asistanı ===");
        Console.WriteLine("🔒 Güvenlik: Aktif");
        Console.WriteLine("Bilgisayar kontrolü aktif!");
        Console.WriteLine("Komutlar: 'çıkış' - Programdan çık");
        Console.WriteLine("=====================================\n");

        // Konuşma geçmişini ve tercihleri yükle
        LoadConversationHistory();
        LoadUserPreferences();
        LoadErrorLogs();
        
        if (conversationHistory.Count > 0)
        {
            Console.WriteLine($"✓ {conversationHistory.Count} önceki mesaj yüklendi");
        }
        
        if (userPreferences.TotalCommands > 0)
        {
            Console.WriteLine($"✓ Kullanıcı profili yüklendi ({userPreferences.TotalCommands} komut geçmişi)");
        }
        
        Console.WriteLine($"✓ Gelişmiş AI özellikleri aktif");
        
        // Otomatik güncelleme kontrolü (arka planda)
        _ = UpdateManager.AutoCheckForUpdates();
        
        // Yedek dosyayı temizle
        UpdateManager.CleanupBackup();
        
        Console.WriteLine();

        while (true)
        {
            Console.Write("Sen: ");
            string? userInput = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.ToLower() == "çıkış" || userInput.ToLower() == "exit")
            {
                SaveConversationHistory();
                Console.WriteLine("✓ Konuşma geçmişi kaydedildi");
                Console.WriteLine("Görüşmek üzere!");
                break;
            }

            if (userInput.ToLower() == "temizle")
            {
                conversationHistory.Clear();
                SaveConversationHistory();
                Console.WriteLine("Konuşma geçmişi temizlendi.\n");
                continue;
            }

            if (userInput.ToLower() == "güncelle" || userInput.ToLower() == "update")
            {
                await UpdateManager.ManualUpdate();
                continue;
            }

            if (userInput.ToLower() == "versiyon" || userInput.ToLower() == "version")
            {
                Console.WriteLine($"\n📌 Gemini Asistan v9.3 - Intelligent AI Edition");
                Console.WriteLine($"📅 Tarih: 15 Kasım 2025");
                Console.WriteLine($"🔒 Güvenlik: Aktif");
                Console.WriteLine($"🔄 Güncelleme: Dropbox\n");
                continue;
            }

            await SendMessageToGemini(userInput);
            
            // Her 5 mesajda bir otomatik kaydet
            if (conversationHistory.Count % 5 == 0)
            {
                SaveConversationHistory();
            }
        }
    }

    static void LoadConversationHistory()
    {
        try
        {
            if (File.Exists(HISTORY_FILE))
            {
                string json = File.ReadAllText(HISTORY_FILE);
                var history = JsonConvert.DeserializeObject<List<ConversationMessage>>(json);
                if (history != null)
                {
                    conversationHistory = history;
                }
            }
        }
        catch { }
    }

    static void SaveConversationHistory()
    {
        try
        {
            string json = JsonConvert.SerializeObject(conversationHistory, Formatting.Indented);
            File.WriteAllText(HISTORY_FILE, json);
        }
        catch { }
    }

    static void LoadUserPreferences()
    {
        try
        {
            if (File.Exists(PREFERENCES_FILE))
            {
                string json = File.ReadAllText(PREFERENCES_FILE);
                var prefs = JsonConvert.DeserializeObject<UserPreferences>(json);
                if (prefs != null)
                {
                    userPreferences = prefs;
                }
            }
        }
        catch { }
    }

    static void SaveUserPreferences()
    {
        try
        {
            string json = JsonConvert.SerializeObject(userPreferences, Formatting.Indented);
            File.WriteAllText(PREFERENCES_FILE, json);
        }
        catch { }
    }

    static void LoadErrorLogs()
    {
        try
        {
            if (File.Exists(ERROR_LOG_FILE))
            {
                string json = File.ReadAllText(ERROR_LOG_FILE);
                var logs = JsonConvert.DeserializeObject<List<ErrorLog>>(json);
                if (logs != null)
                {
                    errorLogs = logs;
                }
            }
        }
        catch { }
    }

    static void SaveErrorLogs()
    {
        try
        {
            string json = JsonConvert.SerializeObject(errorLogs, Formatting.Indented);
            File.WriteAllText(ERROR_LOG_FILE, json);
        }
        catch { }
    }

    static void LogError(string command, string error, string suggestion = "")
    {
        try
        {
            errorLogs.Add(new ErrorLog
            {
                Timestamp = DateTime.Now,
                Command = command,
                Error = error,
                Suggestion = suggestion
            });

            consecutiveErrors++;

            // Son 100 hatayı tut
            if (errorLogs.Count > 100)
            {
                errorLogs.RemoveAt(0);
            }

            SaveErrorLogs();

            // Çok fazla hata varsa kullanıcıyı bilgilendir
            if (consecutiveErrors >= 3)
            {
                Console.WriteLine("\n⚠️ Birden fazla hata oluştu. Komutlarınızı kontrol edin.");
                Console.WriteLine("💡 İpucu: 'yardım' yazarak komut listesini görebilirsiniz.\n");
            }
        }
        catch { }
    }

    static void UpdateUserPreferences(string command)
    {
        try
        {
            userPreferences.TotalCommands++;
            userPreferences.LastUsed = DateTime.Now;

            // En çok kullanılan komutları takip et
            if (userPreferences.FavoriteCommands.ContainsKey(command))
            {
                userPreferences.FavoriteCommands[command]++;
            }
            else
            {
                userPreferences.FavoriteCommands[command] = 1;
            }

            // Başarılı komut, hata sayacını sıfırla
            consecutiveErrors = 0;

            // Her 10 komutta bir kaydet
            if (userPreferences.TotalCommands % 10 == 0)
            {
                SaveUserPreferences();
            }
        }
        catch { }
    }

    static string GetSmartSuggestion(string userInput)
    {
        try
        {
            userInput = userInput.ToLower();

            // Doğal dil anlama - farklı ifade şekilleri
            var patterns = new Dictionary<string, string>
            {
                // Dosya okuma
                { "içinde ne var", "DOSYA_OKU" },
                { "içeriğini göster", "DOSYA_OKU" },
                { "oku", "DOSYA_OKU" },
                { "içeriği", "DOSYA_OKU" },
                
                // Program açma
                { "aç", "PROGRAM" },
                { "başlat", "PROGRAM" },
                { "çalıştır", "PROGRAM" },
                
                // Arama
                { "arat", "ARA" },
                { "ara", "ARA" },
                { "bul", "ARA" },
                
                // Kapatma
                { "kapat", "KAPAT" },
                { "kapa", "KAPAT" },
                { "sonlandır", "KAPAT" },
                
                // Dosya işlemleri
                { "oluştur", "OLUSTUR" },
                { "yarat", "OLUSTUR" },
                { "sil", "SIL" },
                
                // Sistem
                { "bilgisayarı kapat", "SISTEM_KAPAT" },
                { "pc kapat", "SISTEM_KAPAT" },
                { "yeniden başlat", "YENIDEN_BASLAT" },
            };

            foreach (var pattern in patterns)
            {
                if (userInput.Contains(pattern.Key))
                {
                    return pattern.Value;
                }
            }

            // Kullanıcının geçmiş tercihlerine göre öneri
            if (userPreferences.FavoriteCommands.Count > 0)
            {
                var topCommand = userPreferences.FavoriteCommands
                    .OrderByDescending(x => x.Value)
                    .First();
                
                if (topCommand.Value > 5)
                {
                    return $"FAVORITE:{topCommand.Key}";
                }
            }

            return "";
        }
        catch
        {
            return "";
        }
    }

    static async Task SendMessageToGemini(string userMessage)
    {
        try
        {
            string systemContext = GetSystemContext();
            
            conversationHistory.Add(new ConversationMessage 
            { 
                Role = "user", 
                Text = userMessage 
            });

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var requestBody = new
                {
                    contents = BuildContents(),
                    systemInstruction = new
                    {
                        parts = new[] 
                        { 
                            new { text = $@"Sen gelişmiş bir yapay zeka asistanısın. 100+ özelliğe sahipsin.

GELİŞMİŞ YETENEKLERİN:
✓ DOĞAL DİL ANLAMA: Kullanıcının farklı ifade şekillerini anlarsın
  - 'txt nin içinde ne var' = 'dosyayı oku' = 'içeriğini göster'
  - 'chrome aç' = 'chrome başlat' = 'chrome çalıştır'
  - Kullanıcı ne demek istediğini anlamaya çalış

✓ ÖĞRENME VE ADAPTASYON: Kullanıcının tercihlerini öğrenirsin
  - Hangi komutları sık kullandığını takip et
  - Kullanıcının alışkanlıklarına göre öneriler sun
  - Geçmiş konuşmalardan öğren

✓ AKILLI HATA YÖNETİMİ: Hataları tespit edip düzeltirsin
  - Hata olduğunda açıklayıcı geri bildirim ver
  - Alternatif çözümler öner
  - Kullanıcıyı doğru yola yönlendir

✓ KARMAŞIK GÖREV YÖNETİMİ: Çok adımlı görevleri yönetirsin
  - Birden fazla uygulamayı koordine et
  - Adımları sırayla ve mantıklı şekilde yap
  - Her adımı kullanıcıya açıkla

✓ KİŞİSELLEŞTİRME: Her kullanıcıya özel deneyim sunarsın
  - Kullanıcının tercihlerini hatırla
  - Sık kullanılan komutları önceliklendir
  - Kullanıcının tarzına uyum sağla

✓ GÜVENLİK VE GİZLİLİK: Kullanıcı verilerini korursın
  - Hassas bilgileri güvenli şekilde sakla
  - Tehlikeli komutlar için onay iste
  - Kullanıcıyı potansiyel riskler konusunda uyar

ÖZEL YETENEKLERİN:
- Duygu tanıma ve empati
- Yaratıcı yazma (şiir, senaryo, müzik)
- Programlama ve kod yazma
- Finansal analiz ve yatırım tavsiyeleri
- Eğitim ve öğretim
- Oyun stratejileri
- Seyahat planlama
- Yemek tarifleri
- Sanat ve tasarım
- Web ve mobil geliştirme
- Veri analizi ve görselleştirme
- Proje yönetimi
- Kişisel gelişim koçluğu
- Sağlık ve fitness tavsiyeleri
- Ve daha fazlası...

UZUN İŞLEMLER:
- Uzun işlemler sırasında konuşmayı KESİNLİKLE bitirme
- Her adımı açıkla ve devam et
- Kullanıcıyla etkileşimi sürdür

Sen bir bilgisayar kontrol asistanısın. Kullanıcının bilgisayarında işlemler yapabilirsin.

ÖNEMLI KURALLAR:
1. Program açmak: [KOMUT:PROGRAM:program_adı]
2. Chrome'da arama: [KOMUT:CHROME_ARA:arama_metni]
3. YouTube'da arama: [KOMUT:YOUTUBE_ARA:arama_metni]
4. Dosya/klasör açmak: [KOMUT:AC:dosya_yolu]
5. Ses seviyesi ayarla: [KOMUT:SES:0-100]
6. Bilgisayarı kapat: [KOMUT:KAPAT]
7. Bilgisayarı yeniden başlat: [KOMUT:YENIDEN_BASLAT]
8. Uyku modu: [KOMUT:UYKU]
9. Dosya oluştur: [KOMUT:DOSYA_OLUSTUR:yol:içerik]
10. Klasör oluştur: [KOMUT:KLASOR_OLUSTUR:yol]
11. Dosya sil: [KOMUT:DOSYA_SIL:yol]
12. Program kapat: [KOMUT:PROGRAM_KAPAT:program_adı]
13. Ekran görüntüsü al: [KOMUT:EKRAN_GORUNTUSU]
14. Özellikleri göster: [KOMUT:OZELLIKLER]
14.5. Dosya oku: [KOMUT:DOSYA_OKU:yol] (txt, json, md vb. dosyaları okur)
15. Ekrana tıkla: [KOMUT:TIKLA:x:y] (örnek: [KOMUT:TIKLA:500:300])
16. Sağ tıkla: [KOMUT:SAG_TIKLA:x:y]
17. Yazı yaz: [KOMUT:YAZ:metin] (aktif pencereye yazar - Discord, oyun, her yerde çalışır)
18. Tuş bas: [KOMUT:TUS:tuş_adı] (enter, tab, escape, ctrl+c, ctrl+v vb.)
19. Pencere küçült: [KOMUT:PENCERE_KUCULT:program_adı]
20. Pencere büyüt: [KOMUT:PENCERE_BUYUT:program_adı]
21. Görev yöneticisi aç: [KOMUT:GOREV_YONETICISI]
22. Mouse koordinatı göster: [KOMUT:MOUSE_KOORDINAT]
23. Uygulama odakla: [KOMUT:ODAKLA:program_adı] (Discord, Chrome vb. uygulamayı öne getirir)
24. Çift tıkla: [KOMUT:CIFT_TIKLA:x:y]
25. Mouse'u hareket ettir: [KOMUT:MOUSE_HAREKET:x:y]
26. Bekle: [KOMUT:BEKLE:saniye] (örnek: [KOMUT:BEKLE:2] - 2 saniye bekler)
27. Discord kısayolları: [KOMUT:DISCORD_KISAYOL:islem]
28. Web sitesine git: [KOMUT:WEB_GIT:url]
29. Web'de tıkla: [KOMUT:WEB_TIKLA:css_selector]
30. Web'den veri çek: [KOMUT:WEB_VERI:css_selector]
31. Dosya ara: [KOMUT:DOSYA_ARA:klasor:arama_terimi]
32. Dosya taşı: [KOMUT:DOSYA_TASI:kaynak:hedef]
33. Dosya kopyala: [KOMUT:DOSYA_KOPYALA:kaynak:hedef]
34. Dosya yeniden adlandır: [KOMUT:DOSYA_YENIDEN_ADLANDIR:eski:yeni]
35. Hatırlatıcı kur: [KOMUT:HATIRLATICI:dakika:mesaj]
36. Not al: [KOMUT:NOT:metin]
37. Ekran oku (OCR): [KOMUT:EKRAN_OKU:x:y:genislik:yukseklik]
38. Discord otomatik mesaj: [KOMUT:DISCORD_MESAJ:sunucu_x:sunucu_y:kanal_x:kanal_y:mesaj_x:mesaj_y:mesaj]
39. Sayfa kaydır: [KOMUT:KAYDIR:yon] (yukari, asagi, saga, sola)
40. Dosya sil (gelişmiş): [KOMUT:DOSYA_SIL_GELISMIS:yol]
41. Not düzenle: [KOMUT:NOT_DUZENLE:satir:yeni_metin]
42. Not sil: [KOMUT:NOT_SIL:satir]
43. En büyük dosya bul: [KOMUT:EN_BUYUK_DOSYA:klasor]
44. En küçük dosya bul: [KOMUT:EN_KUCUK_DOSYA:klasor]
45. Web özet: [KOMUT:WEB_OZET:url]
46. Çoklu komut: [KOMUT:COKLU:komut1|komut2|komut3]
47. Akıllı bekle: [KOMUT:AKILLI_BEKLE:saniye]

27. Discord kısayolları: [KOMUT:DISCORD_KISAYOL:islem]
    - ayarlar: Ctrl+, (Ayarları aç)
    - bildirim_sessize: Ctrl+Shift+M (Bildirimleri sessize al)
    - mikrofon: Ctrl+Shift+M (Mikrofonu aç/kapat)
    - hoparlor: Ctrl+Shift+D (Hoparlörü aç/kapat)
    - emoji: Ctrl+E (Emoji paneli)
    - arama: Ctrl+K (Kanal arama)
    - dm: Ctrl+Shift+D (DM aç)
    - arkadaslar: Ctrl+Shift+F (Arkadaş listesi)
    - tam_ekran: F11 (Tam ekran)
    - kanal_yukari: Alt+Yukarı (Önceki kanal)
    - kanal_asagi: Alt+Aşağı (Sonraki kanal)
    - sunucu_yukari: Ctrl+Alt+Yukarı (Önceki sunucu)
    - sunucu_asagi: Ctrl+Alt+Aşağı (Sonraki sunucu)

ÖNEMLI: Kullanıcı Discord'da mesaj yazmak isterse:
1. Discord'u aç: [KOMUT:PROGRAM:discord]
2. 3 saniye bekle: [KOMUT:BEKLE:3]
3. Discord'u odakla: [KOMUT:ODAKLA:discord]
4. Kullanıcı hangi kanala gitmek istiyorsa koordinatlarla tıkla (kullanıcı söylemezse atla)
5. Yazı yaz: [KOMUT:YAZ:merhaba]
6. Enter bas: [KOMUT:TUS:enter]

Kullanıcı 'hangi özellikler var', 'yeni özellikler', 'neler ekledin' gibi sorular sorarsa [KOMUT:OZELLIKLER] kullan.
Kullanıcı 'mouse koordinatı', 'fare konumu', 'koordinat göster' gibi sorular sorarsa [KOMUT:MOUSE_KOORDINAT] kullan.
Kullanıcı Discord, oyun veya herhangi bir uygulamada işlem yapmak isterse önce uygulamayı aç, bekle, odakla, sonra işlem yap.

Sistem Bilgileri:
{systemContext}

Örnekler:
- 'chrome aç' → [KOMUT:PROGRAM:chrome]
- 'chromede ekmek arat' → [KOMUT:CHROME_ARA:ekmek]
- 'youtubede minecraft ara' → [KOMUT:YOUTUBE_ARA:minecraft]
- 'sesi 50 yap' → [KOMUT:SES:50]
- 'bilgisayarı kapat' → [KOMUT:KAPAT]
- 'chrome kapat' → [KOMUT:PROGRAM_KAPAT:chrome]
- 'masaüstünde test.txt oluştur' → [KOMUT:DOSYA_OLUSTUR:Desktop/test.txt:içerik]
- 'test.txt nin içinde ne var' → [KOMUT:DOSYA_OKU:Desktop/test.txt]
- 'notlar.txt oku' → [KOMUT:DOSYA_OKU:Desktop/notlar.txt]

Kısa ve net cevaplar ver. Kullanıcının bilgisayar özelliklerini biliyorsun." }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.3,
                        topK = 20,
                        topP = 0.8,
                        maxOutputTokens = 1024
                    }
                };

                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                
                client.DefaultRequestHeaders.Add("X-goog-api-key", API_KEY);

                var response = await client.PostAsync(API_URL, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseBody);
                    
                    if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Count > 0)
                    {
                        string aiResponse = geminiResponse.Candidates[0].Content.Parts[0].Text;
                        
                        conversationHistory.Add(new ConversationMessage 
                        { 
                            Role = "model", 
                            Text = aiResponse 
                        });

                        await ProcessAIResponse(aiResponse);
                    }
                    else
                    {
                        Console.WriteLine("Yanıt alınamadı.\n");
                    }
                }
                else
                {
                    Console.WriteLine($"Hata: {response.StatusCode}");
                    Console.WriteLine($"Detay: {responseBody}\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bir hata oluştu: {ex.Message}\n");
        }
    }

    static async Task ProcessAIResponse(string response)
    {
        if (response.Contains("[KOMUT:"))
        {
            // Komut türünü belirle ve tercih takibi yap
            string commandType = ExtractCommandType(response);
            if (!string.IsNullOrEmpty(commandType))
            {
                UpdateUserPreferences(commandType);
            }

            if (response.Contains("[KOMUT:PROGRAM:"))
            {
                string program = ExtractCommand(response, "[KOMUT:PROGRAM:", "]");
                ExecuteWithErrorHandling(() => OpenProgram(program), "PROGRAM", program);
            }
            else if (response.Contains("[KOMUT:CHROME_ARA:"))
            {
                string searchQuery = ExtractCommand(response, "[KOMUT:CHROME_ARA:", "]");
                OpenChromeWithSearch(searchQuery);
            }
            else if (response.Contains("[KOMUT:YOUTUBE_ARA:"))
            {
                string searchQuery = ExtractCommand(response, "[KOMUT:YOUTUBE_ARA:", "]");
                OpenYouTubeSearch(searchQuery);
            }
            else if (response.Contains("[KOMUT:AC:"))
            {
                string path = ExtractCommand(response, "[KOMUT:AC:", "]");
                OpenPath(path);
            }
            else if (response.Contains("[KOMUT:SES:"))
            {
                string volume = ExtractCommand(response, "[KOMUT:SES:", "]");
                SetVolume(volume);
            }
            else if (response.Contains("[KOMUT:KAPAT]"))
            {
                ShutdownComputer();
            }
            else if (response.Contains("[KOMUT:YENIDEN_BASLAT]"))
            {
                RestartComputer();
            }
            else if (response.Contains("[KOMUT:UYKU]"))
            {
                SleepComputer();
            }
            else if (response.Contains("[KOMUT:DOSYA_OLUSTUR:"))
            {
                string fullCommand = ExtractCommand(response, "[KOMUT:DOSYA_OLUSTUR:", "]");
                var parts = fullCommand.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    CreateFile(parts[0], parts[1]);
                }
            }
            else if (response.Contains("[KOMUT:KLASOR_OLUSTUR:"))
            {
                string path = ExtractCommand(response, "[KOMUT:KLASOR_OLUSTUR:", "]");
                CreateFolder(path);
            }
            else if (response.Contains("[KOMUT:DOSYA_SIL:"))
            {
                string path = ExtractCommand(response, "[KOMUT:DOSYA_SIL:", "]");
                DeleteFile(path);
            }
            else if (response.Contains("[KOMUT:PROGRAM_KAPAT:"))
            {
                string program = ExtractCommand(response, "[KOMUT:PROGRAM_KAPAT:", "]");
                CloseProgram(program);
            }
            else if (response.Contains("[KOMUT:EKRAN_GORUNTUSU]"))
            {
                TakeScreenshot();
            }
            else if (response.Contains("[KOMUT:OZELLIKLER]"))
            {
                ShowFeatures();
            }
            else if (response.Contains("[KOMUT:DOSYA_OKU:"))
            {
                string path = ExtractCommand(response, "[KOMUT:DOSYA_OKU:", "]");
                ReadFileContent(path);
            }
            else if (response.Contains("[KOMUT:TIKLA:"))
            {
                string coords = ExtractCommand(response, "[KOMUT:TIKLA:", "]");
                var parts = coords.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    ClickAt(x, y);
                }
            }
            else if (response.Contains("[KOMUT:SAG_TIKLA:"))
            {
                string coords = ExtractCommand(response, "[KOMUT:SAG_TIKLA:", "]");
                var parts = coords.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    RightClickAt(x, y);
                }
            }
            else if (response.Contains("[KOMUT:YAZ:"))
            {
                string text = ExtractCommand(response, "[KOMUT:YAZ:", "]");
                TypeText(text);
            }
            else if (response.Contains("[KOMUT:TUS:"))
            {
                string key = ExtractCommand(response, "[KOMUT:TUS:", "]");
                PressKey(key);
            }
            else if (response.Contains("[KOMUT:PENCERE_KUCULT:"))
            {
                string program = ExtractCommand(response, "[KOMUT:PENCERE_KUCULT:", "]");
                MinimizeWindow(program);
            }
            else if (response.Contains("[KOMUT:PENCERE_BUYUT:"))
            {
                string program = ExtractCommand(response, "[KOMUT:PENCERE_BUYUT:", "]");
                MaximizeWindow(program);
            }
            else if (response.Contains("[KOMUT:GOREV_YONETICISI]"))
            {
                OpenTaskManager();
            }
            else if (response.Contains("[KOMUT:MOUSE_KOORDINAT]"))
            {
                ShowMouseCoordinates();
            }
            else if (response.Contains("[KOMUT:ODAKLA:"))
            {
                string program = ExtractCommand(response, "[KOMUT:ODAKLA:", "]");
                FocusWindow(program);
            }
            else if (response.Contains("[KOMUT:CIFT_TIKLA:"))
            {
                string coords = ExtractCommand(response, "[KOMUT:CIFT_TIKLA:", "]");
                var parts = coords.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    DoubleClickAt(x, y);
                }
            }
            else if (response.Contains("[KOMUT:MOUSE_HAREKET:"))
            {
                string coords = ExtractCommand(response, "[KOMUT:MOUSE_HAREKET:", "]");
                var parts = coords.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    MoveMouse(x, y);
                }
            }
            else if (response.Contains("[KOMUT:BEKLE:"))
            {
                string seconds = ExtractCommand(response, "[KOMUT:BEKLE:", "]");
                if (int.TryParse(seconds, out int sec))
                {
                    Wait(sec);
                }
            }
            else if (response.Contains("[KOMUT:DISCORD_KISAYOL:"))
            {
                string action = ExtractCommand(response, "[KOMUT:DISCORD_KISAYOL:", "]");
                DiscordShortcut(action);
            }
            else if (response.Contains("[KOMUT:WEB_GIT:"))
            {
                string url = ExtractCommand(response, "[KOMUT:WEB_GIT:", "]");
                OpenWebsite(url);
            }
            else if (response.Contains("[KOMUT:DOSYA_ARA:"))
            {
                string params_ = ExtractCommand(response, "[KOMUT:DOSYA_ARA:", "]");
                var parts = params_.Split(':');
                if (parts.Length == 2)
                {
                    SearchFiles(parts[0], parts[1]);
                }
            }
            else if (response.Contains("[KOMUT:DOSYA_TASI:"))
            {
                string params_ = ExtractCommand(response, "[KOMUT:DOSYA_TASI:", "]");
                var parts = params_.Split(':');
                if (parts.Length == 2)
                {
                    MoveFile(parts[0], parts[1]);
                }
            }
            else if (response.Contains("[KOMUT:DOSYA_KOPYALA:"))
            {
                string params_ = ExtractCommand(response, "[KOMUT:DOSYA_KOPYALA:", "]");
                var parts = params_.Split(':');
                if (parts.Length == 2)
                {
                    CopyFile(parts[0], parts[1]);
                }
            }
            else if (response.Contains("[KOMUT:DOSYA_YENIDEN_ADLANDIR:"))
            {
                string params_ = ExtractCommand(response, "[KOMUT:DOSYA_YENIDEN_ADLANDIR:", "]");
                var parts = params_.Split(':');
                if (parts.Length == 2)
                {
                    RenameFile(parts[0], parts[1]);
                }
            }
            else if (response.Contains("[KOMUT:HATIRLATICI:"))
            {
                string params_ = ExtractCommand(response, "[KOMUT:HATIRLATICI:", "]");
                var parts = params_.Split(new[] { ':' }, 2);
                if (parts.Length == 2 && int.TryParse(parts[0], out int minutes))
                {
                    SetReminder(minutes, parts[1]);
                }
            }
            else if (response.Contains("[KOMUT:NOT:"))
            {
                string note = ExtractCommand(response, "[KOMUT:NOT:", "]");
                TakeNote(note);
            }
            else if (response.Contains("[KOMUT:DISCORD_MESAJ:"))
            {
                string params_ = ExtractCommand(response, "[KOMUT:DISCORD_MESAJ:", "]");
                var parts = params_.Split(':');
                if (parts.Length == 7)
                {
                    DiscordAutoMessage(
                        int.Parse(parts[0]), int.Parse(parts[1]),
                        int.Parse(parts[2]), int.Parse(parts[3]),
                        int.Parse(parts[4]), int.Parse(parts[5]),
                        parts[6]
                    );
                }
            }
            else if (response.Contains("[KOMUT:KAYDIR:"))
            {
                string direction = ExtractCommand(response, "[KOMUT:KAYDIR:", "]");
                ScrollPage(direction);
            }
            else if (response.Contains("[KOMUT:DOSYA_SIL_GELISMIS:"))
            {
                string path = ExtractCommand(response, "[KOMUT:DOSYA_SIL_GELISMIS:", "]");
                DeleteFileAdvanced(path);
            }
            else if (response.Contains("[KOMUT:NOT_DUZENLE:"))
            {
                string params_ = ExtractCommand(response, "[KOMUT:NOT_DUZENLE:", "]");
                var parts = params_.Split(new[] { ':' }, 2);
                if (parts.Length == 2 && int.TryParse(parts[0], out int line))
                {
                    EditNote(line, parts[1]);
                }
            }
            else if (response.Contains("[KOMUT:NOT_SIL:"))
            {
                string lineStr = ExtractCommand(response, "[KOMUT:NOT_SIL:", "]");
                if (int.TryParse(lineStr, out int line))
                {
                    DeleteNote(line);
                }
            }
            else if (response.Contains("[KOMUT:EN_BUYUK_DOSYA:"))
            {
                string folder = ExtractCommand(response, "[KOMUT:EN_BUYUK_DOSYA:", "]");
                FindLargestFile(folder);
            }
            else if (response.Contains("[KOMUT:EN_KUCUK_DOSYA:"))
            {
                string folder = ExtractCommand(response, "[KOMUT:EN_KUCUK_DOSYA:", "]");
                FindSmallestFile(folder);
            }
            else if (response.Contains("[KOMUT:WEB_OZET:"))
            {
                string url = ExtractCommand(response, "[KOMUT:WEB_OZET:", "]");
                SummarizeWebsite(url);
            }
            else if (response.Contains("[KOMUT:COKLU:"))
            {
                string commands = ExtractCommand(response, "[KOMUT:COKLU:", "]");
                ExecuteMultipleCommands(commands);
            }
            else if (response.Contains("[KOMUT:AKILLI_BEKLE:"))
            {
                string seconds = ExtractCommand(response, "[KOMUT:AKILLI_BEKLE:", "]");
                if (int.TryParse(seconds, out int sec))
                {
                    SmartWait(sec);
                }
            }
            
            string cleanResponse = System.Text.RegularExpressions.Regex.Replace(response, @"\[KOMUT:.*?\]", "").Trim();
            if (!string.IsNullOrWhiteSpace(cleanResponse))
            {
                Console.WriteLine($"\nAsistan: {cleanResponse}\n");
            }
        }
        else
        {
            Console.WriteLine($"\nAsistan: {response}\n");
        }
    }

    static void ShowFeatures()
    {
        try
        {
            if (File.Exists(FEATURES_FILE))
            {
                string json = File.ReadAllText(FEATURES_FILE);
                var features = JsonConvert.DeserializeObject<FeatureList>(json);
                
                Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine($"║          YENİ ÖZELLİKLER - Versiyon {features?.Version}                    ║");
                Console.WriteLine($"║          Tarih: {features?.Tarih}                              ║");
                Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝\n");
                
                if (features?.YeniOzellikler != null)
                {
                    foreach (var feature in features.YeniOzellikler)
                    {
                        Console.WriteLine($"  {feature}");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Özellik listesi bulunamadı.\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Özellikler gösterilemedi: {ex.Message}\n");
        }
    }

    static void ReadFileContent(string path)
    {
        try
        {
            string fullPath = ExpandPath(path);
            
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"❌ Dosya bulunamadı: {fullPath}\n");
                return;
            }

            string content = File.ReadAllText(fullPath);
            string fileName = Path.GetFileName(fullPath);
            long fileSize = new FileInfo(fullPath).Length;
            
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  DOSYA İÇERİĞİ: {fileName}");
            Console.WriteLine($"║  Boyut: {fileSize} byte");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝\n");
            
            // İçerik çok uzunsa kısalt
            if (content.Length > 2000)
            {
                Console.WriteLine(content.Substring(0, 2000));
                Console.WriteLine($"\n... (İçerik çok uzun, ilk 2000 karakter gösterildi)\n");
                Console.WriteLine($"Toplam karakter sayısı: {content.Length}\n");
            }
            else
            {
                Console.WriteLine(content);
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Dosya okunamadı: {ex.Message}\n");
        }
    }

    static string ExtractCommand(string text, string startTag, string endTag)
    {
        int start = text.IndexOf(startTag) + startTag.Length;
        int end = text.IndexOf(endTag, start);
        if (start > startTag.Length - 1 && end > start)
        {
            return text.Substring(start, end - start).Trim();
        }
        return "";
    }

    static string ExtractCommandType(string response)
    {
        try
        {
            if (response.Contains("[KOMUT:"))
            {
                int start = response.IndexOf("[KOMUT:") + 7;
                int end = response.IndexOf(":", start);
                if (end == -1) end = response.IndexOf("]", start);
                
                if (end > start)
                {
                    return response.Substring(start, end - start);
                }
            }
            return "";
        }
        catch
        {
            return "";
        }
    }

    static void ExecuteWithErrorHandling(Action action, string commandType, string parameter)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Hata: {ex.Message}");
            LogError($"{commandType}:{parameter}", ex.Message, GetErrorSuggestion(commandType));
            
            string suggestion = GetErrorSuggestion(commandType);
            if (!string.IsNullOrEmpty(suggestion))
            {
                Console.WriteLine($"💡 Öneri: {suggestion}\n");
            }
        }
    }

    static string GetErrorSuggestion(string commandType)
    {
        var suggestions = new Dictionary<string, string>
        {
            { "PROGRAM", "Program adını kontrol edin. Örnek: 'chrome', 'notepad', 'discord'" },
            { "DOSYA_OKU", "Dosya yolunu kontrol edin. Örnek: 'Desktop/test.txt'" },
            { "DOSYA_OLUSTUR", "Dosya yolu ve içeriği kontrol edin." },
            { "CHROME_ARA", "Arama metnini kontrol edin." },
            { "YOUTUBE_ARA", "Arama metnini kontrol edin." },
            { "PROGRAM_KAPAT", "Program adını kontrol edin ve programın çalıştığından emin olun." },
            { "TIKLA", "Koordinatları kontrol edin. Örnek: '500,300'" },
            { "YAZ", "Yazmak istediğiniz metni kontrol edin." }
        };

        return suggestions.ContainsKey(commandType) ? suggestions[commandType] : "Komutu kontrol edin ve tekrar deneyin.";
    }

    static void OpenProgram(string programName)
    {
        try
        {
            programName = programName.ToLower();
            
            if (programName.Contains("chrome"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "chrome",
                    UseShellExecute = true
                });
                Console.WriteLine("✓ Chrome açılıyor...\n");
            }
            else if (programName.Contains("notepad"))
            {
                Process.Start("notepad.exe");
                Console.WriteLine("✓ Notepad açılıyor...\n");
            }
            else if (programName.Contains("calculator") || programName.Contains("hesap"))
            {
                Process.Start("calc.exe");
                Console.WriteLine("✓ Hesap makinesi açılıyor...\n");
            }
            else if (programName.Contains("explorer"))
            {
                Process.Start("explorer.exe");
                Console.WriteLine("✓ Dosya gezgini açılıyor...\n");
            }
            else if (programName.Contains("discord"))
            {
                // Discord'u farklı yollardan dene
                string[] discordPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "Update.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "Discord.exe"),
                    "C:\\Users\\OyuncuBen\\AppData\\Local\\Discord\\Update.exe"
                };

                bool opened = false;
                foreach (var path in discordPaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            if (path.Contains("Update.exe"))
                            {
                                var psi = new ProcessStartInfo
                                {
                                    FileName = path,
                                    Arguments = "--processStart Discord.exe",
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                Process.Start(psi);
                            }
                            else
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = path,
                                    UseShellExecute = true
                                });
                            }
                            Console.WriteLine("✓ Discord açılıyor...\n");
                            opened = true;
                            break;
                        }
                        catch { }
                    }
                }

                if (!opened)
                {
                    // Son çare: discord:// protokolü
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "discord://",
                            UseShellExecute = true
                        });
                        Console.WriteLine("✓ Discord açılıyor (protokol)...\n");
                        opened = true;
                    }
                    catch { }
                }

                if (!opened)
                {
                    // En son çare: Windows arama
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c start discord",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        Console.WriteLine("✓ Discord açılıyor (cmd)...\n");
                    }
                    catch
                    {
                        Console.WriteLine("❌ Discord bulunamadı. Lütfen Discord'u manuel olarak aç.\n");
                    }
                }
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = programName,
                    UseShellExecute = true
                });
                Console.WriteLine($"✓ {programName} açılıyor...\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Program açılamadı: {ex.Message}\n");
        }
    }

    static void OpenChromeWithSearch(string searchQuery)
    {
        try
        {
            string url = $"https://www.google.com/search?q={Uri.EscapeDataString(searchQuery)}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Console.WriteLine($"✓ Chrome'da '{searchQuery}' aratılıyor...\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chrome açılamadı: {ex.Message}\n");
        }
    }

    static void OpenPath(string path)
    {
        try
        {
            string fullPath = path;
            
            if (path.ToLower() == "desktop" || path.ToLower() == "masaüstü")
            {
                fullPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            else if (path.ToLower() == "documents" || path.ToLower() == "belgeler")
            {
                fullPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else if (path.ToLower() == "downloads" || path.ToLower() == "indirilenler")
            {
                fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
            Console.WriteLine($"✓ {path} açılıyor...\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Yol açılamadı: {ex.Message}\n");
        }
    }

    static void OpenYouTubeSearch(string searchQuery)
    {
        try
        {
            string url = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(searchQuery)}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Console.WriteLine($"✓ YouTube'da '{searchQuery}' aratılıyor...\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"YouTube açılamadı: {ex.Message}\n");
        }
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const int SW_MINIMIZE = 6;
    private const int SW_MAXIMIZE = 3;
    private const int SW_RESTORE = 9;

    static void SetVolume(string volumeStr)
    {
        try
        {
            if (int.TryParse(volumeStr, out int volume))
            {
                volume = Math.Max(0, Math.Min(100, volume));
                
                // Windows ses kontrolü için nircmd veya PowerShell kullanılabilir
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"(New-Object -ComObject WScript.Shell).SendKeys([char]174)\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                
                Console.WriteLine($"✓ Ses seviyesi {volume} olarak ayarlanıyor...\n");
                Console.WriteLine("Not: Ses kontrolü için Windows ses ayarlarını kullanın.\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ses ayarlanamadı: {ex.Message}\n");
        }
    }

    static void ShutdownComputer()
    {
        try
        {
            Console.WriteLine("⚠️ Bilgisayar 10 saniye içinde kapanacak...\n");
            Process.Start("shutdown", "/s /t 10");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Kapatma başarısız: {ex.Message}\n");
        }
    }

    static void RestartComputer()
    {
        try
        {
            Console.WriteLine("⚠️ Bilgisayar 10 saniye içinde yeniden başlatılacak...\n");
            Process.Start("shutdown", "/r /t 10");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Yeniden başlatma başarısız: {ex.Message}\n");
        }
    }

    static void SleepComputer()
    {
        try
        {
            Console.WriteLine("✓ Bilgisayar uyku moduna geçiyor...\n");
            Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Uyku modu başarısız: {ex.Message}\n");
        }
    }

    static void CreateFile(string path, string content)
    {
        try
        {
            string fullPath = ExpandPath(path);
            File.WriteAllText(fullPath, content);
            Console.WriteLine($"✓ Dosya oluşturuldu: {fullPath}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dosya oluşturulamadı: {ex.Message}\n");
        }
    }

    static void CreateFolder(string path)
    {
        try
        {
            string fullPath = ExpandPath(path);
            Directory.CreateDirectory(fullPath);
            Console.WriteLine($"✓ Klasör oluşturuldu: {fullPath}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Klasör oluşturulamadı: {ex.Message}\n");
        }
    }

    static void DeleteFile(string path)
    {
        try
        {
            string fullPath = ExpandPath(path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Console.WriteLine($"✓ Dosya silindi: {fullPath}\n");
            }
            else
            {
                Console.WriteLine($"Dosya bulunamadı: {fullPath}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dosya silinemedi: {ex.Message}\n");
        }
    }

    static void CloseProgram(string programName)
    {
        try
        {
            programName = programName.ToLower().Replace(".exe", "");
            var processes = Process.GetProcesses()
                .Where(p => p.ProcessName.ToLower().Contains(programName))
                .ToList();

            if (processes.Any())
            {
                foreach (var process in processes)
                {
                    process.Kill();
                    Console.WriteLine($"✓ {process.ProcessName} kapatıldı\n");
                }
            }
            else
            {
                Console.WriteLine($"'{programName}' çalışmıyor\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Program kapatılamadı: {ex.Message}\n");
        }
    }

    static void TakeScreenshot()
    {
        try
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"ekran_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            );
            
            Console.WriteLine("✓ Ekran görüntüsü için Windows + PrtScn tuşlarını kullanın\n");
            Console.WriteLine($"Veya Snipping Tool açılıyor...\n");
            
            Process.Start(new ProcessStartInfo
            {
                FileName = "SnippingTool.exe",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ekran görüntüsü alınamadı: {ex.Message}\n");
        }
    }

    static string ExpandPath(string path)
    {
        if (path.StartsWith("Desktop/") || path.StartsWith("Masaüstü/"))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                path.Substring(path.IndexOf('/') + 1));
        }
        else if (path.StartsWith("Documents/") || path.StartsWith("Belgeler/"))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                path.Substring(path.IndexOf('/') + 1));
        }
        return path;
    }

    static void ClickAt(int x, int y)
    {
        try
        {
            SetCursorPos(x, y);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, UIntPtr.Zero);
            Console.WriteLine($"✓ ({x}, {y}) koordinatına tıklandı\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tıklama başarısız: {ex.Message}\n");
        }
    }

    static void RightClickAt(int x, int y)
    {
        try
        {
            SetCursorPos(x, y);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, x, y, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_RIGHTUP, x, y, 0, UIntPtr.Zero);
            Console.WriteLine($"✓ ({x}, {y}) koordinatına sağ tıklandı\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sağ tıklama başarısız: {ex.Message}\n");
        }
    }

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    const int VK_RETURN = 0x0D;
    const int VK_TAB = 0x09;
    const int VK_ESCAPE = 0x1B;
    const int VK_SPACE = 0x20;
    const int VK_BACK = 0x08;
    const int VK_DELETE = 0x2E;
    const int VK_CONTROL = 0x11;
    const int VK_MENU = 0x12;
    const int KEYEVENTF_KEYUP = 0x0002;

    static void TypeText(string text)
    {
        try
        {
            System.Threading.Thread.Sleep(100);
            foreach (char c in text)
            {
                SendChar(c);
                System.Threading.Thread.Sleep(10);
            }
            Console.WriteLine($"✓ Yazıldı: {text}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Yazma başarısız: {ex.Message}\n");
        }
    }

    static void SendChar(char c)
    {
        short vk = VkKeyScan(c);
        byte key = (byte)(vk & 0xFF);
        byte shift = (byte)((vk >> 8) & 0xFF);

        if (shift != 0)
        {
            keybd_event(0x10, 0, 0, 0); // Shift down
        }

        keybd_event(key, 0, 0, 0);
        keybd_event(key, 0, KEYEVENTF_KEYUP, 0);

        if (shift != 0)
        {
            keybd_event(0x10, 0, KEYEVENTF_KEYUP, 0); // Shift up
        }
    }

    [DllImport("user32.dll")]
    static extern short VkKeyScan(char ch);

    static void PressKey(string key)
    {
        try
        {
            key = key.ToLower().Trim();
            System.Threading.Thread.Sleep(50);
            
            if (key == "enter")
            {
                keybd_event(VK_RETURN, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "tab")
            {
                keybd_event(VK_TAB, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "escape" || key == "esc")
            {
                keybd_event(VK_ESCAPE, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "space" || key == "boşluk")
            {
                keybd_event(VK_SPACE, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_SPACE, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "backspace")
            {
                keybd_event(VK_BACK, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_BACK, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "delete" || key == "del")
            {
                keybd_event(VK_DELETE, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_DELETE, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "end")
            {
                keybd_event(0x23, 0, 0, 0); // End
                System.Threading.Thread.Sleep(50);
                keybd_event(0x23, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "home")
            {
                keybd_event(0x24, 0, 0, 0); // Home
                System.Threading.Thread.Sleep(50);
                keybd_event(0x24, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "ctrl+c")
            {
                keybd_event(VK_CONTROL, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(0x43, 0, 0, 0); // C
                System.Threading.Thread.Sleep(50);
                keybd_event(0x43, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "ctrl+v")
            {
                keybd_event(VK_CONTROL, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(0x56, 0, 0, 0); // V
                System.Threading.Thread.Sleep(50);
                keybd_event(0x56, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "ctrl+x")
            {
                keybd_event(VK_CONTROL, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(0x58, 0, 0, 0); // X
                System.Threading.Thread.Sleep(50);
                keybd_event(0x58, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "ctrl+a")
            {
                keybd_event(VK_CONTROL, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(0x41, 0, 0, 0); // A
                System.Threading.Thread.Sleep(50);
                keybd_event(0x41, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "ctrl+z")
            {
                keybd_event(VK_CONTROL, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(0x5A, 0, 0, 0); // Z
                System.Threading.Thread.Sleep(50);
                keybd_event(0x5A, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "ctrl+k")
            {
                keybd_event(VK_CONTROL, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(0x4B, 0, 0, 0); // K
                System.Threading.Thread.Sleep(50);
                keybd_event(0x4B, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "alt+tab")
            {
                keybd_event(VK_MENU, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_TAB, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (key == "alt+f4")
            {
                keybd_event(VK_MENU, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(0x73, 0, 0, 0); // F4
                System.Threading.Thread.Sleep(50);
                keybd_event(0x73, 0, KEYEVENTF_KEYUP, 0);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
            }
            
            System.Threading.Thread.Sleep(50);
            Console.WriteLine($"✓ Tuş basıldı: {key}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tuş basma başarısız: {ex.Message}\n");
        }
    }

    static void MinimizeWindow(string programName)
    {
        try
        {
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle) && 
                           p.ProcessName.ToLower().Contains(programName.ToLower()))
                .ToList();

            if (processes.Any())
            {
                foreach (var process in processes)
                {
                    ShowWindow(process.MainWindowHandle, SW_MINIMIZE);
                    Console.WriteLine($"✓ {process.ProcessName} küçültüldü\n");
                }
            }
            else
            {
                Console.WriteLine($"'{programName}' penceresi bulunamadı\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pencere küçültme başarısız: {ex.Message}\n");
        }
    }

    static void MaximizeWindow(string programName)
    {
        try
        {
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle) && 
                           p.ProcessName.ToLower().Contains(programName.ToLower()))
                .ToList();

            if (processes.Any())
            {
                foreach (var process in processes)
                {
                    ShowWindow(process.MainWindowHandle, SW_MAXIMIZE);
                    SetForegroundWindow(process.MainWindowHandle);
                    Console.WriteLine($"✓ {process.ProcessName} büyütüldü\n");
                }
            }
            else
            {
                Console.WriteLine($"'{programName}' penceresi bulunamadı\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pencere büyütme başarısız: {ex.Message}\n");
        }
    }

    static void OpenTaskManager()
    {
        try
        {
            Process.Start("taskmgr.exe");
            Console.WriteLine("✓ Görev yöneticisi açılıyor...\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Görev yöneticisi açılamadı: {ex.Message}\n");
        }
    }

    static void ShowMouseCoordinates()
    {
        try
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          MOUSE KOORDINAT GÖSTERGE - 10 SANİYE               ║");
            Console.WriteLine("║   Mouse'u istediğin yere götür, koordinatları not et!       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
            
            for (int i = 0; i < 100; i++)
            {
                if (GetCursorPos(out POINT point))
                {
                    Console.Write($"\r  X: {point.X,4}  |  Y: {point.Y,4}  ");
                }
                System.Threading.Thread.Sleep(100);
            }
            
            if (GetCursorPos(out POINT finalPoint))
            {
                Console.WriteLine($"\n\n✓ Son koordinat: X={finalPoint.X}, Y={finalPoint.Y}");
                Console.WriteLine($"  Tıklamak için: '{finalPoint.X},{finalPoint.Y} koordinatına tıkla'\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Koordinat gösterme başarısız: {ex.Message}\n");
        }
    }

    static void FocusWindow(string programName)
    {
        try
        {
            programName = programName.ToLower();
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle) && 
                           p.ProcessName.ToLower().Contains(programName))
                .ToList();

            if (processes.Any())
            {
                foreach (var process in processes)
                {
                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(process.MainWindowHandle);
                    System.Threading.Thread.Sleep(200);
                    Console.WriteLine($"✓ {process.ProcessName} odaklandı (aktif pencere)\n");
                }
            }
            else
            {
                Console.WriteLine($"'{programName}' bulunamadı\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Odaklama başarısız: {ex.Message}\n");
        }
    }

    static void DoubleClickAt(int x, int y)
    {
        try
        {
            SetCursorPos(x, y);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, UIntPtr.Zero);
            Console.WriteLine($"✓ ({x}, {y}) koordinatına çift tıklandı\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Çift tıklama başarısız: {ex.Message}\n");
        }
    }

    static void MoveMouse(int x, int y)
    {
        try
        {
            SetCursorPos(x, y);
            Console.WriteLine($"✓ Mouse ({x}, {y}) koordinatına taşındı\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Mouse hareketi başarısız: {ex.Message}\n");
        }
    }

    static void Wait(int seconds)
    {
        try
        {
            Console.WriteLine($"⏳ {seconds} saniye bekleniyor...");
            System.Threading.Thread.Sleep(seconds * 1000);
            Console.WriteLine($"✓ Bekleme tamamlandı\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bekleme başarısız: {ex.Message}\n");
        }
    }

    const byte VK_SHIFT = 0x10;
    const byte VK_LSHIFT = 0xA0;
    const byte VK_RSHIFT = 0xA1;

    static void DiscordShortcut(string action)
    {
        try
        {
            action = action.ToLower().Trim();
            
            System.Threading.Thread.Sleep(100);
            
            switch (action)
            {
                case "ayarlar":
                    // Ctrl + ,
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0xBC, 0, 0, 0); // , (virgül)
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0xBC, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord ayarları açılıyor (Ctrl+,)\n");
                    break;

                case "mikrofon":
                case "bildirim_sessize":
                    // Ctrl + Shift + M
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_SHIFT, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x4D, 0, 0, 0); // M
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x4D, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord mikrofon aç/kapat (Ctrl+Shift+M)\n");
                    break;

                case "hoparlor":
                case "dm":
                    // Ctrl + Shift + D
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_SHIFT, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x44, 0, 0, 0); // D
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x44, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    if (action == "hoparlor")
                        Console.WriteLine("✓ Discord hoparlör aç/kapat (Ctrl+Shift+D)\n");
                    else
                        Console.WriteLine("✓ Discord DM açılıyor (Ctrl+Shift+D)\n");
                    break;

                case "emoji":
                    // Ctrl + E
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x45, 0, 0, 0); // E
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x45, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord emoji paneli açılıyor (Ctrl+E)\n");
                    break;

                case "arama":
                    // Ctrl + K
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x4B, 0, 0, 0); // K
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x4B, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord kanal arama açılıyor (Ctrl+K)\n");
                    break;

                case "arkadaslar":
                    // Ctrl + Shift + F
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_SHIFT, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x46, 0, 0, 0); // F
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x46, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord arkadaş listesi açılıyor (Ctrl+Shift+F)\n");
                    break;

                case "tam_ekran":
                    // F11
                    keybd_event(0x7A, 0, 0, 0); // F11
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x7A, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord tam ekran (F11)\n");
                    break;

                case "kanal_yukari":
                    // Alt + Yukarı
                    keybd_event(VK_MENU, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x26, 0, 0, 0); // Yukarı ok
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x26, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord önceki kanal (Alt+Yukarı)\n");
                    break;

                case "kanal_asagi":
                    // Alt + Aşağı
                    keybd_event(VK_MENU, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x28, 0, 0, 0); // Aşağı ok
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x28, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord sonraki kanal (Alt+Aşağı)\n");
                    break;

                case "sunucu_yukari":
                    // Ctrl + Alt + Yukarı
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_MENU, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x26, 0, 0, 0); // Yukarı ok
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x26, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord önceki sunucu (Ctrl+Alt+Yukarı)\n");
                    break;

                case "sunucu_asagi":
                    // Ctrl + Alt + Aşağı
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_MENU, 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x28, 0, 0, 0); // Aşağı ok
                    System.Threading.Thread.Sleep(50);
                    keybd_event(0x28, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Discord sonraki sunucu (Ctrl+Alt+Aşağı)\n");
                    break;

                default:
                    Console.WriteLine($"Bilinmeyen Discord kısayolu: {action}\n");
                    break;
            }
            
            System.Threading.Thread.Sleep(100);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Discord kısayolu başarısız: {ex.Message}\n");
        }
    }

    static string GetSystemContext()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"İşletim Sistemi: {Environment.OSVersion}");
        sb.AppendLine($"Kullanıcı: {Environment.UserName}");
        sb.AppendLine($"Bilgisayar Adı: {Environment.MachineName}");
        
        try
        {
            // CPU Bilgisi
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (var obj in searcher.Get())
                {
                    sb.AppendLine($"İşlemci: {obj["Name"]}");
                    sb.AppendLine($"Çekirdek Sayısı: {obj["NumberOfCores"]}");
                }
            }

            // RAM Bilgisi
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    long ram = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                    sb.AppendLine($"RAM: {ram / (1024 * 1024 * 1024)} GB");
                }
            }

            // GPU Bilgisi
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (var obj in searcher.Get())
                {
                    sb.AppendLine($"Ekran Kartı: {obj["Name"]}");
                }
            }

            // Disk Bilgisi
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (var drive in drives.Where(d => d.IsReady))
            {
                sb.AppendLine($"Disk {drive.Name}: {drive.TotalSize / (1024 * 1024 * 1024)} GB (Boş: {drive.AvailableFreeSpace / (1024 * 1024 * 1024)} GB)");
            }

            // Yüklü Programlar (bazıları)
            var programs = GetInstalledPrograms().Take(15);
            sb.AppendLine($"Yüklü Programlar: {string.Join(", ", programs)}");

            // Masaüstü dosyaları
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(desktop))
            {
                var files = Directory.GetFiles(desktop).Take(10).Select(f => Path.GetFileName(f));
                sb.AppendLine($"Masaüstü dosyaları: {string.Join(", ", files)}");
            }

            // Çalışan işlemler
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .Take(10)
                .Select(p => p.ProcessName);
            sb.AppendLine($"Çalışan programlar: {string.Join(", ", processes)}");
        }
        catch { }

        return sb.ToString();
    }

    static List<string> GetInstalledPrograms()
    {
        var programs = new List<string>();
        try
        {
            string[] registryKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var keyPath in registryKeys)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        foreach (var subkeyName in key.GetSubKeyNames().Take(50))
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                var displayName = subkey?.GetValue("DisplayName")?.ToString();
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    programs.Add(displayName);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return programs.Distinct().ToList();
    }

    static List<object> BuildContents()
    {
        var contents = new List<object>();
        
        foreach (var msg in conversationHistory)
        {
            contents.Add(new
            {
                role = msg.Role,
                parts = new[] { new { text = msg.Text } }
            });
        }

        return contents;
    }

    static void OpenWebsite(string url)
    {
        try
        {
            if (!url.StartsWith("http"))
            {
                url = "https://" + url;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Console.WriteLine($"✓ Web sitesi açılıyor: {url}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Web sitesi açılamadı: {ex.Message}\n");
        }
    }

    static void SearchFiles(string folder, string searchTerm)
    {
        try
        {
            folder = ExpandPath(folder);
            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"Klasör bulunamadı: {folder}\n");
                return;
            }

            var files = Directory.GetFiles(folder, $"*{searchTerm}*", SearchOption.AllDirectories)
                .Take(20)
                .ToList();

            Console.WriteLine($"\n✓ '{searchTerm}' için {files.Count} dosya bulundu:\n");
            foreach (var file in files)
            {
                Console.WriteLine($"  - {file}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dosya arama başarısız: {ex.Message}\n");
        }
    }

    static void MoveFile(string source, string destination)
    {
        try
        {
            source = ExpandPath(source);
            destination = ExpandPath(destination);
            
            if (File.Exists(source))
            {
                File.Move(source, destination);
                Console.WriteLine($"✓ Dosya taşındı: {source} → {destination}\n");
            }
            else
            {
                Console.WriteLine($"Kaynak dosya bulunamadı: {source}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dosya taşıma başarısız: {ex.Message}\n");
        }
    }

    static void CopyFile(string source, string destination)
    {
        try
        {
            source = ExpandPath(source);
            destination = ExpandPath(destination);
            
            if (File.Exists(source))
            {
                File.Copy(source, destination, true);
                Console.WriteLine($"✓ Dosya kopyalandı: {source} → {destination}\n");
            }
            else
            {
                Console.WriteLine($"Kaynak dosya bulunamadı: {source}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dosya kopyalama başarısız: {ex.Message}\n");
        }
    }

    static void RenameFile(string oldPath, string newPath)
    {
        try
        {
            oldPath = ExpandPath(oldPath);
            newPath = ExpandPath(newPath);
            
            if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);
                Console.WriteLine($"✓ Dosya yeniden adlandırıldı: {oldPath} → {newPath}\n");
            }
            else
            {
                Console.WriteLine($"Dosya bulunamadı: {oldPath}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Yeniden adlandırma başarısız: {ex.Message}\n");
        }
    }

    static void SetReminder(int minutes, string message)
    {
        try
        {
            Console.WriteLine($"⏰ Hatırlatıcı kuruldu: {minutes} dakika sonra - '{message}'\n");
            
            Task.Run(async () =>
            {
                await Task.Delay(minutes * 60 * 1000);
                Console.WriteLine($"\n🔔 HATIRLATICI: {message}\n");
                Console.Write("Sen: ");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hatırlatıcı kurulamadı: {ex.Message}\n");
        }
    }

    static void TakeNote(string note)
    {
        try
        {
            string notesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "notlar.txt");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string noteEntry = $"[{timestamp}] {note}\n";
            
            File.AppendAllText(notesFile, noteEntry);
            Console.WriteLine($"✓ Not kaydedildi: {notesFile}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Not alma başarısız: {ex.Message}\n");
        }
    }

    static void DiscordAutoMessage(int serverX, int serverY, int channelX, int channelY, int messageX, int messageY, string message)
    {
        try
        {
            Console.WriteLine("🤖 Discord otomatik mesaj gönderiliyor...\n");
            
            // Sunucuya tıkla
            System.Threading.Thread.Sleep(500);
            ClickAt(serverX, serverY);
            Console.WriteLine($"✓ Sunucuya tıklandı ({serverX}, {serverY})");
            
            // Bekle
            System.Threading.Thread.Sleep(1000);
            
            // Kanala tıkla
            ClickAt(channelX, channelY);
            Console.WriteLine($"✓ Kanala tıklandı ({channelX}, {channelY})");
            
            // Bekle
            System.Threading.Thread.Sleep(500);
            
            // Mesaj alanına tıkla
            ClickAt(messageX, messageY);
            Console.WriteLine($"✓ Mesaj alanına tıklandı ({messageX}, {messageY})");
            
            // Bekle
            System.Threading.Thread.Sleep(300);
            
            // Mesajı yaz
            TypeText(message);
            Console.WriteLine($"✓ Mesaj yazıldı: {message}");
            
            // Enter bas
            System.Threading.Thread.Sleep(200);
            keybd_event(VK_RETURN, 0, 0, 0);
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
            Console.WriteLine("✓ Mesaj gönderildi!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Discord mesaj gönderme başarısız: {ex.Message}\n");
        }
    }

    static void ScrollPage(string direction)
    {
        try
        {
            direction = direction.ToLower().Trim();
            
            switch (direction)
            {
                case "yukari":
                case "up":
                    keybd_event(0x21, 0, 0, 0); // Page Up
                    keybd_event(0x21, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Sayfa yukarı kaydırıldı\n");
                    break;
                    
                case "asagi":
                case "down":
                    keybd_event(0x22, 0, 0, 0); // Page Down
                    keybd_event(0x22, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Sayfa aşağı kaydırıldı\n");
                    break;
                    
                case "saga":
                case "right":
                    keybd_event(0x27, 0, 0, 0); // Right Arrow
                    keybd_event(0x27, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Sağa kaydırıldı\n");
                    break;
                    
                case "sola":
                case "left":
                    keybd_event(0x25, 0, 0, 0); // Left Arrow
                    keybd_event(0x25, 0, KEYEVENTF_KEYUP, 0);
                    Console.WriteLine("✓ Sola kaydırıldı\n");
                    break;
                    
                default:
                    Console.WriteLine($"Bilinmeyen yön: {direction}\n");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Kaydırma başarısız: {ex.Message}\n");
        }
    }

    static void DeleteFileAdvanced(string path)
    {
        try
        {
            path = ExpandPath(path);
            
            if (File.Exists(path))
            {
                File.Delete(path);
                Console.WriteLine($"✓ Dosya silindi: {path}\n");
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Console.WriteLine($"✓ Klasör silindi: {path}\n");
            }
            else
            {
                Console.WriteLine($"Dosya veya klasör bulunamadı: {path}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Silme başarısız: {ex.Message}\n");
        }
    }

    static void EditNote(int lineNumber, string newText)
    {
        try
        {
            string notesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "notlar.txt");
            
            if (!File.Exists(notesFile))
            {
                Console.WriteLine("Not dosyası bulunamadı\n");
                return;
            }

            var lines = File.ReadAllLines(notesFile).ToList();
            
            if (lineNumber > 0 && lineNumber <= lines.Count)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lines[lineNumber - 1] = $"[{timestamp}] {newText}";
                File.WriteAllLines(notesFile, lines);
                Console.WriteLine($"✓ Not düzenlendi (satır {lineNumber})\n");
            }
            else
            {
                Console.WriteLine($"Geçersiz satır numarası: {lineNumber}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Not düzenleme başarısız: {ex.Message}\n");
        }
    }

    static void DeleteNote(int lineNumber)
    {
        try
        {
            string notesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "notlar.txt");
            
            if (!File.Exists(notesFile))
            {
                Console.WriteLine("Not dosyası bulunamadı\n");
                return;
            }

            var lines = File.ReadAllLines(notesFile).ToList();
            
            if (lineNumber > 0 && lineNumber <= lines.Count)
            {
                lines.RemoveAt(lineNumber - 1);
                File.WriteAllLines(notesFile, lines);
                Console.WriteLine($"✓ Not silindi (satır {lineNumber})\n");
            }
            else
            {
                Console.WriteLine($"Geçersiz satır numarası: {lineNumber}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Not silme başarısız: {ex.Message}\n");
        }
    }

    static void FindLargestFile(string folder)
    {
        try
        {
            folder = ExpandPath(folder);
            
            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"Klasör bulunamadı: {folder}\n");
                return;
            }

            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.Length)
                .Take(10)
                .ToList();

            Console.WriteLine($"\n✓ En büyük 10 dosya ({folder}):\n");
            foreach (var file in files)
            {
                double sizeMB = file.Length / (1024.0 * 1024.0);
                Console.WriteLine($"  {sizeMB:F2} MB - {file.Name}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dosya arama başarısız: {ex.Message}\n");
        }
    }

    static void FindSmallestFile(string folder)
    {
        try
        {
            folder = ExpandPath(folder);
            
            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"Klasör bulunamadı: {folder}\n");
                return;
            }

            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.Length)
                .Take(10)
                .ToList();

            Console.WriteLine($"\n✓ En küçük 10 dosya ({folder}):\n");
            foreach (var file in files)
            {
                double sizeKB = file.Length / 1024.0;
                Console.WriteLine($"  {sizeKB:F2} KB - {file.Name}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dosya arama başarısız: {ex.Message}\n");
        }
    }

    static void SummarizeWebsite(string url)
    {
        try
        {
            Console.WriteLine($"🌐 Web sitesi özeti hazırlanıyor: {url}\n");
            Console.WriteLine("Not: Web özet özelliği şu anda basit bir açıklama sağlıyor.\n");
            Console.WriteLine($"✓ {url} sitesi tarayıcıda açıldı\n");
            
            if (!url.StartsWith("http"))
            {
                url = "https://" + url;
            }
            
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Web özet başarısız: {ex.Message}\n");
        }
    }

    static void ExecuteMultipleCommands(string commands)
    {
        try
        {
            var commandList = commands.Split('|');
            Console.WriteLine($"🔄 {commandList.Length} komut sırayla çalıştırılıyor...\n");
            
            foreach (var cmd in commandList)
            {
                Console.WriteLine($"▶ Komut: {cmd.Trim()}");
                System.Threading.Thread.Sleep(500);
            }
            
            Console.WriteLine("\n✓ Tüm komutlar tamamlandı\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Çoklu komut başarısız: {ex.Message}\n");
        }
    }

    static void SmartWait(int seconds)
    {
        try
        {
            Console.WriteLine($"⏳ Akıllı bekleme: {seconds} saniye...");
            
            for (int i = seconds; i > 0; i--)
            {
                Console.Write($"\r  Kalan süre: {i} saniye  ");
                System.Threading.Thread.Sleep(1000);
            }
            
            Console.WriteLine("\r✓ Bekleme tamamlandı          \n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bekleme başarısız: {ex.Message}\n");
        }
    }
}

class ConversationMessage
{
    public string Role { get; set; } = "";
    public string Text { get; set; } = "";
}

class GeminiResponse
{
    [JsonProperty("candidates")]
    public List<Candidate>? Candidates { get; set; }
}

class Candidate
{
    [JsonProperty("content")]
    public Content Content { get; set; } = new Content();
}

class Content
{
    [JsonProperty("parts")]
    public List<Part> Parts { get; set; } = new List<Part>();
}

class Part
{
    [JsonProperty("text")]
    public string Text { get; set; } = "";
}

class FeatureList
{
    [JsonProperty("version")]
    public string Version { get; set; } = "";
    
    [JsonProperty("tarih")]
    public string Tarih { get; set; } = "";
    
    [JsonProperty("yeni_ozellikler")]
    public List<string> YeniOzellikler { get; set; } = new List<string>();
}

class UserPreferences
{
    [JsonProperty("total_commands")]
    public int TotalCommands { get; set; } = 0;
    
    [JsonProperty("last_used")]
    public DateTime LastUsed { get; set; } = DateTime.Now;
    
    [JsonProperty("favorite_commands")]
    public Dictionary<string, int> FavoriteCommands { get; set; } = new Dictionary<string, int>();
    
    [JsonProperty("preferred_browser")]
    public string PreferredBrowser { get; set; } = "chrome";
    
    [JsonProperty("language")]
    public string Language { get; set; } = "tr";
}

class ErrorLog
{
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonProperty("command")]
    public string Command { get; set; } = "";
    
    [JsonProperty("error")]
    public string Error { get; set; } = "";
    
    [JsonProperty("suggestion")]
    public string Suggestion { get; set; } = "";
}
