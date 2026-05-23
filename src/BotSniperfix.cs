using System;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;

public class BotSniper : Bot
{
    // Jarak utama gaya sniper
    const double JarakBahaya = 230;        // Musuh masuk sini = wajib mundur
    const double JarakAman = 330;          // Di bawah ini masih harus bikin jarak
    const double JarakIdealMin = 380;      // Mulai zona ideal sniper
    const double JarakIdealMax = 560;      // Akhir zona ideal sniper
    const double JarakTembakMaks = 760;    // Jangan buang peluru terlalu jauh

    int waktuHilang = 0;
    int waktuLepasTembok = 0;
    int arahStrafe = 1;
    int tickGerak = 0;

    // Data scan sebelumnya untuk prediksi sederhana saat musuh bergerak/muter
    bool punyaDataMusuhSebelumnya = false;
    double musuhXTerakhir = 0;
    double musuhYTerakhir = 0;

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

        AdjustRadarForGunTurn = true;
        AdjustGunForBodyTurn = true;

        while (IsRunning)
        {
            if (waktuLepasTembok > 0) waktuLepasTembok--;
            waktuHilang++;
            tickGerak++;

            // Ganti arah gerak berkala agar tidak orbit terus-terusan dengan pola yang sama.
            if (tickGerak % 32 == 0) arahStrafe *= -1;

            // Kalau target hilang, radar muter penuh dan bot bergerak pendek, bukan maju jauh.
            if (waktuHilang > 8)
            {
                SetTurnRadarRight(360);
                SetTurnRight(35 * arahStrafe);
                SetForward(45);
            }

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        waktuHilang = 0;

        double jarak = DistanceTo(e.X, e.Y);
        double power = HitungPowerTembakan(jarak);

        // Prediksi posisi musuh agar tidak selalu nembak posisi lama saat musuh muter.
        double targetX = e.X;
        double targetY = e.Y;

        if (punyaDataMusuhSebelumnya)
        {
            double vx = e.X - musuhXTerakhir;
            double vy = e.Y - musuhYTerakhir;
            double kecepatanPeluru = 20 - 3 * power;
            double waktuPeluru = jarak / kecepatanPeluru;

            // Prediksi ringan saja, jangan terlalu ekstrem supaya aim tidak liar.
            targetX = e.X + vx * waktuPeluru * 0.65;
            targetY = e.Y + vy * waktuPeluru * 0.65;
        }

        musuhXTerakhir = e.X;
        musuhYTerakhir = e.Y;
        punyaDataMusuhSebelumnya = true;

        double arahMusuhMutlak = DirectionTo(e.X, e.Y);
        double arahTargetPrediksi = DirectionTo(targetX, targetY);
        double sudutKeMusuh = BearingTo(e.X, e.Y);

        // Radar lock tetap ke posisi asli musuh, bukan prediksi.
        double radarTurn = CalcDeltaAngle(RadarDirection, arahMusuhMutlak);
        radarTurn += radarTurn >= 0 ? 25 : -25;
        SetTurnRadarRight(radarTurn);

        // Gun diarahkan ke posisi prediksi.
        double gunTurn = CalcDeltaAngle(GunDirection, arahTargetPrediksi);
        SetTurnGunRight(gunTurn);

        AturGerakanSniper(sudutKeMusuh, jarak, gunTurn);
        TembakKalauAman(jarak, gunTurn, power);
    }

    void AturGerakanSniper(double sudutKeMusuh, double jarak, double gunTurn)
    {
        if (waktuLepasTembok > 0)
        {
            // Bug fix tembok: jangan maju saat nempel tembok.
            // Mundur dulu, sambil ganti sudut supaya keluar dari tembok/sudut arena.
            SetTurnRight(80 * arahStrafe);
            SetBack(160);
            return;
        }

        if (jarak < JarakBahaya)
        {
            // Bug fix nabrak: hadap ke musuh, lalu MUNDUR.
            // Ini lebih stabil daripada belok 150 derajat lalu maju.
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 20 * arahStrafe));
            SetBack(230);
            return;
        }

        if (jarak < JarakAman)
        {
            // Masih terlalu dekat: mundur diagonal supaya jarak naik tanpa diam total.
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 45 * arahStrafe));
            SetBack(150);
            return;
        }

        if (jarak > JarakIdealMax)
        {
            // Terlalu jauh: dekati sedikit, tapi tidak lurus menabrak musuh.
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 35 * arahStrafe));
            SetForward(85);
            return;
        }

        // Zona ideal sniper: jangan orbit besar 90 derajat terus.
        // Kalau gun belum lurus, gerak pendek untuk stabilkan aim.
        if (Math.Abs(gunTurn) < 5)
        {
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 65 * arahStrafe));
            SetForward(35);
        }
        else
        {
            SetTurnRight(NormalizeRelative(sudutKeMusuh + 55 * arahStrafe));
            SetForward(20);
        }
    }

    double HitungPowerTembakan(double jarak)
    {
        if (jarak < 180) return 2.6;
        if (jarak < 420) return 2.1;
        if (jarak < 620) return 1.6;
        return 1.1;
    }

    void TembakKalauAman(double jarak, double gunTurn, double power)
    {
        if (jarak > JarakTembakMaks) return;

        // Makin jauh, toleransi aim harus makin kecil.
        double toleransiAim = jarak < 300 ? 7 : 4;
        if (Math.Abs(gunTurn) > toleransiAim) return;

        SetFire(power);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Bug fix tabrakan: jangan balas nabrak.
        // Hadap ke bot yang nabrak, lalu mundur keras.
        arahStrafe *= -1;
        SetTurnRight(NormalizeRelative(BearingTo(e.X, e.Y) + 15 * arahStrafe));
        SetBack(240);
        SetFire(2.4);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Bug fix stuck tembok: aktifkan mode keluar tembok dan balik arah strafe.
        arahStrafe *= -1;
        waktuLepasTembok = 16;
        SetBack(180);
        SetTurnRight(100 * arahStrafe);
    }

    static double NormalizeRelative(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}
