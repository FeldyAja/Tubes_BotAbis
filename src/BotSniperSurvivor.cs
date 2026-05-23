using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System;
using System.Drawing;

/// <summary>
/// BotSniperSurvivor
/// Versi upgrade dari BotSniper untuk mode banyak musuh.
/// Fokus strategi:
/// 1. Survive lebih lama dengan gerakan strafe / zig-zag.
/// 2. Tetap menghasilkan damage stabil dengan firepower dinamis.
/// 3. Menghindari tembok agar tidak stuck.
/// 4. Tidak terlalu sering ram karena ram berbahaya saat musuh banyak.
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

        // Radar dan gun dibuat independen dari gerakan body
        AdjustGunForBodyTurn = true;
        AdjustRadarForGunTurn = true;
        AdjustRadarForBodyTurn = true;

        // Gerakan awal agar bot tidak diam
        SetAhead(150);

        while (IsRunning)
        {
            // Radar terus menyapu untuk mencari musuh
            SetTurnRadarRight(360);

            // Anti-wall sederhana
            HindariTembok();

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // =========================
        // 1. Radar Lock
        // =========================
        // Kunci radar ke arah musuh agar target tidak mudah hilang
        double absoluteBearing = BearingTo(e.X, e.Y);
        double radarTurn = NormalizeRelativeAngle(absoluteBearing - RadarDirection);
        SetTurnRadarRight(radarTurn * 2);

        // =========================
        // 2. Evasive Movement
        // =========================
        // Bergerak menyamping dari musuh, bukan maju lurus ke musuh
        // Ini membuat bot lebih sulit ditembak
        double moveAngle = e.Bearing + 90;

        // Tambahkan sedikit variasi agar tidak terlalu mudah ditebak
        if (e.Distance < 180)
        {
            moveAngle += 25 * arahGerak;
        }

        SetTurnRight(moveAngle);

        // Bolak-balik jika gerakan hampir selesai
        if (Math.Abs(DistanceRemaining) < 20)
        {
            arahGerak *= -1;
            SetAhead(180 * arahGerak);
        }

        // =========================
        // 3. Gun Aim
        // =========================
        // Arahkan gun ke posisi musuh
        double gunTurn = NormalizeRelativeAngle(absoluteBearing - GunDirection);
        SetTurnGunRight(gunTurn);

        // =========================
        // 4. Dynamic Firepower
        // =========================
        // Semakin dekat musuh, semakin besar power.
        // Jika energi rendah, jangan boros.
        double power;

        if (e.Distance < 100)
        {
            power = 3.0;
        }
        else if (e.Distance < 300)
        {
            power = 2.0;
        }
        else
        {
            power = 1.0;
        }

        // Jangan habiskan energi sendiri
        if (Energy < 25)
        {
            power = Math.Min(power, 1.0);
        }

        if (Energy < 10)
        {
            power = 0.5;
        }

        // Tembak hanya kalau gun sudah cukup mengarah
        if (GunHeat == 0 && Math.Abs(gunTurn) < 10)
        {
            Fire(power);
        }

        Go();
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Jika menyentuh tembok, langsung balik arah
        arahGerak *= -1;
        SetBack(150);
        SetTurnRight(60);
        Go();
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Di mode banyak musuh, jangan terlalu lama menabrak musuh.
        // Mundur sedikit agar tidak jadi target empuk.
        if (e.IsRammed)
        {
            SetBack(120);
        }
        else
        {
            SetAhead(80);
        }

        // Jika sangat dekat, tembakan besar cukup aman
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

    private double NormalizeRelativeAngle(double angle)
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
