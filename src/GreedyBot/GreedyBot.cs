using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class GreedyBot : Bot
{
    // Constants
    private const double SAFE_DISTANCE_FROM_WALL = 50;
    private const double MIN_FIRE_POWER = 1.0;
    private const double MID_FIRE_POWER = 2.0;
    private const double MAX_FIRE_POWER = 3.0;
    
    // Variables
    private ScannedBotEvent currentTarget; // Musuh yang lagi dibidik
    private int missedShots = 0; // Berapa kali kita meleset, biar gak malu-maluin
    private int hitCount = 0; // Berapa kali kena musuh, buat pamer
    private double lastEnemyEnergy = 100; // Energi terakhir musuh, buat ngecek apa dia nembak apa kagak
    private int moveDirection = 1; // Arah gerak, 1 maju, -1 mundur
    private Random random = new Random(); // Buat bikin keputusan random, biar gak gampang ditebak

    static void Main(string[] args)
    {
        new GreedyBot().Start();
    }
    
    GreedyBot() : base(BotInfo.FromFile("GreedyBot.json")) 
    {
        Console.WriteLine("Initializing GreedyBot with configuration file");
    }
    
    public override void Run()
    {
        Console.WriteLine("Starting new round with greedy strategy");
        BodyColor = Color.Green;
        GunColor = Color.Red;
        RadarColor = Color.Yellow;
        BulletColor = Color.Orange;
        ScanColor = Color.Blue;
        
        AdjustGunForBodyTurn = true;
        AdjustRadarForGunTurn = true;
        
         // Loop utama, jalan terus sampe game berakhir
        while (IsRunning)
        {
            Console.WriteLine($"\nTurn {TurnNumber} - Energi: {Energy}");
            TurnRadarRight(360); // Scan sekeliling, cari musuh
            
            // Kalo gak ada musuh, jalan random aja
            if (currentTarget == null)
            {
                Console.WriteLine("Gak ada musuh nih, jalan random aja dah");
                if (random.Next(10) < 3) // 30% chance buat ganti arah
                {
                    moveDirection = -moveDirection;
                    Console.WriteLine($"Ganti arah jalan: {moveDirection}");
                }
                TurnRight(random.Next(90) * moveDirection);
                Forward(100 * moveDirection);
            }
            
            AvoidWalls(); // Hindari tembok biar gak nabrak mulu
            Go(); // Jalanin semua perintah yang udah diset
        }
    }
    
    // Method buat hindari tembok, penting nih bro
    private void AvoidWalls()
    {
        double minDistance = Math.Min(
            Math.Min(X, ArenaWidth - X),
            Math.Min(Y, ArenaHeight - Y)
        );
        
        Console.WriteLine($"Wall proximity check: {minDistance} units");
        
        if (minDistance < SAFE_DISTANCE_FROM_WALL)
        {
            Console.WriteLine("Initiating wall avoidance maneuver");
            double centerBearing = BearingTo(ArenaWidth/2, ArenaHeight/2);
            TurnRight(centerBearing);
            Forward(100);
            Console.WriteLine($"Moving towards center at bearing {centerBearing}°");
        }
    }
    // Event handler pas nemu musuh
    public override void OnScannedBot(ScannedBotEvent e)
    {
        Console.WriteLine($"Scanned target: {e.ScannedBotId} at ({e.X}, {e.Y})");
        
        // Greedy selection: closest target
        if (currentTarget == null || DistanceTo(e.X, e.Y) < DistanceTo(currentTarget.X, currentTarget.Y))
        {
            currentTarget = e;
            Console.WriteLine($"New primary target: {e.ScannedBotId}");
        }
        
        // Energy change detection
        if (lastEnemyEnergy > e.Energy && lastEnemyEnergy - e.Energy <= 3)
        {
            Console.WriteLine($"Enemy fire detected! Energy drop: {lastEnemyEnergy - e.Energy}");
            moveDirection = -moveDirection;
            TurnRight(90 * moveDirection);
            Forward(100 * moveDirection);
            Console.WriteLine($"Evasive maneuver: moving {moveDirection} direction");
        }
        
        lastEnemyEnergy = e.Energy;
        
        // Targeting system
        double gunBearing = GunBearingTo(e.X, e.Y);
        TurnGunRight(gunBearing);
        Console.WriteLine($"Adjusting gun to bearing {gunBearing}°");

        if (Math.Abs(gunBearing) < 3 && GunHeat == 0)
        {
            double distance = DistanceTo(e.X, e.Y);
            double firePower = GetOptimalFirePower(distance);
            
            if (Energy > firePower + 0.1)
            {
                Console.WriteLine($"Firing at {firePower} power");
                Fire(firePower);
            }
            else if (Energy > 0.1)
            {
                Console.WriteLine("Low energy - using minimum firepower");
                Fire(0.1);
            }
        }
        
        // Movement strategy
        double targetDistance = DistanceTo(e.X, e.Y);
        if (targetDistance < 150)
        {
            Console.WriteLine("Close combat protocol activated");
            TurnRight(BearingTo(e.X, e.Y) + 180);
            Back(100);
        }
        else
        {
            Console.WriteLine("Approaching target with zigzag pattern");
            TurnRight(BearingTo(e.X, e.Y) + (45 * moveDirection));
            Forward(100);
        }
    }


    // Method buat nentuin daya tembak optimal
    private double GetOptimalFirePower(double distance)
    {
        Console.WriteLine($"Calculating firepower for distance: {distance}");
        return distance < 100 ? MAX_FIRE_POWER :
               distance < 300 ? MID_FIRE_POWER : 
               MIN_FIRE_POWER;
    }
    // Event handler pas peluru kita kena musuh,
    public override void OnBulletHit(BulletHitBotEvent e)
    {
        Console.WriteLine($"Successful hit on {e.VictimId}");
        hitCount++;
        missedShots = 0;
        Console.WriteLine($"Total hits: {hitCount} | Damage dealt: {e.Damage}");
        Rescan();
    }

    // Event handler pas peluru kita kena musuh,
    public override void OnBulletHitWall(BulletHitWallEvent e)
    {
        Console.WriteLine($"Missed shot at ({e.Bullet.X}, {e.Bullet.Y})");
        if (++missedShots > 3)
        {
            Console.WriteLine("Reset target tracking due to poor accuracy");
            currentTarget = null;
            missedShots = 0;
            TurnRadarRight(360);
        }
    }

    // Event handler pas peluru kita kena musuh,
    public override void OnHitByBullet(HitByBulletEvent e)
    {
        Console.WriteLine($"Hit by {e.Bullet.OwnerId}'s bullet");
        double evasionAngle = 90 - BearingTo(e.Bullet.X, e.Bullet.Y);
        TurnRight(evasionAngle);
        Forward(100 * moveDirection);
        moveDirection = -moveDirection;
        Console.WriteLine($"Evasion angle: {evasionAngle}° | New direction: {moveDirection}");
    }

    // Event handler pas peluru kita kena musuh,
    public override void OnHitBot(HitBotEvent e)
    {
        Console.WriteLine($"Collision with {e.VictimId}");
        TurnRight(BearingTo(e.X, e.Y) + 90);
        Back(50);
        Console.WriteLine($"Retreating from collision at bearing {BearingTo(e.X, e.Y)}°");
    }

    // Event handler pas kita nabrak tembok, aduh!
    public override void OnHitWall(HitWallEvent e)
    {
        Console.WriteLine("Wall collision detected");
        double centerBearing = BearingTo(ArenaWidth/2, ArenaHeight/2);
        TurnRight(centerBearing);
        Forward(100);
        moveDirection = -moveDirection;
        Console.WriteLine($"Recovery bearing: {centerBearing}°");
    }
}

