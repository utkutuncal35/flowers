# Çiçek Bahçesi (çalışma adı) — Tasarım Belgesi (GDD)

## Özet
Kaydırmalı, üçlü eşleştirme (match-3) tarzında, bölümlü bir çiçek bulmaca oyunu. Dikey ekran, Android. Utku'nun kız arkadaşına kişisel bir hediye olarak yapılıyor. Kişisel dokunuşlar oyunun en önemli parçası.

## Çekirdek döngü
Bölümü aç → hedefi gör → çiçekleri kaydırıp 3+ eşleştir → hedefi hamleler bitmeden tamamla → yıldızlarını al → sonraki bölüm.

## Tahta
- 7 sütun x 9 satır (dikey ekrana uygun). Boyut bölüm bazında değiştirilebilir.
- **5 çiçek türü.** Her türün **hem rengi hem şekli farklı** olmalı (aşağıdaki "Okunabilirlik" maddesine bak).
- Yan yana iki çiçeğin yeri kaydırılarak değiştirilir.
- Yatay veya dikey 3+ aynı çiçek dizisi patlar.
- **Geçersiz hamle:** Eşleşme oluşturmayan takas geri döner ve hamle sayılmaz.
- **Kaskad:** Patlayan çiçeklerin yerine üstekiler düşer, boşluklar yukarıdan yeni çiçeklerle dolar. Yeni eşleşmeler oluşursa zincirleme patlar.
- **Tahta oluşturma:** Başlangıçta hazır eşleşme olmamalı ve en az bir geçerli hamle olmalı.
- **Hamle kalmazsa:** Tahta animasyonla karıştırılır, oyuncu cezalandırılmaz.

