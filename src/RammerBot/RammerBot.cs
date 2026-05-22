using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System;

// ------------------------------------------------------------------
// RammerBot - Bot Alternatif 2 (v2)
// ------------------------------------------------------------------
// Strategi Greedy: Tabrak musuh yang energinya rendah.
// Heuristic: Maksimalkan Ram Damage dengan memilih target terlemah,
// melemahkan dengan tembakan, lalu menabrak dengan full speed.
// ------------------------------------------------------------------
public class RammerBot : Bot
{
    private double enemyX = 0;
    private double enemyY = 0;
    private double enemyEnergy = 100;
    private bool enemyDetected = false;
    private const double RAM_THRESHOLD = 50.0;

    static void Main(string[] args) { new RammerBot().Start(); }
    RammerBot() : base(BotInfo.FromFile("RammerBot.json")) { }

    public override void Run()
    {
        while (IsRunning)
        {
            if (enemyDetected)
            {
                // Radar lock
                double dx = enemyX - X;
                double dy = enemyY - Y;
                double angleToEnemy = Math.Atan2(dx, dy) * (180.0 / Math.PI);
                double radarBearing = NormalizeRelativeAngle(angleToEnemy - RadarDirection);
                TurnRadarRight(radarBearing + (radarBearing > 0 ? 15 : -15));

                // Greedy: tabrak jika musuh lemah, tembak dulu jika masih kuat
                if (enemyEnergy < RAM_THRESHOLD)
                    ChargeAndRam();
                else
                    WeakenAndChase();
            }
            else
            {
                TurnRadarRight(45);
                TurnRight(15);
                Forward(80);
            }
        }
    }

    // Kejar dan tabrak musuh yang lemah
    private void ChargeAndRam()
    {
        double dx = enemyX - X;
        double dy = enemyY - Y;
        double angleToEnemy = Math.Atan2(dx, dy) * (180.0 / Math.PI);
        double bearing = NormalizeRelativeAngle(angleToEnemy - Direction);

        // Arahkan body langsung ke musuh
        TurnLeft(bearing);

        // Maju secepat mungkin untuk menabrak
        Forward(DistanceTo(enemyX, enemyY) + 30);
    }

    // Tembak untuk melemahkan musuh sebelum ditabrak
    private void WeakenAndChase()
    {
        double dx = enemyX - X;
        double dy = enemyY - Y;
        double distance = DistanceTo(enemyX, enemyY);
        double angleToEnemy = Math.Atan2(dx, dy) * (180.0 / Math.PI);

        // Arahkan body ke musuh
        double bearing = NormalizeRelativeAngle(angleToEnemy - Direction);
        TurnLeft(bearing);

        // Tembak untuk melemahkan
        double gunBearing = NormalizeRelativeAngle(angleToEnemy - GunDirection);
        TurnGunLeft(gunBearing);
        if (GunHeat == 0)
            Fire(2.5); // Tembak keras untuk cepat melemahkan

        // Dekati musuh
        Forward(Math.Min(distance, 100));
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        double scannedEnergy = evt.Energy;

        // Greedy: prioritaskan musuh dengan energi paling rendah
        if (!enemyDetected || scannedEnergy < enemyEnergy)
        {
            enemyX = evt.X;
            enemyY = evt.Y;
            enemyEnergy = scannedEnergy;
            enemyDetected = true;
        }

        // Tembak jika musuh masih kuat
        if (enemyEnergy >= RAM_THRESHOLD)
        {
            double distance = DistanceTo(evt.X, evt.Y);
            double firePower = distance < 200 ? 3.0 : 2.0;
            double gunBearing = NormalizeRelativeAngle(
                Math.Atan2(evt.X - X, evt.Y - Y) * (180.0 / Math.PI) - GunDirection);
            TurnGunLeft(gunBearing);
            if (GunHeat == 0)
                Fire(firePower);
        }
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        // Terus tabrak jika musuh masih hidup dan lemah
        if (evt.Energy < RAM_THRESHOLD)
            Forward(40);
        else
            Back(20);
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Sedikit menghindar tapi tetap kejar musuh
        double bearing = CalcBearing(evt.Bullet.Direction);
        TurnLeft(45 - bearing);
    }

    public override void OnHitWall(HitWallEvent evt)
    {
        Back(50);
        TurnRight(30);
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        enemyDetected = false;
        enemyEnergy = 100;
    }
}
