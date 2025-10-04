using System;
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
    
    // Enum for combo action types
    public enum ComboActionType
    {
        KeyPress = 0,     // Regular key/key combination
        KeyPressAndHold = 1, // Press key and hold for X seconds
        LeftClick = 2,    // Left mouse click
        RightClick = 3,   // Right mouse click
        MiddleClick = 4,  // Middle mouse click
        HoldLeftClick = 5, // Hold left click (for channeling)
        ReleaseLeftClick = 6 // Release held left click
    }
    
    // Serializable structures for the new combo system
    [System.Serializable]
    public class SerializableKeyCombination
    {
        public int MainKey { get; set; }
        public bool UseCtrl { get; set; }
        public bool UseShift { get; set; }
        public bool UseAlt { get; set; }
        
        public SerializableKeyCombination() { }
        
        public SerializableKeyCombination(int mainKey, bool ctrl = false, bool shift = false, bool alt = false)
        {
            MainKey = mainKey;
            UseCtrl = ctrl;
            UseShift = shift;
            UseAlt = alt;
        }
    }
    
    [System.Serializable]
    public class SerializableComboAction
    {
        public ComboActionType ActionType { get; set; } = ComboActionType.KeyPress;
        public SerializableKeyCombination KeyCombination { get; set; } = new SerializableKeyCombination();
        public string DisplayName { get; set; } = "";
        public float HoldDuration { get; set; } = 1.0f; // Duration for Press & Hold actions
        
        public SerializableComboAction() { }
        
        public SerializableComboAction(ComboActionType actionType)
        {
            ActionType = actionType;
            KeyCombination = new SerializableKeyCombination();
            DisplayName = GetActionDisplayName(actionType);
            HoldDuration = 1.0f;
        }
        
        public SerializableComboAction(int mainKey, bool ctrl = false, bool shift = false, bool alt = false)
        {
            ActionType = ComboActionType.KeyPress;
            KeyCombination = new SerializableKeyCombination(mainKey, ctrl, shift, alt);
            DisplayName = "Key Press";
            HoldDuration = 1.0f;
        }
        
        private string GetActionDisplayName(ComboActionType actionType)
        {
            return actionType switch
            {
                ComboActionType.KeyPress => "Key Press",
                ComboActionType.KeyPressAndHold => "Press & Hold Key",
                ComboActionType.LeftClick => "Left Click",
                ComboActionType.RightClick => "Right Click", 
                ComboActionType.MiddleClick => "Middle Click",
                ComboActionType.HoldLeftClick => "Hold Left Click",
                ComboActionType.ReleaseLeftClick => "Release Left Click",
                _ => "Unknown Action"
            };
        }
    }
    
    [System.Serializable]
    public class SerializableSkillCombo
    {
        public string Name { get; set; } = "";
        public List<SerializableComboAction> Actions { get; set; } = new List<SerializableComboAction>();
        public List<float> Delays { get; set; } = new List<float>();
        public int TargetRarity { get; set; } // Store as int for serialization
        public bool Enabled { get; set; } = true;
        
        // Combo control settings
        public float ComboCooldown { get; set; } = 5.0f; // Cooldown between combo uses (seconds)
        public bool OneTimePerTarget { get; set; } = false; // Execute only once per target
        public bool IsBuffCombo { get; set; } = false; // Special mode for buffs/weapon swaps
        public float BuffCooldown { get; set; } = 30.0f; // Cooldown for buff combos (seconds)
        
        // Legacy support for old Skills property
        public List<SerializableKeyCombination> Skills { get; set; } = new List<SerializableKeyCombination>();
        
        public SerializableSkillCombo() { }
        
        public SerializableSkillCombo(string name, int rarity)
        {
            Name = name;
            TargetRarity = rarity;
            Actions = new List<SerializableComboAction>();
            Skills = new List<SerializableKeyCombination>();
            Delays = new List<float>();
            Enabled = true;
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

        // Culling Strike Settings
        public bool EnableCullingStrike = false; // enable culling strike skill
        
        public int CullingStrikeKey = 88; // X key by default
        
        public float CullingStrikeRange = 60f; // range for culling strike
        
        public bool CullingStrikeOnlyInCombat = true; // only use culling when targeting monsters
        
        public bool ShowCullingStrikeRange = false; // show visual circle for culling strike range
        
        // Culling Strike thresholds per rarity (Normal, Magic, Rare, Unique) - same as HealthBars plugin
        public float[] CullingStrikeThresholdPerRarity = [30.0f, 20.0f, 10.0f, 5.0f];

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
        public bool EnableComboSystem = true;
        
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
        
        // New Combo System Settings
        public List<SerializableSkillCombo> SkillCombos { get; set; } = new List<SerializableSkillCombo>();
        
        // Key binding combinations for toggle and auto-skill
        public SerializableKeyCombination ToggleKeyCombination { get; set; } = new SerializableKeyCombination(114); // F3
        public SerializableKeyCombination AutoSkillKeyCombination { get; set; } = new SerializableKeyCombination(81); // Q
        public SerializableKeyCombination CullingStrikeKeyCombination { get; set; } = new SerializableKeyCombination(88); // X
        
        // Emergency override - forces mouse movement even with keys pressed
        public bool ForceMouseMovement { get; set; } = false;
    }
}