## Bölümler
- İlk sürümde **30 bölüm.** (Utku'nun kararıyla 20'den 30'a çıktı.)
- Her bölümün bir hedefi ve hamle sınırı var. Hedef türleri:
  - Belirli bir çiçekten N tane topla ("15 hamlede 20 gül topla")
  - Birden fazla çiçek türünden topla
  - Puan hedefi
  - Engel hedefleri: saksıları kır, sarmaşıkları çöz, toprağı çiçeklendir
- **Bölüm çeşitliliği (engeller ve şekiller):**
  - **Sarmaşık:** Çiçeği sarar; çiçek yer değiştiremez ve düşmez ama eşleşmeye katılabilir. Eşleşmenin parçası olunca ya da yanında bir çiçek patlayınca sarmaşık kopar.
  - **Saksı (1 veya 2 katlı):** Hücrede çiçek yerine durur, düşmez. Yanında bir patlama olunca bir kat kırılır.
  - **Toprak:** Çiçeğin altındaki zemin. Üstündeki çiçek patlayınca çiçeklenir.
  - **Şekilli tahtalar:** Kalp, elmas, kum saati gibi. Engellerin ve boşlukların altındaki hücreler yan sütunlardan çaprazlama dolar.
- Her engel önce kolay bir tanıtım bölümüyle gelir, sonra diğerleriyle karışır. 15 ve 25 nefes bölümleri, 30 final (kalp tahta, hepsi bir arada).
- Zorluk yavaş artar: ilk bölümler öğretici ve çok kolay, sonra hedefler büyür ve hamle sayısı daralır. Hamle sayıları ve yıldız eşikleri simülasyonla ayarlanır (bkz. `Tools/LevelTuner`): bir "sıradan oyuncu" botu baştaki bölümleri ~%99, sonlarda ~%65-70 kazanır.
- **Yıldızlar (1–3):** Kalan hamleye ya da puana göre.
- **Bölüm haritası:** Basit bir dikey yol. Açık, kilitli ve tamamlanmış bölümler net görünsün.
- İlerleme ve yıldızlar telefonda kaydedilir.
- Bölüm verisi kodda değil, ayar dosyalarında (ScriptableObject) tutulur. Yeni bölüm eklemek kod yazmayı gerektirmemeli.

## Görsel kimlik
- **Ana renkler:** kırmızı (kiraz kırmızısı), pembe, **bordo** (onun en sevdiği renk).
- Destek renkleri: krem/kırık beyaz (arka plan, boşluk ve okunabilirlik için), az miktarda yaprak yeşili (çiçek sapları ve yaprakları).
- **Okunabilirlik riski:** Ana palet tek bir renk ailesinden (kırmızı-pembe) oluşuyor, bu yüzden çiçekler sadece renkle ayırt edilemez. Her çiçek türünün **belirgin farklı bir silueti** olmalı (ör. gül, lale, kiraz çiçeği, papatya benzeri, karanfil), renkler de açık-koyu olarak iyice ayrışmalı. Kural: Ekran görüntüsü gri tonlamaya çevrildiğinde bile türler ayırt edilebilmeli.
- Genel his: sıcak, yumuşak, romantik ama çocuksu değil. Yuvarlak köşeler, yumuşak geçişler.

## Asset stratejisi
- **Çiçekler:** Palete tam uyması ve tutarlı görünmesi için vektörel olarak çizilip sprite'a çevrilir. Uygun ücretsiz bir paket bulunursa o da değerlendirilebilir.
- **UI:** Ücretsiz paketler (ör. Kenney), palete göre yeniden renklendirilir.
- **Fontlar:** Google Fonts (OFL lisanslı).
- **Sesler:** Ücretsiz ve ticari kullanıma açık ses paketleri.
- **Lisans kuralı:** Sadece CC0 veya açıkça ticari kullanıma izin veren lisanslar. Her asset'in kaynağı ve lisansı `docs/CREDITS.md` dosyasına yazılır.

## Juice (his ve cila)
- Takas: yumuşak kayma animasyonu. Geçersiz takas: hafif "boing" ile geri dönüş.
- Patlama: çiçek küçülüp kaybolurken yapraklar saçılır, tatlı bir ses çalar.
- Kaskad: çiçekler hafif yaylanarak yerine oturur.
- **Kombo:** Zincirleme her patlamada ses perdesi biraz yükselir. Büyük zincirlerde ekranda kısa bir övgü metni çıkar.
- Bölüm bitişi: yıldızlar tek tek dolar, küçük bir kutlama.
- Hafif titreşim (patlamada ve bölüm bitişinde).

## Kişisel dokunuşlar (Utku'dan detay bekleniyor)
- Oyunun adı ona özel olabilir.
- Bazı bölüm sonlarında ona küçük mesajlar.
- Aranızdaki şakalar veya özel anlar oyuna gizlenebilir.
- **Son bölüm (30) bitince sürpriz ekranı:** "Tüm çiçekleri topladın, artık gerçek çiçeği görebilirsin." Butona basınca ön kamera açılır ve görüntü kalp şeklinde bir çerçevede görünür. Gerçek çiçek odur. Kamera açılamazsa yedek bir mesaj çıkar.
- Bu metinler kodda değil, kolayca düzenlenebilen bir veri dosyasında tutulur. (`Data/PersonalMessages`, Inspector'dan düzenlenir.)

## Teknik
- Unity 6.3 LTS, Universal 2D (URP), Android, dikey 1080x1920.
- **Tahta mantığı görselden ayrı:** Eşleşme bulma, kaskad, karıştırma ve hamle kontrolü Unity'den bağımsız, saf C# sınıflarında yazılır ve **birim testleriyle** doğrulanır. Görsel katman sadece bu mantığın sonuçlarını animasyonla gösterir. Sebep: Match-3'ün kenar durumları çok, test etmeden doğru çalıştığından emin olmak zor.
- Animasyonlar sırayla çalışır: oyuncu, animasyon bitmeden yeni hamle yapamaz.
- Çiçek objeleri havuzlanır (object pooling).

## Kapsam dışı (ilk sürüm)
Özel taşlar (bomba, roket vb.), can/enerji sistemi, reklam, mağaza, online özellikler. Özel taşlar, çekirdek oynanış oturduktan sonra **ilk eklenecek şey** olarak düşünülebilir. (Engeller artık kapsamda; yukarıdaki "Bölüm çeşitliliği" maddesine bakın.)

## Bitti sayılması için
- 30 bölüm baştan sona oynanabiliyor, ilerleme kaydediliyor.
- Eşleşme, kaskad ve karıştırma hatasız çalışıyor (birim testleri geçiyor).
- Çiçekler gri tonlamada bile ayırt edilebiliyor.
- Juice efektleri ve sesler var.
- Kişisel mesajlar ve son bölüm sürprizi yerinde.
- APK onun telefonunda sorunsuz çalışıyor.
