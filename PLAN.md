# Pong ödevi: uygulama ve teslim planı

Kaynak: `/Users/siracsimsek/Downloads/Week03_Homework.pdf`. Hedef Unity 6000.3.2f1 + Universal 3D + FishNet 4.7.3R ile iki laptopta aynı topu ve skoru paylaşan Pong.

## 1. Hazırlanan işler

- `Starter 2` korunarak başlangıç scriptleri `Pong/Assets/Scripts/Starter` içine alındı.
- FishNet resmi 4.7.3 etiketinden projeye eklendi; Unity paket sürümleri sabitlendi.
- İstenen beş ağ scripti yazıldı. W/S veya oklar sadece sahip olunan raketi hareket ettirir. İlk iki bağlantı LEFT/RIGHT, diğerleri spectator olur.
- Sunucu topu oluşturur, hareketini ve golleri hesaplar. Skor ve servis sırası SyncVar, olay geçmişi SyncList ile paylaşılır.
- Space için ServerRpc, gol duyurusu için ObserversRpc, tek oyuncuya YOU SERVE için TargetRpc eklendi.
- Eksik ders Tools dosyaları yerine bağlantı düğmeleri, IP alanı, RTT/tick/bant genişliği ve olay paneli olan HUD ile ServerBootstrap yazıldı.
- İlk başarılı Unity açılışında yerel/çok oyunculu sahneleri ve bağlı prefabları oluşturan Editor aracı yazıldı.
- README açıklamaları, tasarım tablosu, demo çalışma notları ve gerçek test kartları hazırlandı. Ölçülmeyen bilgiler boş bırakıldı.

## 2. Unity kurulumu ve ilk doğrulama

Proje Unity Hub'a eklendi ve artık Unity 6000.3.2f1'de açılıp derleniyor. Input System 1.20.1, URP 17.3.0 ve Multiplayer Play Mode 2.0.2 kullanılıyor. Unity MCP v10.0.0 kuruldu ve yerel Editor bağlantısı doğrulandı. Sahne/prefab üretimi tamamlandı; FishNet sahne kimlikleri onarıldı. Gerçek Editor host ve ayrı build istemcileriyle tek top, roller, servis, skor ve yeniden katılma testleri yapıldı.

İlk açılışta otomatik araç `Assets/Scenes/Pong-Start.unity`, `Pong.unity`, `Assets/Prefabs/Paddle.prefab` ve `Ball.prefab` oluşturur. Otomatik kurulum çalışmazsa `Pong > Create missing scenes and prefabs` menüsünü çalıştır. Mevcut Pong sahnesi varsa araç onu değiştirmez.

Önce Pong-Start sahnesinde yerel oyunu kontrol et. Sonra Pong sahnesinde Start Server ve Start Client ile bağlantı loglarını, mavi raketi ve gri topu kontrol et. Console hatası varsa tam mesaj üzerinden düzelt; hata varken sonraki aşamayı tamamlandı sayma.

## 3. Tek laptopta çok oyunculu test

Multiplayer Play Mode'da Server etiketi oluştur ve Player 2'ye ver. Player 2/3/4'ü etkinleştir. Main, Player 3 ve Player 4 istemci olarak bağlansın. Beklenen roller LEFT, RIGHT, SPECTATOR. W veya oklar sadece odaktaki oyuncunun raketini hareket ettirmeli; hareket diğer pencerelerde görünmeli.

LEFT Space ile servis atsın. RIGHT'ın sıra dışı servis isteği etkisiz kalmalı. Golü, ortak skoru ve farklı hedeflere giden bildirimleri kontrol et. İstemciyi koparıp tekrar bağla: skor sıfırlanmamalı, geçmişin son altı satırı gelmeli. Sunucuyu kapatıp açınca yeni maç sıfır skorla başlamalı.

## 4. Gerçek iki laptop testi

Ödevin sırasına göre Step 6'dan sonra ilk partner buluşması yapılır. Aynı telefon hotspot'una bağlan; sunucu IP'sini HUD'a yaz; iki yönde de dene. Partner adı, ağ, IP, ClientId/side, RTT ve ilk deneme sonucu gerçek değerlerle tabloya girilsin.

İkinci buluşmada servis, ralli, gol ve maç ortasında yeniden bağlanma provasını yapın. Tek laptop testleri bu kısmın yerine geçmez.

