using System;
using System.Drawing;
using Robocode;
using Robocode.Util;

namespace MyRobots
{
    public class CrusherBot : Bot
    {
        // ── Robot state ──────────────────────────────────────────────
        private enum Mode { Hunt, Crush }
        private Mode _mode = Mode.Hunt;

        // ── Enemy data ───────────────────────────────────────────────
        private string _enemyName     = null;
        private double _enemyX        = 0;
        private double _enemyY        = 0;
        private double _enemyDistance = 0;
        private double _enemyHeadingRad = 0;
        private double _enemyVelocity = 0;
        private double _enemyEnergy   = 100;
        private int    _lostTicks     = 0;          // ticks since last scan
        private const int LostTimeout = 15;         // ticks before giving up lock

        // ── Movement ─────────────────────────────────────────────────
        private int    _orbitDir      = 1;          // 1=clockwise, -1=counter
        private double _orbitRadius   = 300;        // start far, spiral in
        private const double MinOrbitRadius  = 30;  // ram distance
        private const double OrbitShrinkRate = 4;   // pixels closer per tick

        // ── Scan ─────────────────────────────────────────────────────
        private double _scanDir       = 1;          // 1=right, -1=left
        private const double ScanSpeed = 20;        // degrees per tick in hunt

        // ============================================================
        //  MAIN LOOP
        // ============================================================
        public override void Run()
        {
            BodyColor   = Color.FromArgb(20,  20,  20);
            GunColor    = Color.FromArgb(255, 60,  0);
            RadarColor  = Color.FromArgb(255, 200, 0);
            BulletColor = Color.FromArgb(255, 80,  0);
            ScanColor   = Color.FromArgb(255, 255, 100);

            // Gun follows radar exactly — they are ONE unit
            IsAdjustGunForRobotTurn   = true;
            IsAdjustRadarForRobotTurn = true;
            IsAdjustRadarForGunTurn   = false;  // radar & gun turn together

            while (true)
            {
                _lostTicks++;

                // If we haven't scanned the enemy for too long → back to Hunt
                if (_mode == Mode.Crush && _lostTicks > LostTimeout)
                    EnterHuntMode();

                if (_mode == Mode.Hunt)
                    DoHunt();
                else
                    DoCrush();

                Execute();
            }
        }

        
        private void DoHunt()
        {
            // Spin radar+gun together (gun offset = 0 relative to radar)
            SetTurnGunRight(ScanSpeed * _scanDir);   // gun leads
            // radar is glued to gun (IsAdjustRadarForGunTurn = false)
            // so radar follows gun automatically; we don't need separate radar turn

            // Slowly patrol toward center so we don't get cornered
            double cx = BattleFieldWidth  / 2.0;
            double cy = BattleFieldHeight / 2.0;
            double angleToCenter = Math.Atan2(cx - X, cy - Y) * 180.0 / Math.PI;
            SetTurnRight(NormalizeBearing(angleToCenter - Heading));
            MaxVelocity = 4;
            SetAhead(80);
        }

        // ============================================================
        //  CRUSH MODE — lock radar+gun, spiral in, ram & fire
        // ============================================================
        private void DoCrush()
        {
            // ── 1. Radar+Gun lock on enemy ───────────────────────────
            // Absolute bearing to enemy
            double absBearingRad = Math.Atan2(_enemyX - X, _enemyY - Y);

            // Turn GUN to face enemy (radar is glued to gun)
            double gunTurn = Utils.NormalRelativeAngle(absBearingRad - GunHeadingRadians);
            SetTurnGunRightRadians(gunTurn * 1.9);  // overshoot to stay locked

            // ── 2. Fire — predictive targeting ──────────────────────
            double firePower = _enemyDistance < 100 ? 3.0
                             : _enemyDistance < 250 ? 2.0
                             : Energy < 20          ? 1.0
                                                    : 1.5;

            double bulletSpeed = 20.0 - 3.0 * firePower;
            long   ticks       = (long)(_enemyDistance / bulletSpeed);

            double futureX = _enemyX + Math.Sin(_enemyHeadingRad) * _enemyVelocity * ticks;
            double futureY = _enemyY + Math.Cos(_enemyHeadingRad) * _enemyVelocity * ticks;
            futureX = Clamp(futureX, 18, BattleFieldWidth  - 18);
            futureY = Clamp(futureY, 18, BattleFieldHeight - 18);

            double aimAngle  = Math.Atan2(futureX - X, futureY - Y);
            double aimOffset = Utils.NormalRelativeAngle(aimAngle - GunHeadingRadians);

            if (Math.Abs(aimOffset) < DegToRad(6) && GunHeat == 0)
                SetFire(firePower);

            // ── 3. Body — spiral orbit getting closer ────────────────
            // Shrink orbit radius each tick
            _orbitRadius = Math.Max(MinOrbitRadius, _orbitRadius - OrbitShrinkRate);

            // Orbit: turn body so enemy is 90° to side, then drive toward them
            double bodyBearing = NormalizeBearing(
                Math.Atan2(_enemyX - X, _enemyY - Y) * 180.0 / Math.PI - Heading);

            // Strafe angle: 90° offset in orbit direction
            double strafeAngle = bodyBearing + 90.0 * _orbitDir;
            SetTurnRight(NormalizeBearing(strafeAngle));

            // Speed: fast when far, slow when close (controlled ram)
            double speed = _enemyDistance > 200 ? 8.0
                         : _enemyDistance > 80  ? 6.0
                                                 : 4.0;
            MaxVelocity = speed;

            // Always drive forward — orbit + spiral naturally closes distance
            SetAhead(200);

            // ── 4. Wall avoidance ────────────────────────────────────
            AvoidWalls();
        }

