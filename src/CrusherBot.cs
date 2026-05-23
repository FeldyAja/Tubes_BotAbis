using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ------------------------------------------------------------------
// CrusherBot — Robocode Tank Royale 0.30.0 (C#)
// ------------------------------------------------------------------
// BEHAVIOR:
//
//   HUNT MODE (no target):
//     Radar and gun spin together as one unit, sweeping the arena.
//     Body slowly patrols toward the center.
//
//   CRUSH MODE (enemy found):
//     Radar and gun LOCK onto enemy together and follow them.
//     Fires with predictive targeting.
//     Body orbits the enemy in a SPIRAL, getting closer each tick.
//     Rams the enemy at close range while still firing.
//
//   RESET:
//     When target dies or is lost for too long → back to HUNT instantly.
// ------------------------------------------------------------------
public class CrusherBot : Bot
{
    // ── Mode ─────────────────────────────────────────────────────────
    private enum Mode { Hunt, Crush }
    private Mode _mode = Mode.Hunt;

    // ── Enemy data ───────────────────────────────────────────────────
    private int    _enemyId      = -1;
    private double _enemyX       = 0;
    private double _enemyY       = 0;
    private double _enemyDir     = 0;   // degrees (Tank Royale: 0=north, clockwise)
    private double _enemySpeed   = 0;
    private double _enemyEnergy  = 100;
    private int    _lostTicks    = 0;
    private const int LostTimeout = 12; // ticks before abandoning lock

    // ── Movement / spiral ────────────────────────────────────────────
    private int    _orbitDir     = 1;   //  1 = clockwise,  -1 = counter-clockwise
    private double _orbitRadius  = 300; // current target orbit distance
    private const double MinOrbitRadius  = 35;  // ram distance
    private const double OrbitShrinkRate = 3.5; // px closer per tick

    // ── Scan ─────────────────────────────────────────────────────────
    private const double ScanSpeed = 18; // degrees per tick in hunt mode

    // ── Entry point ──────────────────────────────────────────────────
    static void Main() => new CrusherBot().Start();
    CrusherBot() : base(BotInfo.FromFile("CrusherBot.json")) { }

    // ================================================================
    //  MAIN LOOP
    // ================================================================
    public override void Run()
    {
        BodyColor   = Color.FromArgb(15,  15,  15);
        TurretColor = Color.FromArgb(255, 60,  0);
        RadarColor  = Color.FromArgb(255, 200, 0);
        BulletColor = Color.FromArgb(255, 100, 0);
        ScanColor   = Color.FromArgb(255, 255, 80);

        while (IsRunning)
        {
            _lostTicks++;

            // Lost track of enemy for too long → back to hunting
            if (_mode == Mode.Crush && _lostTicks > LostTimeout)
                EnterHuntMode();

            if (_mode == Mode.Hunt)
                DoHunt();
            else
                DoCrush();

            Go(); // execute all Set* commands for this tick
        }
    }