## 5. Ölçümler ve yazılı teslim

10/60/30 Hz'de aynı istemci sayısı ve benzer aktif ralliyle en az 10 saniye sonra server OUT B/s değerini oku. Tabloya sayıyı ve görünen top/raket akıcılığını yaz; sahneyi 30 Hz'de bırak. Tahminlerini gerçek gözlemlere göre right/wrong/half olarak değerlendir.

Ders notlarının 13 ve 14. bölümleri eksik. 13. bölümden iki kelimeyle sözlü provayı, 14. bölümün gerçek sorularıyla cevapları tamamla. Kendi tahminlerini ve kendi cümlelerini kullan; açıklayamadığın kodu çalışıp öğren.

## 6. GitHub ve demo

GitHub deposu özel olarak oluşturuldu: https://github.com/unitybtw/dgd0005-siracsimsek . Ayrı tasarım kaydı, oyun kaynakları ve doğrulama/ödev cevapları gerçek commitlerle push edildi. Geçmişte yapılmamış adımlar için tarih veya test sonucu uydurmayın. Tasarım tablosunun ayrı commit bağlantısını öğretmene öğrenci gönderir; bu çalışma dışarı mesaj göndermedi.

Demo gününde PDF'deki 12:00 teslim sınırına göre bağlantıyı önceden kurun. Tarih PDF'de belirtilmediği için takvim günü varsayılmadı. İki dakikalık demo: iki raket, servis/ralli/gol, skor ve flash, yalnız bir kişiye YOU SERVE, kopup yeniden bağlanma, iki terim ve bir satırın açıklaması; sonra rolleri değiştirin.

## Tamamlanma ölçütü

Kodların statik derlenmesi tek başına ödevin tamamlandığı anlamına gelmez. `Week03/README.md` içindeki gerçek test listesi, partner kartları, ölçümler, eksik ders soruları ve GitHub teslimi tamamlanınca ödev teslime hazır olur.

## Kalan Editor uyarısı

Bazı resmi Unity paketlerinde invalid signature uyarısı mevcut. Unity bunu eski sürümlerde sertifika geçerlilik zamanı hatası olarak belgeliyor; düzeltme 6000.3.7f1'de. Ödev 6000.3.2f1 istediği için sürüm değiştirilmedi ve imza denetimi kapatılmadı. Oyun derleme hatası kalmadı; bu uyarı ayrı bir Editor sorunu olarak korunuyor.

## Güncel tamamlanma durumu

Yerel starter W/S-oklar ve gol logları, gerçek owner W ve Space ile ağ oyunu, sunucu yeniden başlatma, dört Unity Multiplayer Play Mode süreci (Server/LEFT/RIGHT/SPECTATOR), ortak top/skor/olay geçmişi, 10/30/60 Hz gerçek trafik ölçümleri ve ders notlarının 13-14. bölüm cevapları tamamlandı. Ölçümlerin istemci görüntüsü sütunu güncelleme aralıklarını açıklar; headless ölçüme insan gözlemi yapılmış gibi davranılmaz.

İkinci laptopun hazır olmadığını kullanıcı bildirdi. İki fiziksel laptop, iki hotspot buluşması, bağlantı kartı ve sözlü demo bu nedenle hâlâ gerçek uygulama gerektirir. Özgün geliştirme sırasında adım adım commit kronolojisi geriye dönük üretilemez. Öğretmene tasarım commit bağlantısı gönderilmedi; özel depoya erişim verilmesi veya görünürlük kararı öğrenciye aittir.


## 18 Ekim 2026 - Homework 2

Yeni ödevin beş adımı mevcut proje üzerinde uygulanıyor. Paddle ve Ball sunucu yetkisinde; yön girdisi her tick RPC ile gönderiliyor, sunucuda doğrulanıp 30 ms aralıkla kabul ediliyor. C/X/T ve oyuncu başına ret sayacı eklendi. Week04/README.md yeni teslimin cevaplarını ve doğrulama kapsamını içerir. Önceki Week03 ölçümleri tarihsel kanıttır. Bulut dağıtımı yeni Homework2.pdf'nin beş zorunlu adımında yoktur; ders notlarındaki genel bulut anlatımı dağıtım yapılmış olduğu anlamına gelmez.
