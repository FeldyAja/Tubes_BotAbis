using System;
using Robocode.TankRoyale.BotApi.Graphics;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

    public class BotUtama : Bot
    {
        bool movingForward = true;

        static void Main(string[] args)
        {
            new BotUtama().Start();
        }

        // Memanggil konfigurasi dari BotUtama.json
        public BotUtama() : base(BotInfo.FromFile("BotUtama.json")) { }

        public override void Run()
        {
            // Set warna identitas kelompok
            BodyColor = Color.Black;
            TurretColor = Color.DarkRed;
            RadarColor = Color.Gold;
            
            movingForward = true;

            while (IsRunning)
            {
                // GREEDY SCANNING: Selalu putar radar dan meriam untuk mencari musuh
                // Mengikuti pola Crazy.cs yang menggunakan perulangan gerakan dasar
                SetForward(40000); 
                movingForward = true;
                
                // Berputar sedikit demi sedikit untuk membentuk pola lingkaran besar
                TurnRight(45);
                TurnGunRight(360); // Putar meriam 360 derajat untuk memindai
            }
        }

        // Fungsi ketika melihat bot musuh
        public override void OnScannedBot(ScannedBotEvent e)
        {
            // GREEDY ATTACK: Langsung tembak dengan kekuatan sedang
            // Sederhana seperti sample Crazy.cs
            Fire(1.5);
        }

        // Fungsi ketika menabrak dinding
        public override void OnHitWall(HitWallEvent e)
        {
            // GREEDY SURVIVAL: Balik arah secepatnya
            ReverseDirection();
        }

        // Fungsi ketika bertabrakan dengan bot lain
        public override void OnHitBot(HitBotEvent e)
        {
            // Jika kita menabrak musuh, balik arah agar tidak terjepit
            if (e.IsRammed)
            {
                ReverseDirection();
            }
        }

        // Fungsi pembantu untuk membalikkan arah (diambil dari logika Crazy.cs)
        public void ReverseDirection()
        {
            if (movingForward)
            {
                SetBack(40000);
                movingForward = false;
            }
            else
            {
                SetForward(40000);
                movingForward = true;
            }
        }
    }
