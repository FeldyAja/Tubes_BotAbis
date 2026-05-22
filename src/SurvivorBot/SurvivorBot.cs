using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System;

// ------------------------------------------------------------------
// SurvivorBot - Bot Alternatif 1 (v2)
// ------------------------------------------------------------------
// Strategi Greedy: Bertahan hidup selama mungkin.
// Heuristic: Maksimalkan Survival Score dengan selalu bergerak
// menghindari peluru, hanya tembak saat aman, dan jaga jarak.
// ------------------------------------------------------------------
public class SurvivorBot : Bot
{
    private double enemyX = 0;
    private double enemyY = 0;
    private bool enemyDetected = false;
    private int moveDirection = 1;
    private int turnDirection = 1;

    static void Main(string[] args) { new SurvivorBot().Start(); }
    SurvivorBot() : base(BotInfo.FromFile("SurvivorBot.json")) { }

    public override void Run()
    {
        while (IsRunning)
        {
            if (enemyDetected)
            {
                // Radar lock ke musuh
                double dx = enemyX - X;
                double dy = enemyY - Y;
                double angleToEnemy = Math.Atan2(dx, dy) * (180.0 / Math.PI);
                double radarBearing = NormalizeRelativeAngle(angleToEnemy - RadarDirection);
                TurnRadarRight(radarBearing + (radarBearing > 0 ? 10 : -10));

                // Greedy: jika energi rendah, prioritas menghindar
                if (Energy < 30)
                    Evade();
                else
                    SafeApproach();
            }
            else
            {
                // Scan sambil bergerak ke tengah arena
                TurnRadarRight(45);
                MoveToCenter();
            }
        }
    }

    // Gerakan menghindar saat energi rendah
    private void Evade()
    {
        TurnRight(90 * turnDirection);
        Forward(150 * moveDirection);
        turnDirection *= -1;
    }

    // Pendekatan aman: zigzag sambil jaga jarak
    private void SafeApproach()
    {
        double distance = DistanceTo(enemyX, enemyY);

        // Jaga jarak aman 300-500 piksel
        if (distance < 300)
        {
            // Terlalu dekat: mundur zigzag
            TurnRight(45 * turnDirection);
            Back(100);
            turnDirection *= -1;
        }
        else if (distance > 500)
        {
            // Terlalu jauh: maju sedikit
            double dx = enemyX - X;
            double dy = enemyY - Y;
            double angle = Math.Atan2(dx, dy) * (180.0 / Math.PI);
            double bearing = NormalizeRelativeAngle(angle - Direction);
            TurnLeft(bearing);
            Forward(80);
        }
        else
        {
            // Jarak ideal: gerak zigzag
            TurnRight(30 * turnDirection);
            Forward(100);
            turnDirection *= -1;
        }
    }

    // Bergerak ke tengah arena
    private void MoveToCenter()
    {
        double centerX = ArenaWidth / 2.0;
        double centerY = ArenaHeight / 2.0;
        double dx = centerX - X;
        double dy = centerY - Y;
        double angle = Math.Atan2(dx, dy) * (180.0 / Math.PI);
        double bearing = NormalizeRelativeAngle(angle - Direction);
        TurnLeft(bearing);
        Forward(50);
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        enemyX = evt.X;
        enemyY = evt.Y;
        enemyDetected = true;

        double distance = DistanceTo(evt.X, evt.Y);

        // Greedy: hanya tembak jika energi cukup dan posisi aman
        if (Energy > 40)
        {
            // Predictive aiming
            double firePower = 1.5; // Hemat energi
            double bulletSpeed = 20 - 3 * firePower;
            double travelTime = distance / bulletSpeed;
            double predX = evt.X + Math.Sin(evt.Direction * Math.PI / 180) * evt.Speed * travelTime;
            double predY = evt.Y + Math.Cos(evt.Direction * Math.PI / 180) * evt.Speed * travelTime;

            double dxPred = predX - X;
            double dyPred = predY - Y;
            double predAngle = Math.Atan2(dxPred, dyPred) * (180.0 / Math.PI);
            double gunBearing = NormalizeRelativeAngle(predAngle - GunDirection);
            TurnGunLeft(gunBearing);

            if (GunHeat == 0 && distance < 400)
                Fire(firePower);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Hindari arah peluru
        moveDirection *= -1;
        turnDirection *= -1;
        double bearing = CalcBearing(evt.Bullet.Direction);
        TurnLeft(90 - bearing);
        Forward(150 * moveDirection);
    }

    public override void OnHitWall(HitWallEvent evt)
    {
        moveDirection *= -1;
        Back(60);
        TurnRight(60 * turnDirection);
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        // Selalu hindari tabrakan untuk jaga energi
        moveDirection *= -1;
        Back(80);
        TurnRight(45 * turnDirection);
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        // Bot mati = dapat survival score! Reset dan cari musuh baru
        enemyDetected = false;
    }
}
