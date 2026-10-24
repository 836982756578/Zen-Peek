# ☯️ Zen Peek — Geliştirici & Güncelleme Rehberi

Bu dosya, **Zen Peek** projesinde kod değişiklikleri yaptığınızda projenin nasıl derleneceğini, GitHub'daki dosyaların nasıl güncelleneceğini ve token yönetimiyle ilgili tüm adımları açıklar.

---

## 📂 Klasör Yapısı

- `Source/` : Uygulamanın tüm C# kaynak kodları bu klasördedir.
- `app.ico` : Uygulamanın 5 çözünürlüklü (16px - 256px) özel Yin-Yang vektör ikonu.
- `ZenPeek.exe` : Kodların derlenmiş, sıfır kurulumlu, taşınabilir çalıştırılabilir dosyası.
- `build.ps1` : Projeyi yerel olarak Windows .NET derleyicisi (`csc.exe`) ile derleyen script.
- `guncelle_ve_yayinla.bat` : Kodu derleyip tek tıkla GitHub deposuna ve Release'e yükleyen otomatik araç.
- `README.md` : GitHub'da kullanıcılara gösterilen tanıtım sayfası.
- `LICENSE` : MIT Açık Kaynak Lisansı (Serkan ŞENTÜRK).

---

## 🚀 Değişiklik Yaptığınızda Nasıl Güncellenir?

Kodlarda veya arayüzde bir değişiklik yaptığınızda GitHub'daki dosyaları ve indirilebilir `.exe` dosyasını güncellemek için **3 kolay yolunuz** vardır:

### 1. Yol (En Pratik — Antigravity Üzerinden):
Antigravity sohbetine sadece şunu yazmanız yeterlidir:
> *"**Zen Peek** uygulamasına [istediğiniz özellik] ekle, derle ve GitHub'a güncelle."*
- **Token yazmanız gerekmez:** GitHub token'ınız sisteminizde kayıtlıdır, Antigravity bunu otomatik okur.
- Antigravity kodu günceller, `build.ps1` ile derler, Git'e commit atar ve GitHub Release'deki `ZenPeek.exe` dosyasını otomatik olarak yenisiyle değiştirir.

### 2. Yol (Tek Tıkla — Çift Tıklayarak):
Kodları düzenledikten sonra bu klasördeki **`guncelle_ve_yayinla.bat`** dosyasına çift tıklayın:
1. Kodu otomatik olarak derler.
2. Yeni `ZenPeek.exe` dosyasını üretir.
3. Git ile GitHub'a pushlar.
4. GitHub Release sayfasındaki eski `.exe`'yi kaldırıp güncel olanı yükler.

### 3. Yol (Sadece Yerel Olarak Derlemek İsterseniz):
GitHub'a yüklemeden sadece bilgisayarınızda test etmek için:
- PowerShell'de `.\build.ps1` komutunu çalıştırın. Yeni `ZenPeek.exe` hemen üretilir.

---

## 🔑 GitHub Token'ı Her Seferinde Girmem Gerekiyor mu?

**HAYIR! Kesinlikle gerekmez.**

- GitHub token'ınız Windows kullanıcı ortam değişkenlerine (`GITHUB_TOKEN`) kalıcı olarak işlenmiştir.
- Ayrıca `ZenSuite\token.cfg` dosyasında güvenli bir yerel kopyası mevcuttur (`.gitignore` ile korunur, asla internete yüklenmez).
- İster yeni bir proje ekleyin, ister Antigravity ile yeni bir sohbet başlatın; token sistemden otomatik olarak çekilir. Prompt'a şifre veya token yazmanıza gerek yoktur.

---

## 🌐 GitHub Bağlantıları

- **Depo Adresi:** [https://github.com/836982756578/Zen-Peek](https://github.com/836982756578/Zen-Peek)
- **En Son Sürüm & İndirme:** [https://github.com/836982756578/Zen-Peek/releases/latest](https://github.com/836982756578/Zen-Peek/releases/latest)
- **Geliştirici:** Serkan ŞENTÜRK ([@836982756578](https://github.com/836982756578))
