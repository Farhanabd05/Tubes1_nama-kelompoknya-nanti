using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class CircularBot : Bot
{

    private int turnSpeed = 360;       
    private int scanDirection = 1;     
    private int orbit_angle = 90;    //angle used for orbiting around a scanned bot.
    private Random rng = new Random();// Random generator for any random behavior.

    private double wallMargin = 50;              // margin from walls.
    private double projectionDistance = 100;     // stick for wall smoothing.

    private const double battlefieldWidth = 782;
    private const double battlefieldHeight = 582;
    // **** End Field Declarations ****

    static void Main(string[] args)
    {
        new CircularBot().Start();
    }

    CircularBot() : base(BotInfo.FromFile("CircularBot.json")) { }

    public override void Run()
    {
        BodyColor = Color.FromArgb(0x80, 0x00, 0x80);   // Purple  
        TurretColor = Color.FromArgb(0x00, 0x80, 0x00);   // Green  
        RadarColor = Color.FromArgb(0x80, 0x00, 0x00);    // Orange  
        BulletColor = Color.FromArgb(0xFF, 0x00, 0x00);    // Red 

        AdjustRadarForBodyTurn = false;
        AdjustRadarForGunTurn = false;
        AdjustGunForBodyTurn = false;

        while (IsRunning)
        {
            TurnRadarLeft(turnSpeed * scanDirection);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {   
        double get_radar_angle = RadarBearingTo(e.X, e.Y);
        double get_gun_angle = GunBearingTo(e.X, e.Y);
        double get_body_angle = BearingTo(e.X, e.Y);
        double distance = DistanceTo(e.X, e.Y);

        // Narrow beam adjustments.
        SetTurnRadarLeft(get_radar_angle);

        double desiredHeading = NormalizeAngle(get_body_angle + orbit_angle);
        // Apply wall smoothing to adjust the heading if the projected position is near a wall.
        double smoothedHeading = ApplyWallSmoothing(desiredHeading);
        
        
        SetTurnLeft(smoothedHeading);
        SetForward(100);

        // Predictive Shooting
        double firepower = 2;
        double bulletSpeed = 20 - (3*firepower);
        double predictedX, predictedY;
        PredictEnemyPosition(e, bulletSpeed, out predictedX, out predictedY);
        double fireAngle = GunBearingTo(predictedX, predictedY);
        
        SetTurnGunLeft(fireAngle-1);
        if(distance <= 100){
            Fire(firepower);   
        }

        SetRescan();
    }

    // Adjust the desired heading until the projected position is within safe bounds.
    private double ApplyWallSmoothing(double desiredAngle)
    {
        double smoothedAngle = desiredAngle;
        int iterations = 0;
        // Loop until the projected position is safe or a full rotation has been attempted.
        while (iterations < 360)
        {
            // Project a point ahead along the current smoothed angle.
            double rad = DegreesToRadians(smoothedAngle);
            double projectedX = X + projectionDistance * Math.Cos(rad);
            double projectedY = Y + projectionDistance * Math.Sin(rad);

            if (IsWithinSafeBounds(projectedX, projectedY))
            {
                break;
            }
            // Adjust the angle by a small increment (e.g., 90 degrees) and normalize.
            smoothedAngle = NormalizeAngle(smoothedAngle + 45);
            iterations += 45;
        }
        return smoothedAngle;
    }

    private void PredictEnemyPosition(ScannedBotEvent e, double bulletSpeed, out double predictedX, out double predictedY)
    {
        double time = DistanceTo(e.X, e.Y) / bulletSpeed;
        double enemySpeed = e.Speed;
        double enemyHeading = e.Direction;

        predictedX = e.X + enemySpeed * time * Math.Cos(DegreesToRadians(enemyHeading));
        predictedY = e.Y + enemySpeed * time * Math.Sin(DegreesToRadians(enemyHeading));
    }

    private bool IsWithinSafeBounds(double x, double y)
    {
        return x > wallMargin && x < (battlefieldWidth - wallMargin) &&
            y > wallMargin && y < (battlefieldHeight - wallMargin);
    }

    private double DegreesToRadians(double degrees)
    {
        return (Math.PI / 180) * degrees;
    }

    private double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 360;
        while (angle >= 360) angle -= 360;
        return angle;
    }

    public override void OnHitBot(HitBotEvent e)
    {   
        // if stuck on wall just shoot it down
        SetFire(3);
    }   


}