using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System;

// ------------------------------------------------------------------
// GreedyHunter - Bot Utama (v2)
// ------------------------------------------------------------------
// Strategi Greedy: Selalu mengejar dan menyerang musuh terdekat.
// Heuristic: Maksimalkan Bullet Damage dengan predictive aiming,
// radar lock, dan gerakan melingkar mengelilingi musuh.
// ------------------------------------------------------------------
public class GreedyHunter : Bot
{
    private double enemyX = 0;
    private double enemyY = 0;
    private double enemySpeed = 0;
    private double enemyDirection = 0;
    private bool enemyDetected = false;
    private int moveDirection = 1; // 1 = maju, -1 = mundur

    static void Main(string[] args) { new GreedyHunter().Start(); }
    GreedyHunter() : base(BotInfo.FromFile("GreedyHunter.json")) { }

    public override void Run()
    {
        // Set warna body (opsional)
        while (IsRunning)
        {
            if (enemyDetected)
            {
                // Radar lock: selalu putar radar ke arah musuh
                LockRadarOnEnemy();

                // Greedy: bergerak melingkar mengelilingi musuh
                CircleAroundEnemy();
            }
            else
            {
                // Scan berputar sampai ketemu musuh
                TurnRadarRight(45);
                TurnRight(10);
                Forward(50);
            }
        }
    }

    // Kunci radar ke musuh agar selalu terdeteksi
    private void LockRadarOnEnemy()
    {
        double dx = enemyX - X;
        double dy = enemyY - Y;
        double angleToEnemy = Math.Atan2(dx, dy) * (180.0 / Math.PI);
        double radarBearing = NormalizeRelativeAngle(angleToEnemy - RadarDirection);
        // Putar radar sedikit lebih jauh agar musuh tetap dalam scan arc
        TurnRadarRight(radarBearing + (radarBearing > 0 ? 10 : -10));
    }

    // Bergerak melingkar mengelilingi musuh
    private void CircleAroundEnemy()
    {
        double dx = enemyX - X;
        double dy = enemyY - Y;
        double angleToEnemy = Math.Atan2(dx, dy) * (180.0 / Math.PI);
        double distance = DistanceTo(enemyX, enemyY);

        // Arahkan body tegak lurus ke musuh (untuk gerakan melingkar)
        double perpAngle = angleToEnemy + 90;
        double bodyBearing = NormalizeRelativeAngle(perpAngle - Direction);
        TurnLeft(bodyBearing);

        // Jaga jarak optimal: 150-250 piksel
        if (distance > 250)
            Forward(50 * moveDirection);
        else if (distance < 150)
            Back(50);
        else
            Forward(40 * moveDirection);
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        enemyX = evt.X;
        enemyY = evt.Y;
        enemySpeed = evt.Speed;
        enemyDirection = evt.Direction;
        enemyDetected = true;

        double distance = DistanceTo(evt.X, evt.Y);

        // Predictive aiming: prediksi posisi musuh saat peluru tiba
        double firePower = distance < 150 ? 3.0 : distance < 300 ? 2.0 : 1.0;
        double bulletSpeed = 20 - 3 * firePower;
        double travelTime = distance / bulletSpeed;

        // Prediksi posisi musuh
        double predX = evt.X + Math.Sin(enemyDirection * Math.PI / 180) * enemySpeed * travelTime;
        double predY = evt.Y + Math.Cos(enemyDirection * Math.PI / 180) * enemySpeed * travelTime;

        // Arahkan gun ke posisi prediksi
        double dxPred = predX - X;
        double dyPred = predY - Y;
        double predAngle = Math.Atan2(dxPred, dyPred) * (180.0 / Math.PI);
        double gunBearing = NormalizeRelativeAngle(predAngle - GunDirection);
        TurnGunLeft(gunBearing);

        // Tembak jika gun sudah dingin
        if (GunHeat == 0)
            Fire(firePower);
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Ganti arah melingkar saat kena peluru
        moveDirection *= -1;
        double bearing = CalcBearing(evt.Bullet.Direction);
        TurnLeft(90 - bearing);
        Forward(100 * moveDirection);
    }

    public override void OnHitWall(HitWallEvent evt)
    {
        moveDirection *= -1;
        Back(30);
        TurnRight(45);
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        // Ram jika musuh lemah, mundur jika masih kuat
        if (evt.Energy < 20)
            Forward(30);
        else
        {
            moveDirection *= -1;
            Back(50);
        }
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        enemyDetected = false;
    }
}
