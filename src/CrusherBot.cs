using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class CrusherBot : Bot
{
    static void Main(string[] args) => new CrusherBot().Start();

    // ── Tuning constants ──────────────────────────────────────────
    const double WALL_MARGIN        = 80;   // stay away from walls
    const double DANGER_RADIUS      = 150;  // "too close" threshold
    const double BULLET_SPEED_BASE  = 20;   // approx bullet velocity
    const double FIRE_POWER_MAX     = 3.0;
    const double FIRE_POWER_MIN     = 0.5;
    const double LOW_ENERGY         = 30;   // switch to survival mode

    // ── State ─────────────────────────────────────────────────────
    readonly Dictionary<int, EnemyInfo> _enemies = new();
    int    _targetId    = -1;
    double _movementDir = 1;    // 1 = forward, -1 = reverse
    int    _moveTimer   = 0;
    bool   _dodgeMode   = false;
    int    _dodgeTimer  = 0;
    int    _roundNumber = 0;

    // ══════════════════════════════════════════════════════════════
    // Run — main loop
    // ══════════════════════════════════════════════════════════════
    public override void Run()
    {
        _roundNumber++;

        // Stylish red-and-black colour scheme
        BodyColor   = Color.FromArgb(20,  20,  20);
        TurretColor = Color.FromArgb(200, 30,  30);
        RadarColor  = Color.FromArgb(255, 80,  0);
        BulletColor = Color.FromArgb(255, 200, 0);
        ScanColor   = Color.FromArgb(255, 60,  60);

        // Decouple radar from gun so we can spin it freely
        // (Tank Royale does this automatically per turn)

        while (true)
        {
            // Pick the most dangerous/nearest enemy as our primary target
            UpdateTarget();

            if (_targetId >= 0 && _enemies.TryGetValue(_targetId, out var tgt))
            {
                AimAndFire(tgt);
            }
            else
            {
                // No target — sweep radar full circle
                TurnRadarRight(45);
            }

            PerformMovement();
            HandleDodge();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Targeting — predictive (linear extrapolation)
    // ══════════════════════════════════════════════════════════════
    private void AimAndFire(EnemyInfo e)
    {
        double firePower = ChooseFirePower(e.Distance);
        double bulletSpeed = BULLET_SPEED_BASE - 3 * firePower;

        // Estimate turns for bullet to travel
        double ticks = e.Distance / bulletSpeed;

        // Predict enemy position
        double futureX = e.X + Math.Cos(e.HeadingRad) * e.Speed * ticks;
        double futureY = e.Y + Math.Sin(e.HeadingRad) * e.Speed * ticks;

        // Clamp to arena bounds so we don't shoot at a ghost
        futureX = Math.Max(WALL_MARGIN, Math.Min(ArenaWidth  - WALL_MARGIN, futureX));
        futureY = Math.Max(WALL_MARGIN, Math.Min(ArenaHeight - WALL_MARGIN, futureY));

        // Aim gun at predicted position
        double gunBearing = GunBearingTo(futureX, futureY);
        TurnGunLeft(gunBearing);

        // Radar lock on actual position
        double radarBearing = RadarBearingTo(e.X, e.Y);
        TurnRadarLeft(radarBearing * 2); // over-rotate for radar lock

        // Fire only when gun is roughly aligned
        if (Math.Abs(GunBearingTo(futureX, futureY)) < 5 && GunHeat == 0)
        {
            Fire(firePower);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Movement — anti-gravity + strafing
    // ══════════════════════════════════════════════════════════════
    private void PerformMovement()
    {
        if (_dodgeMode) return; // dodge handler takes over

        // Compute anti-gravity vector
        double forceX = 0, forceY = 0;

        // Repel from walls
        forceX += RepelForce(X,           1.0);
        forceX -= RepelForce(ArenaWidth  - X, 1.0);
        forceY += RepelForce(Y,           1.0);
        forceY -= RepelForce(ArenaHeight - Y, 1.0);

        // Repel from each enemy
        foreach (var e in _enemies.Values)
        {
            double dist = e.Distance;
            if (dist < 1) dist = 1;
            double strength = 5000.0 / (dist * dist);
            double angle = Math.Atan2(Y - e.Y, X - e.X);
            forceX += Math.Cos(angle) * strength;
            forceY += Math.Sin(angle) * strength;
        }

        // Translate force vector into movement
        if (forceX != 0 || forceY != 0)
        {
            double targetAngle = Math.Atan2(forceY, forceX) * 180.0 / Math.PI;
            double bearing = NormalizeBearing(targetAngle - Direction);

            if (Math.Abs(bearing) > 90)
            {
                // Shorter to go in reverse
                bearing = NormalizeBearing(bearing + 180);
                TurnLeft(bearing);
                Back(50);
            }
            else
            {
                TurnLeft(bearing);
                Forward(50);
            }
        }
        else
        {
            // Default oscillating movement so we're never a sitting duck
            _moveTimer--;
            if (_moveTimer <= 0)
            {
                _movementDir *= -1;
                _moveTimer = 15 + new Random().Next(10);
            }
            Forward(80 * _movementDir);
            TurnLeft(15 * _movementDir);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Bullet dodging — triggered when enemy energy drops (they fired)
    // ══════════════════════════════════════════════════════════════
    private void HandleDodge()
    {
        if (_dodgeMode)
        {
            // Strafe perpendicular to target
            TurnLeft(90);
            Forward(60 * _movementDir);
            _dodgeTimer--;
            if (_dodgeTimer <= 0) _dodgeMode = false;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Fire power — scales with distance & own energy
    // ══════════════════════════════════════════════════════════════
    private double ChooseFirePower(double distance)
    {
        if (Energy < LOW_ENERGY)
            return FIRE_POWER_MIN; // survival mode

        if (distance < 100)  return FIRE_POWER_MAX;
        if (distance < 200)  return 2.0;
        if (distance < 350)  return 1.5;
        return FIRE_POWER_MIN;
    }

    // ══════════════════════════════════════════════════════════════
    // Target selection — nearest enemy with lowest energy
    // ══════════════════════════════════════════════════════════════
    private void UpdateTarget()
    {
        if (_enemies.Count == 0) { _targetId = -1; return; }

        double bestScore = double.MaxValue;
        int    bestId    = -1;

        foreach (var kv in _enemies)
        {
            var e = kv.Value;
            // Score = distance - (we prefer close & weak enemies)
            double score = e.Distance + e.Energy * 2;
            if (score < bestScore) { bestScore = score; bestId = kv.Key; }
        }

        _targetId = bestId;
    }

    // ══════════════════════════════════════════════════════════════
    // Events
    // ══════════════════════════════════════════════════════════════

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Detect if enemy fired (energy dropped)
        if (_enemies.TryGetValue(e.ScannedBotId, out var prev))
        {
            double energyDrop = prev.Energy - e.Energy;
            if (energyDrop > 0.09 && energyDrop <= 3.1)
            {
                // Enemy likely fired — dodge!
                _dodgeMode  = true;
                _dodgeTimer = 8;
                _movementDir *= -1; // switch strafe direction
            }
        }

        // Update or add enemy record
        _enemies[e.ScannedBotId] = new EnemyInfo
        {
            X          = e.X,
            Y          = e.Y,
            Energy     = e.Energy,
            Speed      = e.Speed,
            HeadingRad = e.Direction * Math.PI / 180.0,
            Distance   = DistanceTo(e.X, e.Y),
            LastSeen   = TurnNumber
        };
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Ram bonus: if we ram them, fire hard
        double power = e.Energy > 16 ? 3 : e.Energy > 4 ? 2 : 1;
        Fire(power);

        // Back off slightly then re-engage
        Back(30);
        TurnToFaceTarget(e.X, e.Y);
        Forward(50);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Bounce off the wall
        Back(40);
        _movementDir *= -1;
        TurnLeft(30 + new Random().Next(60));
    }

    public override void OnBotDeath(BotDeathEvent e)
    {
        // Remove dead bot from tracking
        _enemies.Remove(e.VictimId);
        if (_targetId == e.VictimId) _targetId = -1;
    }

    public override void OnBulletHit(BulletHitBotEvent e)
    {
        // Hit confirmed — continue pressing advantage
        if (_enemies.TryGetValue(e.VictimId, out var victim))
        {
            victim.Energy -= GetBulletDamage(e.Bullet.Power);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Immediately dodge after being hit
        _dodgeMode  = true;
        _dodgeTimer = 10;
        _movementDir *= -1;
    }

    // ══════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════

    private void TurnToFaceTarget(double x, double y)
    {
        double bearing = BearingTo(x, y);
        TurnLeft(bearing);
    }

    private static double RepelForce(double dist, double power)
    {
        if (dist < 1) dist = 1;
        return power * 2000.0 / (dist * dist);
    }

    private static double NormalizeBearing(double angle)
    {
        while (angle >  180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private static double GetBulletDamage(double power) => 4 * power;
}

// ══════════════════════════════════════════════════════════════════
// Enemy tracking record
// ══════════════════════════════════════════════════════════════════
public class EnemyInfo
{
    public double X          { get; set; }
    public double Y          { get; set; }
    public double Energy     { get; set; }
    public double Speed      { get; set; }
    public double HeadingRad { get; set; }
    public double Distance   { get; set; }
    public int    LastSeen   { get; set; }
}