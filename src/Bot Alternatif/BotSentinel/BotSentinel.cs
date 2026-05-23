using System;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;

public class BotSentinel : Bot
{
    // Penanda apakah bot sudah berhasil menempel di tembok
    bool sudahDiTembok = false;
    
    // Penentu arah (1 untuk jalan maju, -1 untuk jalan mundur)
    int arahGerak = 1;

    static void Main(string[] args)
    {
        new BotSentinel().Start();
    }

    public BotSentinel() : base(BotInfo.FromFile("BotSentinel.json")) { }

    public override void Run()
    {
        // Kosmetik bot
        BodyColor = Color.Gray;
        TurretColor = Color.Black;
        RadarColor = Color.Cyan;
        BulletColor = Color.Red;
        ScanColor = Color.White;

        // Pisahkan engsel meriam dari badan tank
        // Ini wajib agar badan bisa lurus bolak-balik, tapi meriam bebas berputar
        AdjustGunForBodyTurn = true;

        // Nyari dan Nempel di 1 tembok
        // 1. Putar badan menghadap lurus ke tembok terdekat (0, 90, 180, atau 270)
        TurnRight(Direction % 90);
        
        // 2. Gas terus sampai menabrak tembok tersebut
        Forward(50000); 

        // 3. Setelah menabrak (OnHitWall selesai dieksekusi), tandai status true
        sudahDiTembok = true;

        // 4. Mundur sedikit agar badan gak nempel banget ke tembok
        Back(15);

        // 5. Putar 90 derajat agar sejajar dengan tembok, siap bolak-balik sepanjang tembok
        TurnRight(90);

        // LOOP Bolak-balik meng-scan area
        while (IsRunning)
        {
            // Bergerak mondar-mandir dari ujung ke ujung tembok
            SetForward(40000 * arahGerak);
            
            // Putar meriam terus-menerus untuk menyapu ruang terbuka di depan (dan samping serta belakang sebagai perlawananbila ada bot spesies Wall lainnya)
            TurnGunRight(360); 
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // GREEDY OFFENSE (Fokus Area Depan)
        // Saat melihat musuh di ruang terbuka, langsung tembak dengan power 1.5
        Fire(1.5);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // GREEDY DEFENSE (Anti-Ram)
        // Jika ada musuh yang menabrak
        // peluru maksimal (3.0) ditembakkan
        Fire(3.0);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Jika status sudahDiTembok = true, berarti saat ini kita sedang bolak-balik
        // lalu pantat atau moncong tank menyentuh sudut/ujung tembok.
        if (sudahDiTembok)
        {
            // Mundur/Maju beberapa piksel dari sudut tembok agar kagak nyangkut (stuck)
            if (arahGerak == 1) 
            {
                Back(15); // Kalau tadi jalan maju, berarti moncong yang nabrak. Jadi mundur
            } 
            else 
            {
                Forward(15); // Kalau tadi jalan mundur, berarti pantat yang nabrak. Jadi Maju
            }

            // Balik arah gerak (Maju jadi Mundur, Mundur jadi Maju)
            arahGerak *= -1;
        }
    }
}