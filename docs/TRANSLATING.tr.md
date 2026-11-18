# Yeni dil ekleme

5.1 sürümünde başlatıcının **DİLLER → Dil paketi içe aktar** bölümünden topluluk çevirisi kurulabilir. İngilizce ve Türkçe hazır resmî dillerdir. Brezilya Portekizcesi dosyaları şu an **boş çeviri şablonudur**; tamamlanan çeviri incelendikten sonra üçüncü resmî dil olarak paketlenecek.

## Çevirmene gönderilecekler

- [Launcher JSON'u](../localization/launcher/pt-BR.launcher.json): **294 metin**. Yeni dil ekranı ve ayarlar dâhil.
- [Oyun JSON'u](../localization/game/pt-BR.game.json): **1.936 kayıt**; oyun metinleri, yükleme yazısı, PC tuşları ve Discord oda/durum metinleri.
- Sürüm sayfasındaki **Translation Kit**: ayrı `launcher/` ve `game/` klasörleri, 14 orijinal İngilizce DAT tablosu, orijinal/genişletilmiş font grupları ve font araçları.

Dosyalarda dil bilgileri hazır: `language: pt-BR`, `native_name: Português (Brasil)`, `culture: pt-BR`. Çevirmen `author` alanına adını yazabilir. Başka diller için bu üç dil alanı değiştirilir.

Kayıtların yalnızca **`text`** alanını çevirin. `source` İngilizce referanstır. Kimlikleri, dosya yollarını ve teknik alanları değiştirmeyin. `{pc:a}`, `{pc:c}`, `{0}`, `%s` gibi işaretleri ve satır sonlarını koruyun. `{pc:...}` oyun sırasında atanmış klavye/gamepad tuşuna dönüşür. Boş `text` alanları İngilizce kullanır.

Oyunda kullanılan 1.814 metne ek olarak 48 PC/Discord metni vardır. Kalan 70 eski N-Gage telefon/kurulum kaydı referans içindir; PC sürümünde kullanılmaz. `editable: false` olan 4 teknik kayıt çevrilmez.

## Kurulum

Oyunu kapatıp launcher JSON'unu ve oyun JSON'unu ayrı ayrı içe aktarın. Sırası önemli değildir; ikisinde de aynı dil kodunu kullanın. Sonradan aynı JSON'u tekrar içe aktarmak diğer bileşeni korur. Yalnızca launcher çevirisi kurulursa oyun İngilizce açılır; yalnızca oyun çevirisi kurulursa başlatıcı İngilizce olur. Yeni dil üstteki seçicide görünür; oyun dili sonraki açılışta uygulanır. Kayıtlar, kontroller, dinamik tuşlar ve Discord desteği korunur.

İki dosya istenirse ZIP'in kökünde `launcher.json` ve `game.json` adlarıyla birlikte verilebilir. İzin verilen isteğe bağlı font yolları ve ayrıntılı teknik biçim için [İngilizce rehbere](TRANSLATING.md) bakın.

Tam oyun paketinde çevirmen dosyaları `Translations/launcher/` ve `Translations/game/`, kurulan JSON'lar `Arayuz/Languages/<dil>/`, oyuna dönüştürülen tablolar `Oyun/languages/<dil>/` altında bulunur. Ortak oyun dosyaları ve kayıtlar `Oyun/game/` altında kalır. Ham oyun varlıkları Git geçmişine eklenmez; çeviri kitinde ayrı sunulur.

## Çevirmen DAT dosyalarını zaten çevirdiyse

`tools/game_pack_from_dat.py` mevcut çevrilmiş `data/localisation` tablolarını yeni oyun JSON'una dönüştürür. Kullanım örneği [İngilizce rehberde](TRANSLATING.md#already-translated-the-dat-files) bulunur. Araç eksik/çakışan kayıtları ve dinamik tuş işareti gerektiren eski telefon talimatlarını `.review.json` içine yazar; bunlar elle kontrol edilir. Oynamak ve içe aktarmak için Python gerekmez.

Ana fontlar Portekizce Latin harflerini içerir; küçük HUD/Codec fontları, uzun metinler ve satır düzeni oyun içinde ayrıca kontrol edilmelidir. Diğer alfabeler için font çizimi veya ek motor çalışması gerekebilir.

Çevirmen tamamladığında **launcher JSON'unu ve oyun JSON'unu** göndersin. Varsa çevirdiği orijinal DAT dosyaları karşılaştırma için de alınabilir. Gelen gerçek çeviri test edilip onaylandıktan sonra üçüncü resmî dil olarak sürüme eklenir.
