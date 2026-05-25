using System;
using Robocode.TankRoyale.BotApi.Graphics;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class BotSweeper : Bot
{
    bool movingForward = true;

    static void Main(string[] args)
    {
        new BotSweeper().Start();
    }

    public BotSweeper() : base(BotInfo.FromFile("BotSweeper.json")) { }

    public override void Run()
    {
        // Kosmetik Cihuy bot
        BodyColor = Color.Black;
        TurretColor = Color.DarkRed;
        RadarColor = Color.Gold;
        
        movingForward = true;

        while (IsRunning)
        {
            // --- GREEDY SCANNING ---
            // Eksekusi pergerakan lurus skala besar
            SetForward(40000); 
            movingForward = true;
            
            // Belok secara bertahap untuk patroli rute melingkar
            TurnRight(45);

            // Putar meriam 360 derajat untuk menyapu seluruh sudut arena
            TurnGunRight(360); 
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // --- GREEDY ATTACK ---
        // Eksekusi tembakan instan tanpa kalkulasi jarak apapun untuk DPS maksimal
        Fire(1.5);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // --- GREEDY SURVIVAL ---
        // Menghindari status terjebak (stuck) di tembok dengan membalikkan arah kemudi
        ReverseDirection();
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Cegah laras tersangkut bodi lawan jika terjadi benturan proaktif
        if (e.IsRammed)
        {
            ReverseDirection();
        }
    }

    // Fungsi pembantu: Pembalik arah laju tank (Maju <-> Mundur)
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