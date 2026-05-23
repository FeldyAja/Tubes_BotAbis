using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;

// ---------------------------------------------------------------
// BotPredator
// ---------------------------------------------------------------
// Bot agresif berbasis strategi ram (menabrak).
//
// Perilaku utama:
//  - Radar hanya menyapu 90° di depan untuk lock musuh di hadapan
//  - Jika scan musuh, langsung kejar dan tembak firepower 1.5
//  - Jika berhasil menabrak, mundur lalu tembak firepower 3, ram lagi
//  - Jika 10 turn tanpa scan, radar putar 360° untuk mencari musuh baru
// ---------------------------------------------------------------
public class BotPredator : Bot
{
    // ---- State machine ----
    private enum BotState { Searching, Chasing, PostRam }
    private BotState state = BotState.Searching;

    // ---- Target tracking ----
    private int    targetBotId        = -1;
    private double targetX;
    private double targetY;

    // ---- Counter turn tanpa scan ----
    private int turnsSinceLastScan = 0;

    // ---- Arah sweep radar (kanan/kiri bergantian) ----
    private bool radarSweepRight = true;

    // ---------------------------------------------------------------
    static void Main() => new BotPredator().Start();
    BotPredator() : base(BotInfo.FromFile("BotPredator.json")) { }

    // ---------------------------------------------------------------
    // Run – loop utama bot
    // ---------------------------------------------------------------
    public override void Run()
    {
        // Warna bot: tema merah predator
        BodyColor   = Color.Red;
        TurretColor = Color.Yellow;
        RadarColor  = Color.Cyan;
        BulletColor = Color.Yellow;
        ScanColor   = Color.Red;

        // Radar dan gun bergerak independen dari rotasi badan
        AdjustRadarForBodyTurn = true;
        AdjustGunForBodyTurn   = true;
        AdjustRadarForGunTurn  = true;

        while (IsRunning)
        {
            switch (state)
            {
                case BotState.PostRam:
                    HandlePostRam();
                    break;

                case BotState.Chasing:
                    HandleChasing();
                    break;

                case BotState.Searching:
                default:
                    HandleSearching();
                    break;
            }
        }
    }

    // ---------------------------------------------------------------
    // State: Searching
    // Tidak ada target – sweep radar 90° di depan.
    // Jika 10 turn tanpa hasil, lakukan scan 360°.
    // ---------------------------------------------------------------
    private void HandleSearching()
    {
        turnsSinceLastScan++;

        if (turnsSinceLastScan >= 10)
        {
            // Sudah terlalu lama tidak menemukan musuh → putar penuh
            TurnRadarRight(360); // blocking sampai selesai

            // Jika setelah 360° masih tidak ada target, coba lagi dalam 5 turn
            if (state == BotState.Searching)
                turnsSinceLastScan = 5;
        }
        else
        {
            // Sweep radar 90° di area depan badan (±45°)
            SetRadarSweepFront();
            Go();
        }
    }

    // ---------------------------------------------------------------
    // State: Chasing
    // Punya target – putar badan ke target, arahkan laras, maju, tembak 1.5.
    // ---------------------------------------------------------------
    private void HandleChasing()
    {
        turnsSinceLastScan++;

        // Jika terlalu lama kehilangan target, kembali ke mode search
        if (turnsSinceLastScan >= 10)
        {
            state       = BotState.Searching;
            targetBotId = -1;
            return;
        }

        // Putar badan agar menghadap target
        double bodyBearing = BearingTo(targetX, targetY);
        SetTurnLeft(bodyBearing);

        // --- FIX: Arahkan laras (gun) langsung ke target ---
        // GunBearingTo menghitung sudut relatif dari arah laras ke target.
        // Karena AdjustGunForBodyTurn = true, laras TIDAK ikut badan otomatis,
        // sehingga harus diputar manual setiap tick.
        double gunBearing = GunBearingTo(targetX, targetY);
        SetTurnGunLeft(gunBearing);

        // Maju penuh ke arah target (nilai besar agar tidak berhenti di tengah)
        SetForward(10000);

        // Tembak setelah laras mengarah – firepower 1.5 untuk jarak jauh
        SetFire(1.5);

        // Jaga radar tetap menyapu di depan badan agar target tetap terdeteksi
        SetRadarSweepFront();

        Go();
    }

