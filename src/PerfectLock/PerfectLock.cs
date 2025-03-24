using System;
using System.Drawing;
using System.Windows.Forms;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class PerfectLock : Bot
{
    // Variable untuk menyimpan ID target yang sedang dilacak

    static void Main(string[] args)
    {
        new PerfectLock().Start();
    }

    // Konstruktor menggunakan konfigurasi dari file
    PerfectLock() : base(BotInfo.FromFile("PerfectLock.json")) { }

    private int dir = 1;
    private Random random = new Random();
    /**
     * run: Robot's default behavior.
     * This implementation moves in a zigzag pattern.
     */
    public override void Run()
    {
        // Optionally set colors
        BodyColor = System.Drawing.Color.Black;
        GunColor = System.Drawing.Color.Red;
        RadarColor = System.Drawing.Color.Yellow;
        BulletColor = System.Drawing.Color.Green;
        ScanColor = System.Drawing.Color.Orange;

        // Main loop: keep moving while the bot is active.
        AdjustRadarForBodyTurn = true;
        AdjustGunForBodyTurn = true;
        AdjustRadarForGunTurn = false;
        while (IsRunning)
        {
            TurnRadarRight(360);
    }

    /**
     * onScannedBot: What to do when you see another robot.
     * Uses built-in DistanceTo and BearingTo to aim the gun and chooses firepower based on distance.
     */
    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Jika belum ada target atau bot yang baru terdeteksi lebih dekat, update target
        // Compute distance and bearing to the scanned bot.
        double directionToTarget = 180 * Math.Atan2(e.Y - Y, e.X - X) / Math.PI;
        double radarTurn = NormalizeRelativeAngle(directionToTarget - RadarDirection);
        double gunTurn = NormalizeRelativeAngle(directionToTarget - GunDirection);
        // Turn the gun toward the enemy.
        if (TurnNumber % 2 == 1) {
            dir *= -1
        }
        SetTurnRadarRight(Math.Clamp(radarTurn*dir, -20, 20));

        SetTurnGunRight(Math.Clamp(gunTurn*dir, -20, 20));
        Console.WriteLine($"Radar masih memindai: {RadarTurnRemaining} derajat tersisa");
        // Ambil data musuh dari scan terakhir
        double enemyX = e.X;
        double enemyY = e.Y;
        double enemyDirection = e.Direction; // arah musuh dalam derajat
        double enemySpeed = e.Speed;

        // Konversi arah ke radian
        double enemyDirRad = (enemyDirection - 90) * Math.PI / 180.0;

        // Prediksi posisi musuh untuk turn berikutnya (asumsi kecepatan dan arah konstan)
        double predictedEnemyX = enemyX + Math.Cos(enemyDirRad) * enemySpeed;
        double predictedEnemyY = enemyY + Math.Sin(enemyDirRad) * enemySpeed;

        // Tentukan titik target di belakang musuh.
        // Misalnya, kita ingin berada 'offset' unit di belakang arah gerak musuh.
        double offset = 150; // sesuaikan jarak offset sesuai kebutuhan
        double targetX = predictedEnemyX - Math.Cos(enemyDirRad) * offset;
        double targetY = predictedEnemyY - Math.Sin(enemyDirRad) * offset;

        // Hitung sudut yang harus dihadapi bot kita untuk menuju target tersebut
        double angleToTarget = 180 * Math.Atan2(targetY - Y, targetX - X) / Math.PI; // fungsi ini menghitung sudut absolut dari posisi bot ke target
        double relativeAngle = NormalizeRelativeAngle(angleToTarget - Direction);

        double gunTurn = NormalizeRelativeAngle(angleToTarget - GunDirection);
        var distance = DistanceTo(targetX, targetY);
        // Atur perintah berputar ke target
        if (relativeAngle >= 0)
        {
            SetForward(100);
            SetTurnRight(-relativeAngle);
            // SetTurnGunRight(Math.Clamp(gunTurn, -20, 20));
            Go();
        }
        else
        {
            SetForward(100);
            SetTurnLeft(relativeAngle);
            // SetTurnGunLeft(Math.Clamp(gunTurn, -20, 20));
            Go();
        }

        if (distance < 20 && Energy > 3 && GunHeat == 0) {
            Fire(2);
        } else if (distance < 100) {
            Fire(1);
        }
        
        // Jika sudut perbedaan kecil (misalnya kurang dari 10°), lanjutkan maju menuju target
        if (Math.Abs(relativeAngle) < 10)
        {
            TargetSpeed = MaxSpeed;
        }
        else
        {
            TargetSpeed = 0; // hentikan maju jika masih perlu koreksi arah
        }
    } else {
        SetTurnRadarRight(360);
    }

        // Lakukan scanning ulang tiap turn
        Go();
        Rescan();
    }

    }

    public override void OnHitBot(HitBotEvent e)
    {   
        var get_radar_angle = RadarBearingTo(e.X, e.Y);
        var get_gun_angle = GunBearingTo(e.X, e.Y);
        var get_body_angle = BearingTo(e.X, e.Y);
        
        // Keep gun locked on target
        SetTurnRadarLeft(get_radar_angle);
        SetTurnGunLeft(get_gun_angle);
        Fire(3);

        Forward(50);
    }
}