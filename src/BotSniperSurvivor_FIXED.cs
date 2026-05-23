using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System;
using System.Drawing;

/// <summary>
/// BotSniperSurvivor
/// Versi perbaikan:
/// - Tidak memakai SetAhead, tapi SetForward / SetBack.
/// - Tidak memakai e.Distance dan e.Bearing karena tidak tersedia.
/// - Distance dan bearing dihitung manual dari posisi bot dan posisi musuh.
/// </summary>
public class BotSniperSurvivor : Bot
{
    private int arahGerak = 1;

    static void Main(string[] args)
    {
        new BotSniperSurvivor().Start();
    }

    BotSniperSurvivor() : base(BotInfo.FromFile("BotSniperSurvivor.json")) { }

    public override void Run()
    {
        BodyColor = Color.DarkBlue;
        TurretColor = Color.Black;
        RadarColor = Color.Cyan;
        BulletColor = Color.Red;
        ScanColor = Color.White;

        AdjustGunForBodyTurn = true;
        AdjustRadarForGunTurn = true;
        AdjustRadarForBodyTurn = true;

        SetForward(180);

        while (IsRunning)
        {
            SetTurnRadarRight(360);
            HindariTembok();
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Hitung sudut absolut ke musuh
        double absoluteBearing = BearingTo(e.X, e.Y);

        // Hitung bearing relatif terhadap arah badan bot
        double relativeBearing = NormalisasiSudut(absoluteBearing - Direction);

        // Hitung jarak manual karena e.Distance tidak tersedia
        double distance = HitungJarak(e.X, e.Y);

        // =========================
        // 1. Radar Lock
        // =========================
        double radarTurn = NormalisasiSudut(absoluteBearing - RadarDirection);
        SetTurnRadarRight(radarTurn * 2);

        // =========================
        // 2. Evasive Movement / Strafe
        // =========================
        // Bergerak menyamping dari musuh agar tidak mudah ditembak
        double moveAngle = relativeBearing + 90;

        if (distance < 180)
        {
            moveAngle += 25 * arahGerak;
        }

        SetTurnRight(moveAngle);

        if (Math.Abs(DistanceRemaining) < 20)
        {
            arahGerak *= -1;
            SetForward(180 * arahGerak);
        }

        // =========================
        // 3. Gun Aim
        // =========================
        double gunTurn = NormalisasiSudut(absoluteBearing - GunDirection);
        SetTurnGunRight(gunTurn);

        // =========================
        // 4. Dynamic Firepower
        // =========================
        double power;

        if (distance < 100)
        {
            power = 3.0;
        }
        else if (distance < 300)
        {
            power = 2.0;
        }
        else
        {
            power = 1.0;
        }

        if (Energy < 25)
        {
            power = Math.Min(power, 1.0);
        }

        if (Energy < 10)
        {
            power = 0.5;
        }

        if (GunHeat == 0 && Math.Abs(gunTurn) < 10)
        {
            Fire(power);
        }

        Go();
    }

    public override void OnHitWall(HitWallEvent e)
    {
        arahGerak *= -1;
        SetBack(150);
        SetTurnRight(60);
        Go();
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Jangan terlalu lama kontak badan dengan musuh
        SetBack(120);

        if (GunHeat == 0 && Energy > 20)
        {
            Fire(3.0);
        }

        Go();
    }

    private void HindariTembok()
    {
        double margin = 80;

        if (X < margin || X > ArenaWidth - margin || Y < margin || Y > ArenaHeight - margin)
        {
            arahGerak *= -1;
            SetBack(120);
            SetTurnRight(70);
        }
    }

    private double HitungJarak(double musuhX, double musuhY)
    {
        double dx = musuhX - X;
        double dy = musuhY - Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }

    private double NormalisasiSudut(double angle)
    {
        while (angle > 180)
        {
            angle -= 360;
        }

        while (angle < -180)
        {
            angle += 360;
        }

        return angle;
    }
}