        // ============================================================
        //  EVENT: Enemy scanned
        // ============================================================
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            // Accept any robot in Hunt mode; in Crush mode prefer same target
            if (_mode == Mode.Crush && _enemyName != null && e.Name != _enemyName)
            {
                if (e.Distance >= _enemyDistance) return; // ignore farther robots
                // Closer enemy found — switch target
            }

            // ── Detect incoming bullet (energy drop) → dodge ────────
            double energyDrop = _enemyEnergy - e.Energy;
            if (energyDrop > 0.09 && energyDrop <= 3.0)
                _orbitDir *= -1;

            // ── Store enemy data ─────────────────────────────────────
            _enemyName       = e.Name;
            _enemyDistance   = e.Distance;
            _enemyHeadingRad = e.HeadingRadians;
            _enemyVelocity   = e.Velocity;
            _enemyEnergy     = e.Energy;
            _lostTicks       = 0;

            double absBearing = HeadingRadians + e.BearingRadians;
            _enemyX = X + e.Distance * Math.Sin(absBearing);
            _enemyY = Y + e.Distance * Math.Cos(absBearing);

            // ── Switch to Crush mode ─────────────────────────────────
            if (_mode == Mode.Hunt)
                EnterCrushMode();
        }

        // ============================================================
        //  EVENT: Enemy dies → back to Hunt immediately
        // ============================================================
        public override void OnRobotDeath(RobotDeathEvent e)
        {
            if (e.Name == _enemyName)
                EnterHuntMode();
        }

        // ============================================================
        //  EVENT: Bullet hit enemy — we're on target
        // ============================================================
        public override void OnBulletHit(BulletHitEvent e)
        {
            // Keep closing in aggressively
            _orbitRadius = Math.Max(MinOrbitRadius, _orbitRadius - 10);
        }

        // ============================================================
        //  EVENT: Hit by bullet → flip orbit direction
        // ============================================================
        public override void OnHitByBullet(HitByBulletEvent e)
        {
            _orbitDir *= -1;
        }

        // ============================================================
        //  EVENT: Rammed wall
        // ============================================================
        public override void OnHitWall(HitWallEvent e)
        {
            _orbitDir *= -1;
        }

        // ============================================================
        //  EVENT: Rammed robot
        // ============================================================
        public override void OnHitRobot(HitRobotEvent e)
        {
            // We've reached ram distance — unload everything
            Fire(3.0);
        }

        // ============================================================
        //  MODE SWITCHES
        // ============================================================
        private void EnterHuntMode()
        {
            _mode        = Mode.Hunt;
            _enemyName   = null;
            _orbitRadius = 300;     // reset spiral radius for next target
            _lostTicks   = 0;
            Out.WriteLine("[HUNT] Scanning for targets...");
        }

        private void EnterCrushMode()
        {
            _mode      = Mode.Crush;
            _lostTicks = 0;
            Out.WriteLine($"[CRUSH] Target acquired: {_enemyName}");
        }

        // ============================================================
        //  WALL AVOIDANCE
        // ============================================================
        private void AvoidWalls()
        {
            const double margin = 50;
            bool nearWall = X < margin || X > BattleFieldWidth  - margin ||
                            Y < margin || Y > BattleFieldHeight - margin;
            if (nearWall)
            {
                double cx = BattleFieldWidth  / 2.0;
                double cy = BattleFieldHeight / 2.0;
                double ang = Math.Atan2(cx - X, cy - Y) * 180.0 / Math.PI;
                SetTurnRight(NormalizeBearing(ang - Heading));
                SetAhead(60);
            }
        }

        // ============================================================
        //  HELPERS
        // ============================================================
        private static double NormalizeBearing(double angle)
        {
            while (angle >  180.0) angle -= 360.0;
            while (angle < -180.0) angle += 360.0;
            return angle;
        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;

        private static double Clamp(double v, double min, double max)
            => Math.Max(min, Math.Min(max, v));
    }
}