using System.Numerics;
using System.IO;
using System.Text.Json;
using GameHelper.Plugin;
using GameHelper.RemoteEnums;

namespace AutoAim
{

    public enum TargetPriority
    {
        Closest,
        LowestHealth,
        HighestRarity
    }

   
    public sealed class AutoAimSettings : IPSettings
    {
        
        private static readonly string ConfigFilePath = Path.Combine(Directory.GetCurrentDirectory(), "AutoAimConfig.json");
        public bool EnableAutoAim = false;


   
        public bool IsEnabled
        {
            get => EnableAutoAim;
            set => EnableAutoAim = value;
        }

    
        public int ToggleKey = 114; // F3 by default

 
        public float MouseSpeed = 1.0f;


        public bool SmoothMovement = true;

 
        public bool ShowWalkableGrid = false;


        public bool EnableLineOfSight = true;


        public bool ShowRangeCircle { get; set; } = false;


        public bool ShowTargetLines = false;


        public bool ShowDebugWindow = false;

        public bool BeepSound = true;

        public bool IsToggleActive = false;

        public float TargetingRange = 90f;

        public float RayCastRange = 60f;

        public bool TargetNormal = true;

        public bool TargetMagic = true;

        public bool TargetRare = true;

        public bool TargetUnique = true;

        public float AimSpeed = 1.0f;

        public float AimSmoothing = 0.3f;

        public bool AimAtCenter = true;

        public TargetPriority TargetPriority = TargetPriority.Closest;

        public bool ShowTargetIndicator = true;

        public float TargetSwitchDelay = 100f;

        public bool PreferClosest = true;
    }
}