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

        public bool ShowLineOfSight = false;

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

        // Auto-Skill Settings
        public bool EnableAutoSkill = false;
        
        public int AutoSkillKey = 81; // Q key by default
        
        public float AutoSkillRange = 50f;
        
        public float AutoSkillCooldown = 1.0f; // seconds between skill uses
        
        public bool AutoSkillHoldKey = false; // false = press/release, true = hold key
        
        public bool AutoSkillOnlyInCombat = true; // only use skill when targeting monsters
        
        public int AutoSkillKeyHoldDuration = 100; // milliseconds to hold key if AutoSkillHoldKey is false
        
        public bool ShowAutoSkillRange = false; // show visual circle for auto-skill range

        // Auto-Chest Settings
        public bool EnableAutoChest = false;
        
        public bool OpenRegularChests = true; // open normal chests
        
        public bool OpenStrongboxes = false; // open strongboxes (more valuable but riskier)
        
        public float AutoChestRange = 60f; // range to detect and open chests
        
        public bool OnlyOpenWhenSafe = true; // only open when no monsters nearby
        
        public float SafetyCheckRange = 80f; // range to check for monsters before opening
        
        public bool ShowChestRange = false; // show visual circle for chest detection range
        
        public bool ShowSafetyRange = false; // show visual circle for safety check range
        
        public float ChestCooldown = 0.5f; // seconds between chest interactions
    }
}