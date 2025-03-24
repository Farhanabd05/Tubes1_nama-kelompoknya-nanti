using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class AggressiveRam : Bot
{

    int turnSpeed = 360;

    int scanDirection = 1;

    private Random rng = new Random(); // Declare random generator

    // The main method starts our bot
    static void Main(string[] args)
    {
        new AggressiveRam().Start();
    }

    // Constructor, which loads the bot settings file
    AggressiveRam() : base(BotInfo.FromFile("AggressiveRam.json")) { }

    // Called when a new round is started -> initialize and do some movement
    public override void Run()
    {
        // Set colors
        BodyColor = Color.FromArgb(0xFF, 0xCC, 0x00);   // Yellow (Gold)
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00); // Red
        RadarColor = Color.FromArgb(0x00, 0x00, 0x00);  // Black
        BulletColor = Color.FromArgb(0x00, 0xFF, 0xFF); // Cyan (RGB: 0, 255, 255)

        AdjustRadarForBodyTurn = false;
        AdjustRadarForGunTurn = false;
        AdjustGunForBodyTurn = false;

        // Spin the gun around FAST
        while (IsRunning)
        {

            TurnRadarLeft(turnSpeed*scanDirection);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {   
        var get_radar_angle = RadarBearingTo(e.X, e.Y);
        var get_gun_angle = GunBearingTo(e.X, e.Y);
        var get_body_angle = BearingTo(e.X, e.Y);
        var distance = DistanceTo(e.X, e.Y); // Get distance to enemy
        
        // Narrow Beam
        SetTurnRadarLeft(get_radar_angle);
        SetTurnGunLeft(get_gun_angle);


        // Oscillating Beam
        // scanDirection *= -1;

        // SetTurnRadarLeft(turnSpeed * scanDirection);
        // SetTurnGunLeft(get_gun_angle);
        if(get_gun_angle <= 5 && Energy >= 50){
            if(distance > 100){
                SetFire(1);
            }
            else{
                SetFire(3);
            }
        }
        else if(distance <= 50){
            SetFire(3);
        }
        else if(distance <= 100){
            SetFire(2);
        }

        SetTurnLeft(get_body_angle/8);
        // SetForward(Math.Min(distance / 2, 150));
        SetForward(distance);

        SetRescan();

    }
    public override void OnHitBot(HitBotEvent e)
    {   
        var get_radar_angle = RadarBearingTo(e.X, e.Y);
        var get_gun_angle = GunBearingTo(e.X, e.Y);
        var get_body_angle = BearingTo(e.X, e.Y);
        var distance = DistanceTo(e.X, e.Y);
        
        // Keep gun locked on target
        SetTurnRadarLeft(get_radar_angle);
        SetTurnGunLeft(get_gun_angle);
        SetTurnLeft(get_body_angle);
        
        if (e.Energy > 12)
            SetFire(3);
        else if (e.Energy > 7.5)
            SetFire(2);
        else if (e.Energy > 3)
            SetFire(1);
        else if (e.Energy > 1.5)
            SetFire(.5);
        else if (e.Energy > .3)
            SetFire(.1);

    }


}