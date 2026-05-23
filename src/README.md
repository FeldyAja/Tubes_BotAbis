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

**Cara Menjalankan Bot:**
**Windows**
Di bawah ini kasus sample bot bawaan github
Build Game Engine 
Buka Powershell, lalu jalankan  :

cd tank-royale-0.30.0
./gradlew :gui-app:clean
./gradlew :gui-app:build


Jalankan Game Engine
Buka PowerShell baru, lalu jalankan :

_cd tank-royale-0.30.0
_java -jar ./gui-app/build/libs/robocode-tankroyale-gui-0.30.0.jar__


Build Bot
Buka PowerShell baru, masuk ke folder bot, lalu jalankan : 

_cd sample-bots/csharp/NamaBot
_dotnet clean_
_dotnet restore_
_dotnet build_
_

Jalankan Bot 
Masih di terminal yang sama, jalankan: 

_$env:SERVER_SECRET = "isi_dengan_secret_dari_server.properties"
_dotnet run_
_
Mulai Pertandingan di GUI
1. Di GUI Robocode, klik Config → Bot Root Directories
2. Klik Add dan arahkan ke folder sample-bots/csharp 
3. Klik OK 
4. Klik Battle → Start Battle Di bagian Joined Bots, pilih bot yang sudah terhubung
5. Klik Add All lalu Start Battle


	


**Linux**
Di bawah ini kasus bukan sample bots bawaan github
Build Game Engine
Buka Vscode terminal ubuntu, lalu jalankan  :

cd tank-royale-0.30.0
_./gradlew :gui-app:clean
_./gradlew :gui-app:build
__

Jalankan Game Engine
Buka terminal baru, lalu jalankan :

_cd tank-royale-0.30.0_
_java -jar ./gui-app/build/libs/robocode-tankroyale-gui-0.30.0.jar

_Lalu mulai local server

Build Bot
Buka terminal baru, masuk ke folder bot, jalankan:
dotnet clean
dotnet restore
dotnet build

Jalankan Bot
Masih di terminal yang sama, jalankan:
_export SERVER_SECRET=isi_dengan_secret_dari_server.properties_
_chmod +x NamaFolderBot.sh_
_./NamaFolderBot.sh_

Mulai Pertandingan di GUI
Bila saat start local server hasilnya tidak merah, melainkan success hijau, maka:
Karena file sh sudah di jalankan, cek di Start Battle pada Game Engine
Bila berhasil, maka ada nama bot di kotak local/remote server di kiri bawah
Add bot bot tersebut
Lalu start battle untuk memulai pertempuran antar bot yang dipilih