    // ---------------------------------------------------------------
    // State: PostRam
    // Baru saja menabrak musuh – mundur, arahkan laras, tembak keras, ram lagi.
    // ---------------------------------------------------------------
    private void HandlePostRam()
    {
        // Mundur sedikit untuk memberi jarak tembak yang optimal
        Back(40);

        // --- FIX: Arahkan laras ke target sebelum tembak ---
        // Setelah mundur, badan mungkin sudah tidak lurus ke musuh,
        // jadi kita putar laras ke posisi target terakhir yang diketahui.
        double gunBearing = GunBearingTo(targetX, targetY);
        TurnGunLeft(gunBearing); // blocking agar laras benar-benar terarah dulu

        // Tembak firepower 3 karena posisi sangat dekat dengan musuh
        Fire(3);

        // Kembali ke mode chasing agar segera ram lagi
        state = BotState.Chasing;

        // Set maju agar langsung bergerak di iterasi berikutnya
        SetForward(10000);
        Go();
    }

    // ---------------------------------------------------------------
    // Helper: Atur radar agar sweep ±45° dari arah badan (total 90°).
    // Dipanggil di setiap tick saat Searching maupun Chasing.
    // ---------------------------------------------------------------
    private void SetRadarSweepFront()
    {
        // Hitung berapa derajat radar harus berputar agar menghadap badan
        double bearingToBodyDirection = CalcRadarBearing(Direction);

        // Tambahkan offset ±45° untuk efek sweep bolak-balik
        // radarSweepRight = true  → geser 45° ke kanan dari depan badan
        // radarSweepRight = false → geser 45° ke kiri dari depan badan
        double sweepOffset = radarSweepRight ? -45.0 : 45.0;
        SetTurnRadarLeft(bearingToBodyDirection + sweepOffset);

        radarSweepRight = !radarSweepRight;
    }

    // ---------------------------------------------------------------
    // Event: Musuh terdeteksi radar
    // ---------------------------------------------------------------
    public override void OnScannedBot(ScannedBotEvent e)
    {
        turnsSinceLastScan = 0;

        if (state == BotState.Searching)
        {
            // Pertama kali menemukan musuh – lock target dan mulai kejar
            targetBotId = e.ScannedBotId;
            targetX     = e.X;
            targetY     = e.Y;
            state       = BotState.Chasing;
        }
        else if (e.ScannedBotId == targetBotId)
        {
            // Update posisi target yang sedang dikejar agar tidak ketinggalan
            targetX = e.X;
            targetY = e.Y;
        }
        // Bot lain yang terscan saat Chasing diabaikan –
        // bot tetap fokus pada target yang sudah di-lock
    }

    // ---------------------------------------------------------------
    // Event: Bot kita menabrak bot lain
    // ---------------------------------------------------------------
    public override void OnHitBot(HitBotEvent e)
    {
        // Masuk mode PostRam untuk mundur dan tembak sebelum ram lagi
        state = BotState.PostRam;
    }

    // ---------------------------------------------------------------
    // Event: Bot kita menabrak dinding
    // ---------------------------------------------------------------
    public override void OnHitWall(HitWallEvent e)
    {
        // Mundur dari dinding agar tidak stuck
        SetBack(30);

        // Arahkan kembali ke target, atau putar 90° jika tidak ada target
        if (targetBotId >= 0)
            SetTurnLeft(BearingTo(targetX, targetY));
        else
            SetTurnRight(90);

        Go();
    }

    // ---------------------------------------------------------------
    // Event: Bot musuh mati
    // ---------------------------------------------------------------
    public override void OnBotDeath(Robocode.TankRoyale.BotApi.Events.BotDeathEvent e)
    {
        // Jika target kita yang mati, lepas lock dan cari musuh baru
        if (e.VictimId == targetBotId)
        {
            targetBotId        = -1;
            state              = BotState.Searching;
            turnsSinceLastScan = 10; // Trigger full 360 scan segera
        }
    }
}