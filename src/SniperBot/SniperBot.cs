using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System;

// ------------------------------------------------------------------
// SniperBot - Bot Alternatif 3 (v2)
// ------------------------------------------------------------------
// Strategi Greedy: Diam di sudut, tembak peluru berat dari jauh.
// Heuristic: Maksimalkan Bullet Damage Bonus dengan selalu pakai
// firepower 3.0 dan predictive aiming untuk akurasi tinggi.
// ------------------------------------------------------------------
public class SniperBot : Bot
{
    private double enemyX = 0;
    private double enemyY = 0;
    private double enemySpeed = 0;
    private double enemyDirection = 0;
    private bool enemyDetected = false;
    private bool inPosition = false;
    private double targetCornerX = 0;
    private double targetCornerY = 0;

    static void Main(string[] args) { new SniperBot().Start(); }
    SniperBot() : base(BotInfo.FromFile("SniperBot.json")) { }

    public override void Run()
    {
        // Tentukan sudut yang jauh dari tengah arena
        targetCornerX = 80;
        targetCornerY = 80;

        while (IsRunning)
        {
            if (!inPosition)
            {
                GoToCorner();
            }
            else
            {
                if (enemyDetected)
                {
                    // Radar lock ke musuh
                    double dx = enemyX - X;
                    double dy = enemyY - Y;
                    double angleToEnemy = Math.Atan2(dx, dy) * (180.0 / Math.PI);
                    double radarBearing = NormalizeRelativeAngle(angleToEnemy - RadarDirection);
                    TurnRadarRight(radarBearing + (radarBearing > 0 ? 10 : -10));
                }
                else
                {
                    // Scan berputar
                    TurnRadarRight(45);
                }
            }
        }
    }

    private void GoToCorner()
    {
        double dx = targetCornerX - X;
        double dy = targetCornerY - Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 30)
        {
            inPosition = true;
            return;
        }

        double angle = Math.Atan2(dx, dy) * (180.0 / Math.PI);
        double bearing = NormalizeRelativeAngle(angle - Direction);
        TurnLeft(bearing);
        Forward(Math.Min(distance, 100));
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        enemyX = evt.X;
        enemyY = evt.Y;
        enemySpeed = evt.Speed;
        enemyDirection = evt.Direction;
        enemyDetected = true;

        double distance = DistanceTo(evt.X, evt.Y);

        // Greedy: selalu pakai firepower maksimal untuk damage besar
        double firePower = distance < 700 ? 3.0 : 2.0;
        double bulletSpeed = 20 - 3 * firePower;
        double travelTime = distance / bulletSpeed;

        // Predictive aiming: prediksi posisi musuh saat peluru tiba
        double predX = evt.X + Math.Sin(enemyDirection * Math.PI / 180) * enemySpeed * travelTime;
        double predY = evt.Y + Math.Cos(enemyDirection * Math.PI / 180) * enemySpeed * travelTime;

        double dxPred = predX - X;
        double dyPred = predY - Y;
        double predAngle = Math.Atan2(dxPred, dyPred) * (180.0 / Math.PI);
        double gunBearing = NormalizeRelativeAngle(predAngle - GunDirection);
        TurnGunLeft(gunBearing);

        // Tembak saat gun sudah dingin dan sudah di posisi
        if (GunHeat == 0)
            Fire(firePower);
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Pindah sedikit lalu kembali ke sudut
        double bearing = CalcBearing(evt.Bullet.Direction);
        TurnLeft(90 - bearing);
        Forward(80);
        inPosition = false;
    }

    public override void OnHitWall(HitWallEvent evt)
    {
        Back(30);
        TurnRight(45);
        inPosition = false;
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        Back(60);
        TurnRight(60);
        inPosition = false;
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        enemyDetected = false;
    }
}
