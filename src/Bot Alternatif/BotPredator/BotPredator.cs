using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;

// ---------------------------------------------------------------
// BotPredator – Bot ramming agresif
// Radar sweep 90° di depan | Kejar + tembak 1.5 | PostRam tembak 3
// ---------------------------------------------------------------
public class BotPredator : Bot
{
    private double targetX, targetY;
    private int    targetBotId        = -1;
    private int    turnsSinceLastScan = 0;
    private bool   radarSweepRight    = true;

    static void Main() => new BotPredator().Start();
    BotPredator() : base(BotInfo.FromFile("BotPredator.json")) { }

    public override void Run()
    {
        BodyColor   = Color.Red;
        TurretColor = Color.Yellow;
        RadarColor  = Color.Cyan;
        BulletColor = Color.Yellow;

        // Radar dan gun bergerak independen dari badan
        AdjustRadarForBodyTurn = true;
        AdjustGunForBodyTurn   = true;
        AdjustRadarForGunTurn  = true;

        while (IsRunning)
        {
            turnsSinceLastScan++;

            if (turnsSinceLastScan >= 10)
            {
                // Tidak ada musuh 10 turn → scan 360° untuk cari ulang
                targetBotId = -1;
                TurnRadarRight(360);
                if (turnsSinceLastScan >= 10) // masih belum dapat target
                    turnsSinceLastScan = 5;   // coba lagi dalam 5 turn
            }
            else if (targetBotId >= 0)
            {
                // Punya target: putar badan + laras ke target, maju, tembak
                SetTurnLeft(BearingTo(targetX, targetY));
                SetTurnGunLeft(GunBearingTo(targetX, targetY));
                SetForward(10000);
                SetFire(1.5);
                SweepRadarFront();
                Go();
            }
            else
            {
                // Belum ada target: sweep radar 90° di depan
                SweepRadarFront();
                Go();
            }
        }
    }

    // Sweep radar ±45° dari arah badan (total 90°), bolak-balik tiap tick
    private void SweepRadarFront()
    {
        double offset = radarSweepRight ? -45.0 : 45.0;
        SetTurnRadarLeft(CalcRadarBearing(Direction) + offset);
        radarSweepRight = !radarSweepRight;
    }

    // Musuh terdeteksi → lock target pertama, update posisi target yang sama
    public override void OnScannedBot(ScannedBotEvent e)
    {
        turnsSinceLastScan = 0;
        if (targetBotId < 0 || e.ScannedBotId == targetBotId)
        {
            targetBotId = e.ScannedBotId;
            targetX     = e.X;
            targetY     = e.Y;
        }
    }

    // Berhasil tabrak → mundur, arahkan laras, tembak 3, langsung maju lagi
    public override void OnHitBot(HitBotEvent e)
    {
        Back(40);
        TurnGunLeft(GunBearingTo(targetX, targetY)); // blocking: laras terarah dulu
        Fire(3);
        SetForward(10000);
        Go();
    }

    // Nabrak dinding → mundur dan balik ke target
    public override void OnHitWall(HitWallEvent e)
    {
        SetBack(30);
        SetTurnLeft(targetBotId >= 0 ? BearingTo(targetX, targetY) : 90);
        Go();
    }

    // Target mati → lepas lock, segera cari musuh baru
    public override void OnBotDeath(Robocode.TankRoyale.BotApi.Events.BotDeathEvent e)
    {
        if (e.VictimId == targetBotId)
        {
            targetBotId        = -1;
            turnsSinceLastScan = 10;
        }
    }
}