    // ================================================================
    //  HUNT MODE — radar + gun sweep together
    // ================================================================
    private void DoHunt()
    {
        // Spin gun and radar at the same rate → they move as ONE unit
        SetTurnGunRight(ScanSpeed);
        SetTurnRadarRight(ScanSpeed);

        // Body: patrol toward arena center so we don't get cornered
        double bearingCenter = BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0);
        SetTurnRight(bearingCenter);
        MaxSpeed = 3;
        SetForward(80);
    }

    // ================================================================
    //  CRUSH MODE — lock, spiral in, ram & fire
    // ================================================================
    private void DoCrush()
    {
        double dist = DistanceTo(_enemyX, _enemyY);

        // ── 1. Lock radar and gun onto enemy together ─────────────────
        // Both use the angle from their own current direction to the enemy.
        // Since we always set them the same amount, they stay aligned.
        double gunBearing = GunBearingTo(_enemyX, _enemyY);

        // Overshoot multiplier keeps the lock tight even if enemy moves fast
        SetTurnGunRight(gunBearing   * 2.0);
        SetTurnRadarRight(gunBearing * 2.0); // radar follows gun

        // ── 2. Predictive firing ──────────────────────────────────────
        double firePower = Energy < 20 ? 1.0
                         : dist  < 100 ? 3.0
                         : dist  < 250 ? 2.0
                                       : 1.5;

        double bulletSpeed = 20.0 - 3.0 * firePower;
        double ticks       = dist / bulletSpeed;

        // Convert Tank Royale direction (degrees, 0=north CW) to vector
        double dirRad  = _enemyDir * Math.PI / 180.0;
        double futureX = _enemyX + Math.Sin(dirRad) * _enemySpeed * ticks;
        double futureY = _enemyY + Math.Cos(dirRad) * _enemySpeed * ticks;

        // Clamp predicted position inside arena bounds
        futureX = Math.Max(18, Math.Min(ArenaWidth  - 18, futureX));
        futureY = Math.Max(18, Math.Min(ArenaHeight - 18, futureY));

        // Aim at predicted position
        double aimBearing = GunBearingTo(futureX, futureY);
        SetTurnGunRight(aimBearing);

        // Fire when gun is aimed within 5° and gun is cool
        if (Math.Abs(aimBearing) < 5.0 && GunHeat == 0)
            SetFire(firePower);

        // ── 3. Spiral orbit movement ──────────────────────────────────
        // Shrink orbit radius every tick → robot naturally spirals inward
        _orbitRadius = Math.Max(MinOrbitRadius, _orbitRadius - OrbitShrinkRate);

        // Turn body so enemy is 90° to our side, then drive forward.
        // The combination of forward movement + perpendicular facing = orbit.
        double bodyBearing = BearingTo(_enemyX, _enemyY);
        SetTurnRight(bodyBearing + 90.0 * _orbitDir);

        // Speed: fast when far, controlled when close (for accurate ramming)
        MaxSpeed = dist > 200 ? 8.0 : dist > 80 ? 5.0 : 3.0;
        SetForward(200);

        // ── 4. Wall avoidance ─────────────────────────────────────────
        AvoidWalls();
    }

    // ================================================================
    //  EVENTS
    // ================================================================
    public override void OnScannedBot(ScannedBotEvent e)
    {
        double scannedDist = DistanceTo(e.X, e.Y);

        // In Crush mode: ignore if not our target AND farther than current target
        if (_mode == Mode.Crush && _enemyId != -1 && e.ScannedBotId != _enemyId)
        {
            if (scannedDist >= DistanceTo(_enemyX, _enemyY))
                return; // ignore — not our target and not closer
            // Closer enemy found → switch target
        }

        // Detect incoming bullet via enemy energy drop → flip orbit to dodge
        if (_enemyId == e.ScannedBotId)
        {
            double drop = _enemyEnergy - e.Energy;
            if (drop > 0.09 && drop <= 3.0)
                _orbitDir *= -1;
        }

        // Store fresh enemy data
        _enemyId     = e.ScannedBotId;
        _enemyX      = e.X;
        _enemyY      = e.Y;
        _enemyDir    = e.Direction;
        _enemySpeed  = e.Speed;
        _enemyEnergy = e.Energy;
        _lostTicks   = 0;

        // Transition: HUNT → CRUSH on first scan
        if (_mode == Mode.Hunt)
            EnterCrushMode();
    }

    // Target destroyed → reset and hunt immediately
    public override void OnBotDeath(BotDeathEvent e)
    {
        if (e.VictimId == _enemyId)
        {
            Console.WriteLine($"[CRUSHER] Target {_enemyId} destroyed. Hunting next...");
            EnterHuntMode();
        }
    }

    // Bullet landed → accelerate spiral inward
    public override void OnBulletHit(BulletHitBotEvent e)
    {
        _orbitRadius = Math.Max(MinOrbitRadius, _orbitRadius - 12);
    }

    // We got hit → flip orbit direction (dodge)
    public override void OnHitByBullet(HitByBulletEvent e)
    {
        _orbitDir *= -1;
    }

    // Rammed wall → flip orbit direction
    public override void OnHitWall(HitWallEvent e)
    {
        _orbitDir *= -1;
        SetBack(30); // bounce off
    }

    // Physical ram → unload max firepower
    public override void OnHitBot(HitBotEvent e)
    {
        Fire(3.0);
    }

    // ================================================================
    //  MODE HELPERS
    // ================================================================
    private void EnterHuntMode()
    {
        _mode        = Mode.Hunt;
        _enemyId     = -1;
        _orbitRadius = 300;   // reset spiral for next target
        _lostTicks   = 0;
        Console.WriteLine("[CRUSHER] HUNT MODE — scanning...");
    }

    private void EnterCrushMode()
    {
        _mode      = Mode.Crush;
        _lostTicks = 0;
        Console.WriteLine($"[CRUSHER] CRUSH MODE — target: {_enemyId}");
    }

    // ================================================================
    //  WALL AVOIDANCE
    // ================================================================
    private void AvoidWalls()
    {
        const double margin = 50;
        bool nearWall = X < margin || X > ArenaWidth  - margin ||
                        Y < margin || Y > ArenaHeight - margin;

        if (nearWall)
        {
            double bearCenter = BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0);
            SetTurnRight(bearCenter);
            SetForward(80);
        }
    }
}
