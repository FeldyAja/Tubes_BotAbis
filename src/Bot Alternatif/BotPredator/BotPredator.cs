using System;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;

public class BotPredator : Bot
{
    // Timer untuk memutar setir saat menggesek tembok
    int waktuSwerveTembok = 0;
    
    // Timer jika kehilangan jejak musuh
    int waktuHilang = 0;

    static void Main(string[] args)
    {
        new BotPredator().Start();
    }

    public BotPredator() : base(BotInfo.FromFile("BotPredator.json")) { }

    public override void Run()
    {
        BodyColor = Color.OrangeRed;
        TurretColor = Color.DarkRed;
        RadarColor = Color.White;
        BulletColor = Color.Yellow;
        ScanColor = Color.Red;

        AdjustRadarForGunTurn = true;
        AdjustGunForBodyTurn = true;

        while (IsRunning)
        {
            // Kurangi timer manuver tembok
            if (waktuSwerveTembok > 0) waktuSwerveTembok--;
            
            waktuHilang++;

            // Kalo sudah 10 putaran gak lihat musuh, putar radar 360 derajat nyari mangsa
            if (waktuHilang > 10)
            {
                SetTurnRadarRight(360);
                SetForward(50); // Maju pelan biar gak diam saja
            }

            Go(); // Eksekusi pergerakan
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        waktuHilang = 0; // Musuh ketemu, Reset timer.

        double arahMusuhMutlak = DirectionTo(e.X, e.Y);
        double sudutKeMusuh = BearingTo(e.X, e.Y);
        double jarak = DistanceTo(e.X, e.Y);

        // WIPER SCAN 90 DERAJAT DEPAN ONLY
        double radarTurn = CalcDeltaAngle(RadarDirection, arahMusuhMutlak);
        radarTurn += (radarTurn >= 0 ? 45 : -45); 
        SetTurnRadarRight(radarTurn);

        // KUNCI MERIAM
        SetTurnGunRight(CalcDeltaAngle(GunDirection, arahMusuhMutlak));

        // PERGERAKAN (HANYA GAS MAJU)
        if (waktuSwerveTembok > 0)
        {
            // Kalo lagi nyenggol tembok, abaikan arah musuh sebentar.
            // Banting setir 45 derajat biar tubuh bebas dari tembok.
            // Gas tetap maju (Tidak ada SetBack)
            SetTurnRight(45);
            SetForward(100); 
        }
        else
        {
            // Kalo area aman, langsung kunci tubuh ke musuh dan gas
            SetTurnRight(sudutKeMusuh);
            SetForward(jarak + 50); // Gas tabrak musuh, lebih jarak agar menembus/melewati musuh
        }

        // Tembak musuh
        if (Math.Abs(CalcDeltaAngle(GunDirection, arahMusuhMutlak)) < 15)
        {
            if (jarak < 100) SetFire(3.0); // Tabrak & eksekusi jarak dekat
            else SetFire(1.5);             // Nyicil damage dari jauh
        }
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // STUNLOCK RAMMER (TABRAK BERULANG-ULANG)
        // Kunci lagi tubuh ke arah musuh yang ditabrak
        SetTurnRight(BearingTo(e.X, e.Y));
        
        // Gak ada mundur sama sekali, Terus gas ke depan
        SetForward(100); 
        
        // tembak peluru maksimal 
        SetFire(3.0);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Beri waktu 10 putaran agar OnScannedBot membelokkan tubuh menjauhi tembok
        // tanpa perlu mengerem atau memundurkan tank.
        waktuSwerveTembok = 10;
    }
}