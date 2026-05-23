using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ------------------------------------------------------------------
// GreedyShooter — Fixed for Robocode Tank Royale 0.30.0
// ------------------------------------------------------------------
// FIXES APPLIED:
//
//   FIX 1 — Removed IsAdjustGunForBodyTurn / IsAdjustRadarForBodyTurn /
//            IsAdjustRadarForGunTurn — these properties do NOT exist in
//            Tank Royale. Gun and radar are always independently controlled
//            via SetTurnGunRight/Left and SetTurnRadarRight/Left.
//
//   FIX 2 — ScannedBotEvent has no .Distance property in Tank Royale.
//            Replaced with DistanceTo(e.X, e.Y) helper method.
// ------------------------------------------------------------------
public class GreedyShooter : Bot
{
    // ── Enemy registry ────────────────────────────────────────────────
    private class EnemyInfo
    {
        public int    Id;
        public double X;
        public double Y;
        public double Energy;
        public double HeadingRad;
        public double Velocity;
        public double Distance;
        public int    LastSeen;
    }

    private readonly Dictionary<int, EnemyInfo> _enemies = new();
    private const int EnemyStaleTicks = 20;

    // ── Movement ──────────────────────────────────────────────────────
    private int _orbitDir = 1;

    // ── Tick counter ─────────────────────────────────────────────────
    private int _tick = 0;

    static void Main()
    {
        new GreedyShooter().Start();
    }
    GreedyShooter() : base(BotInfo.FromFile("GreedyShooter.json")) { }

    // ============================================================
    //  MAIN LOOP
    // ============================================================
    public override void Run()
    {
        BodyColor   = Color.FromArgb(20,  20,  20);
        TurretColor = Color.FromArgb(0,   255, 80);
        RadarColor  = Color.FromArgb(0,   200, 255);
        BulletColor = Color.FromArgb(0,   255, 80);
        ScanColor   = Color.FromArgb(150, 255, 150);

        // FIX 1 — In Tank Royale, IsAdjust* properties do NOT exist.
        // Gun and radar are controlled independently without any flags.
        // Just spin the radar continuously — it always works independently.
        SetTurnRadarRight(double.PositiveInfinity);

        while (IsRunning)
        {
            _tick++;

            PruneStaleEnemies();

            var target = SelectGreedyTarget();
            if (target != null)
                AimAndFire(target);

            DoOrbit(target);

            Forward(30);
        }
    }

    // ============================================================
    //  GREEDY RULE 1 — Target with lowest energy
    // ============================================================
    private EnemyInfo SelectGreedyTarget()
    {
        EnemyInfo best     = null;
        double    lowestHP = double.MaxValue;

        foreach (var kv in _enemies)
        {
            var e = kv.Value;
            if (e.Energy < lowestHP)
            {
                lowestHP = e.Energy;
                best     = e;
            }
        }
        return best;
    }

    // ============================================================
    //  GREEDY RULE 2 — Maximum fire power based on energy & distance
    // ============================================================
    private double CalculateGreedyFirePower(double distanceToTarget)
    {
        double greedyPower = Energy * 0.30;
        greedyPower = Math.Min(3.0, Math.Max(0.1, greedyPower));

        if (distanceToTarget > 400) greedyPower = Math.Min(greedyPower, 1.5);
        if (distanceToTarget > 600) greedyPower = Math.Min(greedyPower, 0.5);

        return greedyPower;
    }

    // ============================================================
    //  GREEDY RULE 3 — Aim and fire immediately (within 10° tolerance)
    // ============================================================
    private void AimAndFire(EnemyInfo target)
    {
        double firePower   = CalculateGreedyFirePower(target.Distance);
        double bulletSpeed = 20.0 - 3.0 * firePower;

        // Predictive aim: calculate where enemy will be when bullet arrives
        double travelTicks = target.Distance / bulletSpeed;
        double futureX = target.X + Math.Sin(target.HeadingRad) * target.Velocity * travelTicks;
        double futureY = target.Y + Math.Cos(target.HeadingRad) * target.Velocity * travelTicks;

        futureX = Math.Max(18, Math.Min(ArenaWidth  - 18, futureX));
        futureY = Math.Max(18, Math.Min(ArenaHeight - 18, futureY));

        // GunBearingTo() returns positive = target is to the RIGHT of gun
        double gunBearing = GunBearingTo(futureX, futureY);
        SetTurnGunRight(gunBearing);

        if (Math.Abs(gunBearing) <= 10.0 && GunHeat == 0)
        {
            Fire(firePower);
            Console.WriteLine(
                $"[GREEDY FIRE] Target={target.Id} HP={target.Energy:F1} " +
                $"Power={firePower:F2} ({firePower / Energy * 100:F1}% of energy spent)");
        }
    }

    // ============================================================
    //  MOVEMENT — orbit around weakest target
    // ============================================================
    private void DoOrbit(EnemyInfo target)
    {
        if (target == null)
        {
            SetTurnRight(10);
            return;
        }

        double bearing = BearingTo(target.X, target.Y);
        SetTurnRight(bearing + 90 * _orbitDir);
        MaxSpeed = 6;
    }

    // ============================================================
    //  EVENTS
    // ============================================================
    public override void OnScannedBot(ScannedBotEvent e)
    {
        // FIX 2 — ScannedBotEvent has no .Distance in Tank Royale.
        // Use the built-in DistanceTo(x, y) helper instead.
        _enemies[e.ScannedBotId] = new EnemyInfo
        {
            Id          = e.ScannedBotId,
            X           = e.X,
            Y           = e.Y,
            Energy      = e.Energy,
            HeadingRad  = e.Direction * Math.PI / 180.0,
            Velocity    = e.Speed,
            Distance    = DistanceTo(e.X, e.Y),   // ← FIX 2
            LastSeen    = _tick
        };
    }

    public override void OnBotDeath(BotDeathEvent e)
    {
        _enemies.Remove(e.VictimId);
        Console.WriteLine($"[GREEDY] Target {e.VictimId} eliminated. Selecting next weakest...");
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        _orbitDir *= -1;
    }

    public override void OnHitWall(HitWallEvent e)
    {
        _orbitDir *= -1;
    }

    // ============================================================
    //  HELPER — remove enemies not seen recently
    // ============================================================
    private void PruneStaleEnemies()
    {
        var toRemove = new List<int>();
        foreach (var kv in _enemies)
            if (_tick - kv.Value.LastSeen > EnemyStaleTicks)
                toRemove.Add(kv.Key);
        foreach (var id in toRemove)
            _enemies.Remove(id);
    }
}
