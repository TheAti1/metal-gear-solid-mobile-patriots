<div align="center">
<img src="assets/patriots.jpg" width="110" alt="Patriots Çeviri">

# Metal Gear Solid Mobile · Patriots Edition

**Türkçe / İngilizce başlatıcı ve oyun · Canlı tuş yazıları · Discord Rich Presence**

[English](README.md) · [Sürümler](../../releases) · [Derleme rehberi](docs/BUILD.md)

<img src="docs/screenshots/tr/home.png" width="960" alt="Türkçe Patriots başlatıcısı">
</div>

## Neler var?

- Başlatıcının sağ üstünden **Türkçe / English** seçimi. Arayüz hemen değişir; oyun seçilen dilde bir sonraki açılışta başlar.
- Oyun içindeki tuş yazıları klavye veya kontrolcüye ve güncel atamalara göre değişir.
- Xbox, PlayStation ve Nintendo tuş adları; oyun açıkken tuş atamalarını değiştirme.
- Tam ekran, geniş ekran kamera, görüş alanı, kenar yumuşatma, çizim ölçeği ve doku seçenekleri.
- Discord'da gerçek oda, Codec / ara sahne / duraklatma durumu, oturum süresi ve görseller.
- İki dilde ortak kayıtlar, otomatik kayıt yedekleri ve dosya doğrulama.

## Oynamak

1. [Sürümler](../../releases) bölümündeki Windows paketini indir ve klasörün tamamını çıkar.
2. **Metal Gear Solid Mobile.exe** dosyasını aç.
3. Dili seç, **OYNA / PLAY** düğmesine bas.

Discord desteği için masaüstü Discord uygulaması açık olmalıdır. **Ses / Discord** bölümünden etkinlik ve oda paylaşımı ayrı ayrı kapatılabilir.

| İşlev | Klavye | Xbox |
|---|---|---|
| Hareket | WASD / yön tuşları | Sol analog / yön tuşları |
| Eylem / ateş | Space / J | A / RT |
| Birinci şahıs | C | Y / LT |
| Codec / duraklatma | Q / Tab | Start / X |
| Ekipman / geri | E / Esc | B / RB |
| Kamera modu | R / Num * | LB |

Atamalar başlatıcıdan değiştirilebilir. **F1** motor ayarlarını, **F12** ekran görüntüsü işlevini açar.
Mevcut yerel kayıtlar iki dilde de kullanılabilir; dağıtım ZIP'i kişisel kayıt içermez.

## Proje yapısı

`launcher/`: WPF başlatıcı ve Discord bağlantısı. `runtime/`: değiştirilmiş mgsm motoru.
`localization/`: tamamlanmış Türkçe JSON. `tools/`: arşiv ve font araçları. `docs/`: derleme ve testler.

Altyapı, özgün ARM oyun kodunu Unicorn ile çalıştırır; PC grafik, ses ve girişini mgsm sağlar.
Kaynak kodundan derlemek için [derleme rehberini](docs/BUILD.md) kullan.

## Emeği geçenler

Özgün oyun: Konami / Kojima Productions / Ideaworks3D (2008).
Altyapı: [LeithSilver/mgsm](https://github.com/LeithSilver/mgsm), GPL-2.0-or-later.
Türkçe çeviri, başlatıcı ve entegrasyon: [Patriots Çeviri](https://www.patriotsceviri.com.tr/).
Başlatıcı ve depo ekleri GPL-3.0-or-later; bileşenlerin özgün lisansları korunmuştur.
