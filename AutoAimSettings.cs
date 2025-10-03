using System.Numerics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using GameHelper.Plugin;
using GameHelper.RemoteEnums;
using ClickableTransparentOverlay.Win32;

namespace AutoAim
{
    public class KeyAction
    {
        public string DisplayName { get; set; } = "";
        public bool IsCtrlPressed { get; set; } = false;
        public bool IsShiftPressed { get; set; } = false;
        public bool IsAltPressed { get; set; } = false;
        public VK Key { get; set; } = VK.NONAME;
        
        public override string ToString()
        {
            var modifiers = new List<string>();
            if (IsCtrlPressed) modifiers.Add("Ctrl");
            if (IsShiftPressed) modifiers.Add("Shift");
            if (IsAltPressed) modifiers.Add("Alt");
            
            var modStr = modifiers.Count > 0 ? string.Join("+", modifiers) + "+" : "";
            return $"{modStr}{Key}";
        }
    }

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
        
        // === COMBO SYSTEM BY RARITY ===
        public bool EnableComboSystem = false;
        
        // Normal/Magic Monsters Combo
        public bool EnableNormalCombo = true;
        public List<KeyAction> NormalComboKeys = new List<KeyAction> 
        { 
            new KeyAction { Key = VK.KEY_Q },
            new KeyAction { Key = VK.KEY_W }
        };
        public float[] NormalComboDelays = { 0.5f, 0.3f }; // delays between keys
        public bool[] NormalComboHold = { false, false }; // hold or press each key
        
        // Rare Monsters Combo  
        public bool EnableRareCombo = true;
        public List<KeyAction> RareComboKeys = new List<KeyAction> 
        { 
            new KeyAction { Key = VK.KEY_Q },
            new KeyAction { Key = VK.KEY_W },
            new KeyAction { Key = VK.KEY_E }
        };
        public float[] RareComboDelays = { 0.8f, 0.5f, 0.3f };
        public bool[] RareComboHold = { false, false, false };
        
        // Unique/Boss Monsters Combo
        public bool EnableUniqueCombo = true;
        public List<KeyAction> UniqueComboKeys = new List<KeyAction> 
        { 
            new KeyAction { Key = VK.KEY_Q },
            new KeyAction { Key = VK.KEY_W },
            new KeyAction { Key = VK.KEY_E },
            new KeyAction { Key = VK.KEY_R }
        };
        public float[] UniqueComboDelays = { 1.0f, 0.8f, 0.5f, 0.3f };
        public bool[] UniqueComboHold = { false, false, false, false };
        
        // Combo Settings
        public float ComboRange = 70f;
        public bool ComboOnlyInRange = true;
        public bool ShowComboStatus = true;
        public bool ResetComboOnTargetChange = true;
    }
}