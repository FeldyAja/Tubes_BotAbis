# Tubes Stima - Robocode Tank Royale Bots

## Anggota Kelompok
- [Feldy] - [124140083]
- [Jojo Marpaung] - [124140191]
- [Andreas Silalahi] - [124140148]

---

## Deskripsi Singkat Algoritma Greedy Tiap Bot

Bot 1: BotSweeper (Main Bot)
Sifat Greedy: Greedy pada coverage - selalu bergerak maju sejauh mungkin sambil memutar gun 360 derajat untuk memaksimalkan area yang dipindai dan ditembak, setiap scan akan menembak seketika.
Heuristic: Maksimalkan Bullet Damage dengan DPS (Damage Per Second) konstan menggunakan tembakan firepower 1.5 setiap kali musuh terdeteksi tanpa kalkulasi apapun, sambil terus berpatroli melingkar.

Bot 2: BotPredator
Sifat Greedy: Greedy pada target locking — bot langsung mengunci target pertama yang ditemukan dan terus mengejarnya sampai mati, tanpa mempertimbangkan apakah ada target lain yang lebih menguntungkan (lebih dekat atau lebih lemah).
Heuristik: Maksimalkan Ram Damage + Bullet Damage dengan strategi pursue-and-ram yaitu:
Selalu maju ke arah target terkunci sejauh 10000 piksel (gas penuh)
Menembak firepower 1.5 saat mengejar untuk cicil damage
Saat berhasil menabrak, mundur 1 piksel lalu tembak firepower maksimal 3.0 untuk eksekusi
Radar sweep 90 derajat di depan agar fokus pada target yang sedang dikejar, bukan menyapu arena secara luas

Bot 3: BotSentinel
Sifat Greedy: Greedy pada posisi - langsung mencari dan menempel di tembok terdekat sebagai posisi bertahan optimal, lalu menyapu area terbuka dengan gun secara terus-menerus.
Heuristic: Maksimalkan Survival Score dengan meminimalkan area yang bisa diserang (menempel tembok) sambil memaksimalkan scan coverage dengan rotasi gun 360 derajat. Saat kontak langsung dengan musuh, tembak firepower maksimal (3.0).
Bot 4: SniperBot (Alternatif 3)
Strategi Greedy : Bersembunyi di sudut arena dan menembak dengan peluru berat dari jauh.
Heuristic : Maksimalkan Bullet Damage Bonus dengan selalu menggunakan firepower maksimal (3.0) dan predictive aiming untuk akurasi tinggi, sambil meminimalkan risiko terkena serangan balik.

---

## Requirements

- .NET 10.0 SDK
- Robocode Tank Royale Game Engine (versi modifikasi asisten)
- Java JDK 17

## Cara Build & Menjalankan Bot

### Build bot
```bash
cd [nama-bot]
dotnet build
dotnet publish -c Release
```

### Menjalankan bot
1. Jalankan game engine Tank Royale terlebih dahulu
2. Di terminal, masuk ke folder output bot:
```bash
cd [nama-bot]/bin/Release/net6.0/publish
dotnet [NamaBot].dll
```
3. Di GUI Tank Royale, tambahkan bot melalui menu Battle > Start Battle

---

## Struktur Repository
```
src/
├── main-bot/
│   └── GreedyHunter/
├── alternative-bots/
│   ├── SurvivorBot/
│   ├── RammerBot/
│   └── SniperBot/
doc/
└── laporan.pdf
README.md
```
