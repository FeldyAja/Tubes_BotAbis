using System;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;

public class BotSniper : Bot
{
    // Konfigurasi gaya main sniper
    const double JarakBahaya = 180;       // Kalau musuh lebih dekat dari ini, langsung menjauh
    const double JarakAman = 320;         // Jarak minimal yang dianggap aman
    const double JarakIdeal = 430;        // Jarak favorit untuk nembak sambil strafing
    const double JarakTembakMaks = 700;   // Jangan buang peluru kalau musuh terlalu jauh

    int waktuHilang = 0;
    int waktuLepasTembok = 0;
    int arahStrafe = 1;

    static void Main(string[] args)
    {
        new BotSniper().Start();
    }

    public BotSniper() : base(BotInfo.FromFile("BotSniper.json")) { }

    public override void Run()
    {
        BodyColor = Color.SteelBlue;
        TurretColor = Color.Black;
        RadarColor = Color.Cyan;
        BulletColor = Color.LightBlue;
        ScanColor = Color.White;

        // Radar dan gun bisa bergerak independen dari badan.
        AdjustRadarForGunTurn = true;
        AdjustGunForBodyTurn = true;

        while (IsRunning)
        {
            if (waktuLepasTembok > 0) waktuLepasTembok--;
            waktuHilang++;

            // Kalau target hilang, radar muter penuh dan bot bergerak pelan agar tidak statis.
            if (waktuHilang > 8)
            {
                SetTurnRadarRight(360);
                SetTurnRight(25 * arahStrafe);
                SetForward(60);
            }

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        waktuHilang = 0;

        double arahMusuhMutlak = DirectionTo(e.X, e.Y);
        double sudutKeMusuh = BearingTo(e.X, e.Y);
        double jarak = DistanceTo(e.X, e.Y);

        // Radar lock: lewatkan sedikit dari posisi musuh agar radar tetap nempel.
        double radarTurn = CalcDeltaAngle(RadarDirection, arahMusuhMutlak);
        radarTurn += radarTurn >= 0 ? 30 : -30;
        SetTurnRadarRight(radarTurn);

        // Gun lock: sniper wajib mengunci meriam dulu sebelum menembak.
        double gunTurn = CalcDeltaAngle(GunDirection, arahMusuhMutlak);
        SetTurnGunRight(gunTurn);

        AturGerakanSniper(sudutKeMusuh, jarak);
        TembakKalauAman(jarak, gunTurn);
    }

    void AturGerakanSniper(double sudutKeMusuh, double jarak)
    {
        if (waktuLepasTembok > 0)
        {
            // Saat kena tembok, belok tajam dan maju untuk keluar dari sudut map.
            SetTurnRight(90 * arahStrafe);
            SetForward(130);
            return;
        }

        if (jarak < JarakBahaya)
        {
            // Musuh terlalu dekat: hadap menjauh secara diagonal, lalu kabur.
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 150));
            SetForward(180);
        }
        else if (jarak < JarakAman)
        {
            // Masih kurang aman: mundur menyamping supaya sulit ditembak.
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 110 * arahStrafe));
            SetForward(140);
        }
        else if (jarak > JarakIdeal + 120)
        {
            // Terlalu jauh: mendekat sedikit, tapi tetap miring agar tidak lurus ke musuh.
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 25 * arahStrafe));
            SetForward(100);
        }
        else
        {
            // Zona ideal sniper: strafe melingkar sambil menjaga jarak.
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 90 * arahStrafe));
            SetForward(120);
        }
    }

    void TembakKalauAman(double jarak, double gunTurn)
    {
        if (jarak > JarakTembakMaks) return;
        if (Math.Abs(gunTurn) > 8) return;

        double power;

        if (jarak < 220) power = 3.0;       // Musuh dekat, damage maksimal
        else if (jarak < 430) power = 2.2;  // Jarak ideal sniper
        else power = 1.4;                   // Jauh, hemat energi

        SetFire(power);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Kalau tertabrak/menabrak, jangan duel jarak dekat. Langsung menjauh.
        arahStrafe *= -1;
        SetTurnRight(NormalizeRelative(BearingTo(e.X, e.Y) + 170));
        SetForward(170);
        SetFire(3.0);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Ganti arah strafe supaya tidak mengulang pola yang sama ke tembok.
        arahStrafe *= -1;
        waktuLepasTembok = 12;
    }

    static double NormalizeRelative(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}