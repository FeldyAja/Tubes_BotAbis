# Tubes Stima - Robocode Tank Royale Bots

## Anggota Kelompok
- [Nama 1] - [NIM]
- [Nama 2] - [NIM]
- [Nama 3] - [NIM]

---

## Deskripsi Singkat Algoritma Greedy Tiap Bot

### 1. GreedyHunter (Bot Utama)
**Strategi:** Selalu mengejar dan menyerang musuh terdekat.  
**Heuristic:** Maksimalkan Bullet Damage dengan memilih firepower optimal berdasarkan jarak (dekat = peluru berat, jauh = peluru ringan).  
**Komponen skor yang dioptimalkan:** Bullet Damage + Bullet Damage Bonus.

### 2. SurvivorBot (Bot Alternatif 1)
**Strategi:** Bertahan hidup selama mungkin dengan menghindari bahaya.  
**Heuristic:** Maksimalkan Survival Score dengan hanya menyerang saat energi > 50 dan bergerak zigzag untuk menghindari peluru.  
**Komponen skor yang dioptimalkan:** Survival Score + Last Survival Bonus.

### 3. RammerBot (Bot Alternatif 2)
**Strategi:** Menabrak musuh yang energinya sudah rendah.  
**Heuristic:** Maksimalkan Ram Damage dengan memilih target musuh paling lemah (energi < 40) untuk ditabrak, musuh kuat ditembak dulu untuk dilemahkan.  
**Komponen skor yang dioptimalkan:** Ram Damage + Ram Damage Bonus.

### 4. SniperBot (Bot Alternatif 3)
**Strategi:** Bersembunyi di sudut arena dan menembak dengan peluru berat.  
**Heuristic:** Maksimalkan Bullet Damage Bonus dengan selalu menggunakan firepower tinggi (3.0) untuk membunuh musuh dan mendapatkan bonus 20%.  
**Komponen skor yang dioptimalkan:** Bullet Damage Bonus.

---

## Requirements

- .NET 6.0 SDK
- Robocode Tank Royale Game Engine (versi modifikasi asisten)
- Java JDK 21

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
