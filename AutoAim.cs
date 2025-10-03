using System;
using System.Media;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json;
using ImGuiNET;
using GameHelper;
using GameHelper.Plugin;
using GameHelper.RemoteEnums;
using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using GameHelper.Utils;
using GameOffsets.Natives;
using System.Xml;

namespace AutoAim
{

    public sealed class AutoAim : PCore<AutoAimSettings>
    {
 
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;  

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        private string SettingPathname =>
        Path.Combine(this.DllDirectory, "config", "AutoAimConfig.json");
        private bool wasToggleKeyPressed = false;
        private Entity targetedMonster = null;
        private string _debugInfo = "";
        private string _debugInfo2 = "";
        private const int VK_LSHIFT = 0xA4;
        private const int VK_XBUTTON1 = 0x05; 
        private const int VK_XBUTTON2 = 0x06;
        
        // Auto-Skill variables
        private DateTime lastSkillUse = DateTime.MinValue;
        private bool isSkillKeyHeld = false;
        private DateTime skillKeyPressTime = DateTime.MinValue;
        
        // Combo system variables
        private List<SkillCombo> skillCombos = new List<SkillCombo>();
        private int currentComboIndex = -1;
        private int currentSkillInCombo = 0;
        private DateTime lastComboSkillTime = DateTime.MinValue;
        private bool isExecutingCombo = false;
        private Entity currentComboTarget = null;
        
        // Auto-Chest variables
        private DateTime lastChestInteraction = DateTime.MinValue;
        private Entity targetedChest = null;
        
        // Key binding system variables
        private Dictionary<string, bool> isCapturingKey = new Dictionary<string, bool>();
        private Dictionary<string, string> keyBindingLabels = new Dictionary<string, string>();
        private Dictionary<string, KeyCombination> keyCombinations = new Dictionary<string, KeyCombination>();
        
        // Structure to hold key combinations
        private struct KeyCombination
        {
            public int MainKey;
            public bool UseCtrl;
            public bool UseShift;
            public bool UseAlt;
            
            public KeyCombination(int mainKey, bool ctrl = false, bool shift = false, bool alt = false)
            {
                MainKey = mainKey;
                UseCtrl = ctrl;
                UseShift = shift;
                UseAlt = alt;
            }
        }
        
        // Structure for skill combos
        private struct SkillCombo
        {
            public string Name;
            public List<KeyCombination> Skills;
            public List<float> Delays; // Delay after each skill in seconds
            public Rarity TargetRarity;
            public bool Enabled;
            
            public SkillCombo(string name, Rarity rarity)
            {
                Name = name;
                Skills = new List<KeyCombination>();
                Delays = new List<float>();
                TargetRarity = rarity;
                Enabled = true;
            }
        }
        public override void DrawSettings()
        {
            ImGui.Text("=== AUTO AIM SETTINGS ===");
            ImGui.TextWrapped("This plugin automatically aims at nearby monsters. Use the toggle key to enable/disable.");
            ImGui.Separator();

            // Main enable/disable setting at top
            bool isEnabled = this.Settings.IsEnabled;
            if (ImGui.Checkbox("Enable Auto Aim", ref isEnabled))
            {
                this.Settings.IsEnabled = isEnabled;
            }
            ImGuiHelper.ToolTip("Enable or disable the auto aim functionality");
            
            ImGui.Separator();

            // Tab-based interface
            if (ImGui.BeginTabBar("AutoAimTabs"))
            {
                // Tab 1: Keybind Settings
                if (ImGui.BeginTabItem("Keybind"))
                {
                    ImGui.Text("Toggle Key:");
                    DrawKeyBindButton("Toggle Key", ref this.Settings.ToggleKey, "toggleKey");
                    ImGuiHelper.ToolTip("Click the button and press any key to bind it as toggle key");
                    
                    ImGui.EndTabItem();
                }

                // Tab 2: Targeting Settings
                if (ImGui.BeginTabItem("Targeting"))
                {
                    ImGui.SliderFloat("Targeting Range", ref this.Settings.TargetingRange, 10f, 200f);
                    ImGuiHelper.ToolTip("Maximum range for targeting monsters (in grid units)");

                    ImGui.SliderFloat("RayCast Range (Visual)", ref this.Settings.RayCastRange, 50f, 1000f);
                    ImGuiHelper.ToolTip("Range for walkable grid visualization and line-of-sight checks");
                    
                    ImGui.Checkbox("Enable Line of Sight", ref this.Settings.EnableLineOfSight);
                    ImGuiHelper.ToolTip("Prevents targeting monsters behind walls - HIGHLY RECOMMENDED");
                    
                    ImGui.Separator();
                    ImGui.Text("Monster Types to Target:");
                    ImGui.Checkbox("Target Normal (White)", ref this.Settings.TargetNormal);
                    ImGui.Checkbox("Target Magic (Blue)", ref this.Settings.TargetMagic);
                    ImGui.Checkbox("Target Rare (Yellow)", ref this.Settings.TargetRare);
                    ImGui.Checkbox("Target Unique (Orange)", ref this.Settings.TargetUnique);
                    
                    ImGui.EndTabItem();
                }

                // Tab 3: Auto-Skill Settings
                if (ImGui.BeginTabItem("Auto-Skill"))
                {
                    bool enableAutoSkill = this.Settings.EnableAutoSkill;
                    if (ImGui.Checkbox("Enable Auto-Skill", ref enableAutoSkill))
                    {
                        this.Settings.EnableAutoSkill = enableAutoSkill;
                    }
                    ImGuiHelper.ToolTip("Automatically use a skill when close to monsters");

                    if (this.Settings.EnableAutoSkill)
                    {
                        ImGui.Separator();
                        
                        // Skill Key Selection
                        ImGui.Text("Skill Key:");
                        DrawKeyBindButton("Auto-Skill Key", ref this.Settings.AutoSkillKey, "autoSkillKey");
                        ImGuiHelper.ToolTip("Click the button and press any key to bind it as auto-skill key");
                        
                        // Range and Timing
                        ImGui.SliderFloat("Auto-Skill Range", ref this.Settings.AutoSkillRange, 10f, 150f);
                        ImGuiHelper.ToolTip("Range within which to automatically use the skill");
                        
                        ImGui.SliderFloat("Skill Cooldown (seconds)", ref this.Settings.AutoSkillCooldown, 0.1f, 5.0f);
                        ImGuiHelper.ToolTip("Time between skill uses");
                        
                        // Key behavior
                        bool holdKey = this.Settings.AutoSkillHoldKey;
                        if (ImGui.Checkbox("Hold Key (vs Press/Release)", ref holdKey))
                        {
                            this.Settings.AutoSkillHoldKey = holdKey;
                        }
                        ImGuiHelper.ToolTip("Hold key down vs press and release");
                        
                        if (!this.Settings.AutoSkillHoldKey)
                        {
                            int holdDuration = this.Settings.AutoSkillKeyHoldDuration;
                            if (ImGui.SliderInt("Key Hold Duration (ms)", ref holdDuration, 50, 500))
                            {
                                this.Settings.AutoSkillKeyHoldDuration = holdDuration;
                            }
                            ImGuiHelper.ToolTip("How long to hold key when pressing");
                        }
                        
                        bool onlyInCombat = this.Settings.AutoSkillOnlyInCombat;
                        if (ImGui.Checkbox("Only During Combat", ref onlyInCombat))
                        {
                            this.Settings.AutoSkillOnlyInCombat = onlyInCombat;
                        }
                        ImGuiHelper.ToolTip("Only use skill when actively targeting monsters");
                        
                        ImGui.Separator();
                        bool showAutoSkillRange = this.Settings.ShowAutoSkillRange;
                        if (ImGui.Checkbox("Show Auto-Skill Range Circle", ref showAutoSkillRange))
                        {
                            this.Settings.ShowAutoSkillRange = showAutoSkillRange;
                        }
                        ImGuiHelper.ToolTip("Show visual circle indicating auto-skill range");
                        
                        ImGui.Separator();
                        ImGui.Text("Skill Combos by Rarity:");
                        
                        // Show warning if combos are active
                        bool hasCombosEnabled = skillCombos.Any(c => c.Enabled && c.Skills.Count > 0);
                        if (hasCombosEnabled)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Orange
                            ImGui.Text("⚠️ Combos are active - Single Skill Key above is ignored!");
                            ImGui.PopStyleColor();
                            ImGui.Separator();
                        }
                        
                        DrawSkillCombosInterface();
                    }
                    
                    ImGui.EndTabItem();
                }

                // Tab 4: Auto-Chest Settings
                if (ImGui.BeginTabItem("Auto-Chest"))
                {
                    bool enableAutoChest = this.Settings.EnableAutoChest;
                    if (ImGui.Checkbox("Enable Auto-Chest", ref enableAutoChest))
                    {
                        this.Settings.EnableAutoChest = enableAutoChest;
                    }
                    ImGuiHelper.ToolTip("Automatically detect and open nearby chests");

                    if (this.Settings.EnableAutoChest)
                    {
                        ImGui.Separator();
                        
                        // Chest Types
                        ImGui.Text("Chest Types to Open:");
                        bool openRegular = this.Settings.OpenRegularChests;
                        if (ImGui.Checkbox("Regular Chests", ref openRegular))
                        {
                            this.Settings.OpenRegularChests = openRegular;
                        }
                        ImGuiHelper.ToolTip("Open normal chests (safer)");
                        
                        bool openStrongboxes = this.Settings.OpenStrongboxes;
                        if (ImGui.Checkbox("Strongboxes", ref openStrongboxes))
                        {
                            this.Settings.OpenStrongboxes = openStrongboxes;
                        }
                        ImGuiHelper.ToolTip("Open strongboxes (more valuable but can spawn monsters)");
                        
                        // Range Settings
                        ImGui.Separator();
                        ImGui.SliderFloat("Chest Detection Range", ref this.Settings.AutoChestRange, 20f, 150f);
                        ImGuiHelper.ToolTip("Range to detect and move to chests");
                        
                        ImGui.SliderFloat("Chest Interaction Cooldown", ref this.Settings.ChestCooldown, 0.1f, 2.0f);
                        ImGuiHelper.ToolTip("Time between chest interactions (seconds)");
                        
                        // Safety Settings
                        ImGui.Separator();
                        ImGui.Text("Safety Settings:");
                        bool onlyWhenSafe = this.Settings.OnlyOpenWhenSafe;
                        if (ImGui.Checkbox("Only Open When Safe", ref onlyWhenSafe))
                        {
                            this.Settings.OnlyOpenWhenSafe = onlyWhenSafe;
                        }
                        ImGuiHelper.ToolTip("Only open chests when no monsters are nearby");
                        
                        if (this.Settings.OnlyOpenWhenSafe)
                        {
                            ImGui.SliderFloat("Safety Check Range", ref this.Settings.SafetyCheckRange, 30f, 200f);
                            ImGuiHelper.ToolTip("Range to check for monsters before opening chests");
                        }
                        
                        // Visual Settings
                        ImGui.Separator();
                        ImGui.Text("Visual Settings:");
                        bool showChestRange = this.Settings.ShowChestRange;
                        if (ImGui.Checkbox("Show Chest Range Circle", ref showChestRange))
                        {
                            this.Settings.ShowChestRange = showChestRange;
                        }
                        ImGuiHelper.ToolTip("Show visual circle for chest detection range");
                        
                        if (this.Settings.OnlyOpenWhenSafe)
                        {
                            bool showSafetyRange = this.Settings.ShowSafetyRange;
                            if (ImGui.Checkbox("Show Safety Range Circle", ref showSafetyRange))
                            {
                                this.Settings.ShowSafetyRange = showSafetyRange;
                            }
                            ImGuiHelper.ToolTip("Show visual circle for safety check range");
                        }
                    }
                    
                    ImGui.EndTabItem();
                }

                // Tab 5: Advanced Settings
                if (ImGui.BeginTabItem("Advanced"))
                {
                    // Movement/Speed Settings
                    ImGui.Text("Movement Mouse Settings:");
                    ImGui.SliderFloat("Mouse Speed", ref this.Settings.MouseSpeed, 0.1f, 5.0f);
                    ImGuiHelper.ToolTip("How fast the mouse moves to targets");

                    bool smoothMovement = this.Settings.SmoothMovement;
                    if (ImGui.Checkbox("Smooth Movement", ref smoothMovement))
                    {
                        this.Settings.SmoothMovement = smoothMovement;
                    }
                    ImGuiHelper.ToolTip("Smooth mouse movement vs direct movement");
                    
                    ImGui.Separator();
                    
                    // Targeting Advanced Settings
                    ImGui.Text("Targeting Settings:");
                    ImGui.SliderFloat("Target Switch Delay (ms)", ref this.Settings.TargetSwitchDelay, 0f, 1000f);
                    ImGuiHelper.ToolTip("Delay before switching to a new target");
                    
                    ImGui.Checkbox("Prefer Closest Target", ref this.Settings.PreferClosest);
                    ImGuiHelper.ToolTip("Always target the closest monster vs keeping current target");

                    ImGui.Separator();
                    ImGui.Text("Audio Settings:");
                    if (ImGui.RadioButton("Beep Off", this.Settings.BeepSound == false))
                        this.Settings.BeepSound = false;
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Beep On", this.Settings.BeepSound))
                        this.Settings.BeepSound = true;
                    
                    ImGui.EndTabItem();
                }

                // Tab 6: Debug Settings
                if (ImGui.BeginTabItem("Debug"))
                {
                    bool showRange = this.Settings.ShowRangeCircle;
                    if (ImGui.Checkbox("Show Range Circle", ref showRange))
                    {
                        this.Settings.ShowRangeCircle = showRange;
                    }
                    ImGuiHelper.ToolTip("Show visual circles indicating targeting and raycast ranges");
                
                    ImGui.Checkbox("Show Walkable Grid", ref this.Settings.ShowWalkableGrid);
                    ImGuiHelper.ToolTip("Show walkable grid values for debugging line-of-sight");
                
                    ImGui.Checkbox("Show Target Lines", ref this.Settings.ShowTargetLines);
                    ImGuiHelper.ToolTip("Show line from player to current target");
                
                    ImGui.Checkbox("Show Debug Window", ref this.Settings.ShowDebugWindow);
                    ImGuiHelper.ToolTip("Show debug information window");
                
                    ImGui.Checkbox("Show Line of Sight Visual", ref this.Settings.ShowLineOfSight);
                    ImGuiHelper.ToolTip("Show visual line-of-sight checks for debugging");
                
                    ImGui.Text($"Grid Debug Status: {(this.Settings.ShowWalkableGrid ? "ON" : "OFF")}");
                    
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        public override void SaveSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingPathname));
            var json = JsonConvert.SerializeObject(
                this.Settings,
                Newtonsoft.Json.Formatting.Indented
            );
            File.WriteAllText(SettingPathname, json);
        }


        public override void OnDisable()
        {
         
        }

        public override void OnEnable(bool isGameOpened)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingPathname));
            if (File.Exists(SettingPathname))
            {
                var json = File.ReadAllText(SettingPathname);
                this.Settings = JsonConvert.DeserializeObject<AutoAimSettings>(json);
            }
            else
            {
                this.Settings = new AutoAimSettings();
            }
            
            // Initialize combos after settings are loaded
            InitializeDefaultCombos();
        }



        public override void DrawUI()
        {
            HandleToggleKey();

            // Always try to draw visualization for debug purposes (even when paused/not foreground)
            DrawVisualizationForDebug();

            // Auto-aim functionality requires stricter conditions
            if (!this.Settings.IsEnabled)
                return;

            if (Core.States.GameCurrentState != GameStateTypes.InGameState)
                return;

            var currentAreaInstance = Core.States.InGameStateObject.CurrentAreaInstance;
            var currentWorldInstance = Core.States.InGameStateObject.CurrentWorldInstance;
            var player = currentAreaInstance.Player;
            
            if (!player.TryGetComponent<Render>(out var playerRender))
                return;

            if (!Core.Process.Foreground)
                 return;

            if (Core.States.InGameStateObject.GameUi.SkillTreeNodesUiElements.Count > 0)
                return;

            var playerPos = new Vector2(playerRender.GridPosition.X, playerRender.GridPosition.Y);

            if ((GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0)
            {
                
                this._debugInfo = "AutoAim paused (Left Alt held)";
                targetedMonster = null; 
                return;
            }

            // Auto-aim logic
            if (targetedMonster != null && !IsValidTarget(targetedMonster))
            {
                targetedMonster = null;
            }

            // Find best target
            var bestTarget = FindBestTarget(currentAreaInstance, playerPos);
            
            if (bestTarget != null)
            {
                targetedMonster = bestTarget;
                MoveMouseToTarget(targetedMonster);
                this._debugInfo2 = $"MoveMouse called for target at distance {(targetedMonster.TryGetComponent<Render>(out var r) ? Vector2.Distance(playerPos, new Vector2(r.GridPosition.X, r.GridPosition.Y)).ToString("F1") : "N/A")}";
            }
            else
            {
                this._debugInfo2 = "No bestTarget found";
            }

            // Handle auto-skill
            HandleAutoSkill(targetedMonster, playerPos);
            HandleSkillKeyRelease();

            // Handle auto-chest
            HandleAutoChest(currentAreaInstance, playerPos);
            

            if (this.Settings.ShowRangeCircle)
            {
                DrawRaycastRangeCircle(playerPos, currentAreaInstance, targetedMonster);
            }
            
            // Draw walkable grid if enabled
            if (this.Settings.ShowWalkableGrid)
            {
                DrawWalkableGrid(playerPos, currentAreaInstance);
            }
        }

        private int GetWalkableValue(GameHelper.RemoteObjects.States.InGameStateObjects.AreaInstance area, int x, int y)
        {
            var mapWalkableData = area.GridWalkableData;
            var bytesPerRow = area.TerrainMetadata.BytesPerRow;
            
            if (mapWalkableData.Length == 0 || bytesPerRow <= 0)
                return 0;

            var totalRows = mapWalkableData.Length / bytesPerRow;
            var width = bytesPerRow * 2; 

            if (x < 0 || y < 0 || x >= width || y >= totalRows)
                return 0;

            var index = (y * bytesPerRow) + (x / 2);
            if (index >= mapWalkableData.Length)
                return 0;

            var data = mapWalkableData[index];
            var shiftAmount = (x % 2 == 0) ? 0 : 4;
            
            return (data >> shiftAmount) & 0xF; 
        }


        private void HandleToggleKey()
        {
            bool isToggleKeyPressed = IsKeyCombinationPressed("toggleKey");
            
            if (isToggleKeyPressed && !wasToggleKeyPressed)
            {
                this.Settings.IsEnabled = !this.Settings.IsEnabled;
                

                if (!this.Settings.IsEnabled)
                {
                    targetedMonster = null; 
                }

                if (this.Settings.BeepSound)
                    MessageBeep(0xFFFFFFFF);
            }
            
            wasToggleKeyPressed = isToggleKeyPressed;
        }
        
        private bool IsKeyCombinationPressed(string keyId)
        {
            if (!keyCombinations.ContainsKey(keyId))
                return false;
                
            var combination = keyCombinations[keyId];
            
            // Check if main key is pressed
            bool mainKeyPressed = (GetAsyncKeyState(combination.MainKey) & 0x8000) != 0;
            if (!mainKeyPressed) return false;
            
            // Check modifier requirements
            bool ctrlPressed = (GetAsyncKeyState(17) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(16) & 0x8000) != 0;
            bool altPressed = (GetAsyncKeyState(18) & 0x8000) != 0;
            
            // Must match exactly what was configured
            return ctrlPressed == combination.UseCtrl &&
                   shiftPressed == combination.UseShift &&
                   altPressed == combination.UseAlt;
        }
        
        private void DrawSkillCombosInterface()
        {
            // Initialize default combos if empty
            if (skillCombos.Count == 0)
            {
                InitializeDefaultCombos();
            }
            
            ImGui.Text("Configure skill combinations for different monster rarities:");
            ImGui.Separator();
            
            for (int i = 0; i < skillCombos.Count; i++)
            {
                var combo = skillCombos[i];
                
                ImGui.PushID($"combo_{i}");
                
                // Combo header with rarity color
                var rarityColor = GetRarityColor(combo.TargetRarity);
                ImGui.PushStyleColor(ImGuiCol.Text, rarityColor);
                bool enabled = combo.Enabled;
                if (ImGui.Checkbox($"{combo.Name} ({combo.TargetRarity})", ref enabled))
                {
                    var updatedCombo = combo;
                    updatedCombo.Enabled = enabled;
                    skillCombos[i] = updatedCombo;
                    SaveCombosToSettings(); // Save when enabled/disabled
                }
                ImGui.PopStyleColor();
                
                if (combo.Enabled)
                {
                    ImGui.Indent(20);
                    
                    // Show current skills in combo
                    ImGui.Text($"Skills in combo: {combo.Skills.Count}");
                    
                    for (int j = 0; j < combo.Skills.Count; j++)
                    {
                        ImGui.PushID($"skill_{i}_{j}");
                        
                        // Skill name and delay editor
                        ImGui.Text($"{j + 1}. {GetCombinationName(combo.Skills[j])}");
                        
                        // Delay editor (inline)
                        ImGui.SameLine();
                        ImGui.Text("Delay:");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(60);
                        
                        var delay = combo.Delays[j];
                        if (ImGui.DragFloat($"##delay", ref delay, 0.1f, 0.0f, 10.0f, "%.1fs"))
                        {
                            var updatedCombo = combo;
                            updatedCombo.Delays[j] = Math.Max(0.0f, delay);
                            skillCombos[i] = updatedCombo;
                            SaveCombosToSettings(); // Save when delay changed
                        }
                        
                        // Remove button
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Remove"))
                        {
                            var updatedCombo = combo;
                            updatedCombo.Skills.RemoveAt(j);
                            updatedCombo.Delays.RemoveAt(j);
                            skillCombos[i] = updatedCombo;
                            SaveCombosToSettings(); // Save when skill removed
                            ImGui.PopID();
                            break;
                        }
                        
                        ImGui.PopID();
                    }
                    
                    // Add new skill to combo
                    if (ImGui.Button($"Add Skill to {combo.Name}##{i}"))
                    {
                        isCapturingKey[$"combo_{i}_new"] = true;
                    }
                    
                        // Handle new skill capture
                        if (isCapturingKey.ContainsKey($"combo_{i}_new") && isCapturingKey[$"combo_{i}_new"])
                        {
                            ImGui.SameLine();
                            ImGui.Text("Press key combination...");
                            
                            var capturedCombination = GetPressedKeyCombination();
                            if (capturedCombination.MainKey != 0)
                            {
                                var updatedCombo = combo;
                                updatedCombo.Skills.Add(capturedCombination);
                                updatedCombo.Delays.Add(0.5f); // Default 0.5s delay
                                skillCombos[i] = updatedCombo;
                                isCapturingKey[$"combo_{i}_new"] = false;
                                SaveCombosToSettings(); // Save when new skill added
                            }
                            
                            if ((GetAsyncKeyState(27) & 0x8000) != 0) // ESC to cancel
                            {
                                isCapturingKey[$"combo_{i}_new"] = false;
                            }
                        }                    ImGui.Unindent(20);
                }
                
                ImGui.PopID();
                ImGui.Separator();
            }
        }
        
        private void InitializeDefaultCombos()
        {
            // Load combos from settings first
            LoadCombosFromSettings();
            
            // If no combos loaded, create defaults
            if (skillCombos.Count == 0)
            {
                // Normal + Magic monsters - basic combo (most common monsters)
                var basicCombo = new SkillCombo("Normal + Magic Monsters", Rarity.Normal);
                skillCombos.Add(basicCombo);
                
                // Rare monsters - advanced combo
                var rareCombo = new SkillCombo("Rare Monsters", Rarity.Rare);
                skillCombos.Add(rareCombo);
                
                // Unique monsters - ultimate combo
                var uniqueCombo = new SkillCombo("Unique Monsters", Rarity.Unique);
                skillCombos.Add(uniqueCombo);
                
                // Save defaults
                SaveCombosToSettings();
            }
        }
        
        private System.Numerics.Vector4 GetRarityColor(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Normal => new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f), // White
                Rarity.Magic => new System.Numerics.Vector4(0.3f, 0.3f, 1.0f, 1.0f),   // Blue
                Rarity.Rare => new System.Numerics.Vector4(1.0f, 1.0f, 0.0f, 1.0f),    // Yellow
                Rarity.Unique => new System.Numerics.Vector4(1.0f, 0.5f, 0.0f, 1.0f),  // Orange
                _ => new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f)              // Gray
            };
        }
        
        private bool TryStartCombo(Entity target)
        {
            if (isExecutingCombo)
                return false; // Already executing a combo

            if (!target.TryGetComponent<ObjectMagicProperties>(out var magicProps))
                return false;
                
            var targetRarity = magicProps.Rarity;
            
            // Try to find combo with fallback priority:
            // Unique -> Rare -> Normal+Magic -> Default skill
            var fallbackOrder = new List<Rarity>();
            
            switch (targetRarity)
            {
                case Rarity.Unique:
                    fallbackOrder.AddRange(new[] { Rarity.Unique, Rarity.Rare, Rarity.Normal });
                    break;
                case Rarity.Rare:
                    fallbackOrder.AddRange(new[] { Rarity.Rare, Rarity.Normal });
                    break;
                case Rarity.Magic:
                case Rarity.Normal:
                    fallbackOrder.Add(Rarity.Normal); // Normal combo handles both Normal and Magic
                    break;
                default:
                    fallbackOrder.Add(Rarity.Normal); // Default fallback
                    break;
            }
            
            // Try each fallback option in order
            foreach (var fallbackRarity in fallbackOrder)
            {
                for (int i = 0; i < skillCombos.Count; i++)
                {
                    var combo = skillCombos[i];
                    if (!combo.Enabled || combo.Skills.Count == 0)
                        continue;
                        
                    if (combo.TargetRarity == fallbackRarity)
                    {
                        // Start combo execution
                        currentComboIndex = i;
                        currentSkillInCombo = 0;
                        isExecutingCombo = true;
                        currentComboTarget = target;
                        lastComboSkillTime = DateTime.MinValue; // Execute first skill immediately
                        return true;
                    }
                }
            }
            
            return false; // No combo found, will use default skill
        }        private void HandleComboExecution()
        {
            if (!isExecutingCombo || currentComboIndex < 0 || currentComboIndex >= skillCombos.Count)
                return;
                
            var combo = skillCombos[currentComboIndex];
            
            // Check if target is still valid
            if (currentComboTarget == null || !IsValidTarget(currentComboTarget))
            {
                StopCombo();
                return;
            }
            
            // Check if we need to execute the next skill
            var timeSinceLastSkill = DateTime.Now - lastComboSkillTime;
            var requiredDelay = currentSkillInCombo > 0 ? combo.Delays[currentSkillInCombo - 1] : 0;
            
            if (timeSinceLastSkill.TotalSeconds >= requiredDelay)
            {
                if (currentSkillInCombo < combo.Skills.Count)
                {
                    // Execute current skill in combo
                    ExecuteSkillCombination(combo.Skills[currentSkillInCombo]);
                    lastComboSkillTime = DateTime.Now;
                    currentSkillInCombo++;
                }
                else
                {
                    // Combo finished
                    StopCombo();
                }
            }
        }
        
        private void ExecuteSkillCombination(KeyCombination combination)
        {
            var skillKey = (byte)combination.MainKey;
            
            // Press modifiers first
            if (combination.UseCtrl) keybd_event(17, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            if (combination.UseShift) keybd_event(16, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            if (combination.UseAlt) keybd_event(18, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            
            // Press main key
            keybd_event(skillKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            
            // Small delay for key registration
            System.Threading.Thread.Sleep(50);
            
            // Release main key first
            keybd_event(skillKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            
            // Then release modifiers
            if (combination.UseAlt) keybd_event(18, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (combination.UseShift) keybd_event(16, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (combination.UseCtrl) keybd_event(17, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        private void StopCombo()
        {
            isExecutingCombo = false;
            currentComboIndex = -1;
            currentSkillInCombo = 0;
            currentComboTarget = null;
            lastComboSkillTime = DateTime.MinValue;
        }
        
        private void LoadCombosFromSettings()
        {
            skillCombos.Clear();
            
            foreach (var savedCombo in this.Settings.SkillCombos)
            {
                var combo = new SkillCombo(savedCombo.Name, (Rarity)savedCombo.TargetRarity);
                combo.Enabled = savedCombo.Enabled;
                
                for (int i = 0; i < savedCombo.Skills.Count; i++)
                {
                    var savedSkill = savedCombo.Skills[i];
                    var skillCombo = new KeyCombination(savedSkill.MainKey, savedSkill.UseCtrl, savedSkill.UseShift, savedSkill.UseAlt);
                    combo.Skills.Add(skillCombo);
                    
                    if (i < savedCombo.Delays.Count)
                        combo.Delays.Add(savedCombo.Delays[i]);
                    else
                        combo.Delays.Add(0.5f); // Default delay
                }
                
                skillCombos.Add(combo);
            }
            
            // Load key combinations
            if (this.Settings.ToggleKeyCombination != null)
            {
                var toggleCombo = new KeyCombination(
                    this.Settings.ToggleKeyCombination.MainKey,
                    this.Settings.ToggleKeyCombination.UseCtrl,
                    this.Settings.ToggleKeyCombination.UseShift,
                    this.Settings.ToggleKeyCombination.UseAlt
                );
                keyCombinations["toggleKey"] = toggleCombo;
                this.Settings.ToggleKey = toggleCombo.MainKey; // For compatibility
            }
            
            if (this.Settings.AutoSkillKeyCombination != null)
            {
                var autoSkillCombo = new KeyCombination(
                    this.Settings.AutoSkillKeyCombination.MainKey,
                    this.Settings.AutoSkillKeyCombination.UseCtrl,
                    this.Settings.AutoSkillKeyCombination.UseShift,
                    this.Settings.AutoSkillKeyCombination.UseAlt
                );
                keyCombinations["autoSkillKey"] = autoSkillCombo;
                this.Settings.AutoSkillKey = autoSkillCombo.MainKey; // For compatibility
            }
        }
        
        private void SaveCombosToSettings()
        {
            this.Settings.SkillCombos.Clear();
            
            foreach (var combo in skillCombos)
            {
                var savedCombo = new SerializableSkillCombo(combo.Name, (int)combo.TargetRarity);
                savedCombo.Enabled = combo.Enabled;
                
                for (int i = 0; i < combo.Skills.Count; i++)
                {
                    var skill = combo.Skills[i];
                    var savedSkill = new SerializableKeyCombination(skill.MainKey, skill.UseCtrl, skill.UseShift, skill.UseAlt);
                    savedCombo.Skills.Add(savedSkill);
                    
                    if (i < combo.Delays.Count)
                        savedCombo.Delays.Add(combo.Delays[i]);
                    else
                        savedCombo.Delays.Add(0.5f);
                }
                
                this.Settings.SkillCombos.Add(savedCombo);
            }
            
            // Save key combinations
            if (keyCombinations.ContainsKey("toggleKey"))
            {
                var toggleCombo = keyCombinations["toggleKey"];
                this.Settings.ToggleKeyCombination = new SerializableKeyCombination(
                    toggleCombo.MainKey, toggleCombo.UseCtrl, toggleCombo.UseShift, toggleCombo.UseAlt
                );
            }
            
            if (keyCombinations.ContainsKey("autoSkillKey"))
            {
                var autoSkillCombo = keyCombinations["autoSkillKey"];
                this.Settings.AutoSkillKeyCombination = new SerializableKeyCombination(
                    autoSkillCombo.MainKey, autoSkillCombo.UseCtrl, autoSkillCombo.UseShift, autoSkillCombo.UseAlt
                );
            }
            
            // Trigger save
            SaveSettings();
        }

        private void HandleAutoSkill(Entity currentTarget, Vector2 playerPos)
        {
            if (!this.Settings.EnableAutoSkill)
                return;

            // Handle combo execution
            HandleComboExecution();

            // Check if we should only use skill during combat
            if (this.Settings.AutoSkillOnlyInCombat && currentTarget == null)
                return;

            // Check if target is in range
            bool targetInRange = false;
            if (currentTarget != null && currentTarget.TryGetComponent<Render>(out var render))
            {
                var targetPos = new Vector2(render.GridPosition.X, render.GridPosition.Y);
                var distance = Vector2.Distance(playerPos, targetPos);
                
                if (distance <= this.Settings.AutoSkillRange)
                {
                    targetInRange = true;
                }
            }
            else if (!this.Settings.AutoSkillOnlyInCombat)
            {
                targetInRange = true; // Can use without target if not combat-only
            }

            if (targetInRange)
            {
                // Check if any combos are configured and enabled
                bool hasCombosEnabled = skillCombos.Any(c => c.Enabled && c.Skills.Count > 0);
                
                if (hasCombosEnabled && currentTarget != null)
                {
                    // Try to start combo with intelligent fallback
                    if (!TryStartCombo(currentTarget))
                    {
                        // No combo available even with fallback, use default skill as last resort
                        var timeSinceLastUse = DateTime.Now - lastSkillUse;
                        if (timeSinceLastUse.TotalSeconds >= this.Settings.AutoSkillCooldown)
                        {
                            UseSkill();
                            lastSkillUse = DateTime.Now;
                        }
                    }
                }
                else if (!hasCombosEnabled)
                {
                    // No combos configured, use single skill
                    var timeSinceLastUse = DateTime.Now - lastSkillUse;
                    if (timeSinceLastUse.TotalSeconds >= this.Settings.AutoSkillCooldown)
                    {
                        UseSkill();
                        lastSkillUse = DateTime.Now;
                    }
                }
                // If no target but combos enabled, don't do anything (combos require targets)
            }
        }

        private void UseSkill()
        {
            if (!keyCombinations.ContainsKey("autoSkillKey"))
                return;
                
            var combination = keyCombinations["autoSkillKey"];
            var skillKey = (byte)combination.MainKey;

            if (this.Settings.AutoSkillHoldKey)
            {
                // Hold key behavior - press and keep held until we're out of range
                if (!isSkillKeyHeld)
                {
                    // Press modifiers first
                    if (combination.UseCtrl) keybd_event(17, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    if (combination.UseShift) keybd_event(16, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    if (combination.UseAlt) keybd_event(18, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    
                    keybd_event(skillKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    isSkillKeyHeld = true;
                    skillKeyPressTime = DateTime.Now;
                }
            }
            else
            {
                // Press and release behavior
                // Press modifiers first
                if (combination.UseCtrl) keybd_event(17, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                if (combination.UseShift) keybd_event(16, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                if (combination.UseAlt) keybd_event(18, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                
                keybd_event(skillKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                skillKeyPressTime = DateTime.Now;
            }
        }

        private void HandleSkillKeyRelease()
        {
            if (!this.Settings.EnableAutoSkill || !keyCombinations.ContainsKey("autoSkillKey"))
                return;
                
            var combination = keyCombinations["autoSkillKey"];

            // Handle key release for press/release mode
            if (!this.Settings.AutoSkillHoldKey && skillKeyPressTime != DateTime.MinValue)
            {
                var timeSincePress = DateTime.Now - skillKeyPressTime;
                if (timeSincePress.TotalMilliseconds >= this.Settings.AutoSkillKeyHoldDuration)
                {
                    // Release main key first
                    keybd_event((byte)combination.MainKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    
                    // Then release modifiers
                    if (combination.UseAlt) keybd_event(18, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (combination.UseShift) keybd_event(16, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (combination.UseCtrl) keybd_event(17, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    
                    skillKeyPressTime = DateTime.MinValue;
                }
            }

            // Handle key release for hold mode when no target or out of range
            if (this.Settings.AutoSkillHoldKey && isSkillKeyHeld)
            {
                bool shouldReleaseKey = true;
                
                if (targetedMonster != null && targetedMonster.TryGetComponent<Render>(out var render))
                {
                    var currentAreaInstance = Core.States.InGameStateObject.CurrentAreaInstance;
                    var player = currentAreaInstance.Player;
                    
                    if (player.TryGetComponent<Render>(out var playerRender))
                    {
                        var playerPos = new Vector2(playerRender.GridPosition.X, playerRender.GridPosition.Y);
                        var targetPos = new Vector2(render.GridPosition.X, render.GridPosition.Y);
                        var distance = Vector2.Distance(playerPos, targetPos);
                        
                        if (distance <= this.Settings.AutoSkillRange)
                        {
                            shouldReleaseKey = false;
                        }
                    }
                }

                if (shouldReleaseKey)
                {
                    // Release main key first
                    keybd_event((byte)combination.MainKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    
                    // Then release modifiers
                    if (combination.UseAlt) keybd_event(18, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (combination.UseShift) keybd_event(16, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (combination.UseCtrl) keybd_event(17, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    
                    isSkillKeyHeld = false;
                }
            }
        }

        private void HandleAutoChest(AreaInstance currentArea, Vector2 playerPos)
        {
            if (!this.Settings.EnableAutoChest)
                return;

            // Don't open chests while actively targeting monsters (in combat)
            if (targetedMonster != null)
            {
                // Clear any chest target when in combat
                if (targetedChest != null)
                    targetedChest = null;
                return;
            }

            // Check cooldown
            var timeSinceLastChest = DateTime.Now - lastChestInteraction;
            if (timeSinceLastChest.TotalSeconds < this.Settings.ChestCooldown)
                return;

            // Check if current targeted chest is still valid
            if (targetedChest != null && !IsValidChest(targetedChest))
            {
                targetedChest = null;
            }

            // Find best chest if we don't have one
            if (targetedChest == null)
            {
                targetedChest = FindBestChest(currentArea, playerPos);
            }

            // Open chest if we have a valid target
            if (targetedChest != null)
            {
                OpenChest(targetedChest, playerPos);
            }
        }

        private Entity FindBestChest(AreaInstance currentArea, Vector2 playerPos)
        {
            Entity bestChest = null;
            float closestDistance = float.MaxValue;

            foreach (var entity in currentArea.AwakeEntities.Values)
            {
                if (!IsValidChest(entity))
                    continue;

                if (!entity.TryGetComponent<Render>(out var render))
                    continue;

                var chestPos = new Vector2(render.GridPosition.X, render.GridPosition.Y);
                var distance = Vector2.Distance(playerPos, chestPos);

                // Check if chest is in range
                if (distance > this.Settings.AutoChestRange)
                    continue;

                // Check safety if enabled
                if (this.Settings.OnlyOpenWhenSafe && !IsAreaSafe(currentArea, chestPos))
                    continue;

                // Find closest chest
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestChest = entity;
                }
            }

            return bestChest;
        }

        private bool IsValidChest(Entity entity)
        {
            if (entity == null || !entity.IsValid)
                return false;

            // Must be a chest
            if (entity.EntityType != EntityTypes.Chest)
                return false;

            // Check if we have the Chest component
            if (!entity.TryGetComponent<Chest>(out var chest))
                return false;

            // For chests, use the same logic as monsters - access cache directly
            if (entity.TryGetComponent<Targetable>(out var targetable))
            {
                // Access the cache field to get the raw data from game memory
                var targetableType = typeof(Targetable);
                var cacheField = targetableType.GetField("cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (cacheField != null)
                {
                    var cache = cacheField.GetValue(targetable);
                    var cacheType = cache.GetType();
                    
                    // Get IsTargetable from cache (closed chests are targetable, opened are not)
                    var isTargetableField = cacheType.GetField("IsTargetable");
                    var hiddenFromPlayerField = cacheType.GetField("HiddenfromPlayer");
                    
                    if (isTargetableField != null && hiddenFromPlayerField != null)
                    {
                        bool isTargetable = (bool)isTargetableField.GetValue(cache);
                        bool hiddenFromPlayer = (bool)hiddenFromPlayerField.GetValue(cache);
                        
                        // For chests: must be targetable (closed) and not hidden
                        if (!isTargetable || hiddenFromPlayer)
                            return false;
                    }
                }
                else
                {
                    // Fallback to restrictive check if reflection fails
                    if (!targetable.IsTargetable)
                        return false;
                }
            }
            else
            {
                // If no Targetable component, skip this chest
                return false;
            }

            // Check chest type preferences
            bool isStrongbox = chest.IsStrongbox;
            
            if (isStrongbox && !this.Settings.OpenStrongboxes)
                return false;
                
            if (!isStrongbox && !this.Settings.OpenRegularChests)
                return false;

            return true;
        }

        private bool IsAreaSafe(AreaInstance currentArea, Vector2 chestPos)
        {
            foreach (var entity in currentArea.AwakeEntities.Values)
            {
                if (!entity.IsValid || 
                    entity.EntityType != EntityTypes.Monster ||
                    entity.EntityState == EntityStates.MonsterFriendly)
                    continue;

                // Check if monster is alive
                if (entity.TryGetComponent<Life>(out var life) && !life.IsAlive)
                    continue;

                if (entity.TryGetComponent<Render>(out var render))
                {
                    var monsterPos = new Vector2(render.GridPosition.X, render.GridPosition.Y);
                    var distance = Vector2.Distance(chestPos, monsterPos);
                    
                    if (distance <= this.Settings.SafetyCheckRange)
                        return false; // Monster too close to chest
                }
            }

            return true; // Area is safe
        }

        private void OpenChest(Entity chest, Vector2 playerPos)
        {
            if (!chest.TryGetComponent<Render>(out var render))
                return;

            var chestPos = new Vector2(render.GridPosition.X, render.GridPosition.Y);
            var distance = Vector2.Distance(playerPos, chestPos);

            // Move mouse to chest and click
            MoveMouseToChest(chest);
            
            // Click the chest
            ClickLeftMouse();
            
            lastChestInteraction = DateTime.Now;
            targetedChest = null; // Reset target after interaction
        }

        private void MoveMouseToChest(Entity chest)
        {
            try
            {
                if (chest == null || !chest.TryGetComponent<Render>(out var render))
                    return;

                var gameState = Core.States.InGameStateObject;
                var chestWorldPos = render.WorldPosition;
                var chestScreenPos = gameState.CurrentWorldInstance.WorldToScreen(chestWorldPos, chestWorldPos.Z);
                
                var gameWindowRect = Core.Process.WindowArea;
                var targetScreenX = (int)(gameWindowRect.X + chestScreenPos.X);
                var targetScreenY = (int)(gameWindowRect.Y + chestScreenPos.Y);
                
                SetCursorPos(targetScreenX, targetScreenY);
            }
            catch
            {
                // Handle errors silently
            }
        }

        private void ClickLeftMouse()
        {
            // Import mouse click functions
            const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
            const uint MOUSEEVENTF_LEFTUP = 0x0004;
            
            // Simulate left mouse button press and release
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(50); // Small delay
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);


        private Entity FindBestTarget(AreaInstance currentArea, Vector2 playerPos)
        {
            Entity bestTarget = null;
            float bestScore = float.MaxValue;
            int totalMonsters = 0;
            int inRangeMonsters = 0;
            int validMonsters = 0;
            int afterLineOfSight = 0;
            int afterRarity = 0;
            
            var debugDistances = new List<float>();

            foreach (var entity in currentArea.AwakeEntities.Values)
            {
                if (!entity.IsValid || 
                    entity.EntityState == EntityStates.Useless ||
                    entity.EntityType != EntityTypes.Monster ||
                    entity.EntityState == EntityStates.MonsterFriendly)
                {
                    continue;
                }
                
                totalMonsters++;

                if (!IsValidTarget(entity))
                    continue;

                validMonsters++;

                if (!entity.TryGetComponent<Render>(out var render))
                    continue;

                var monsterPos = new Vector2(render.GridPosition.X, render.GridPosition.Y);
                var distance = Vector2.Distance(playerPos, monsterPos);
                
                debugDistances.Add(distance);

                if (distance > this.Settings.TargetingRange)
                {
                    continue;
                }
                    
                inRangeMonsters++;

                if (this.Settings.EnableLineOfSight && !RayCaster.IsMonsterTargetable(currentArea, playerPos, monsterPos, true))
                    continue;
                
                afterLineOfSight++;

                if (!IsRarityAllowed(entity))
                    continue;
                    
                afterRarity++;

               
                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestTarget = entity;
                }
            }

            var maxDist = debugDistances.Any() ? debugDistances.Max() : 0f;
            var minDist = debugDistances.Any() ? debugDistances.Min() : 0f;
            var inRangeCount = debugDistances.Count(d => d <= this.Settings.TargetingRange);
            var outsideRangeCount = debugDistances.Count(d => d > this.Settings.TargetingRange);
            
            this._debugInfo = $"Total: {totalMonsters}, Valid: {validMonsters}, InRange: {inRangeCount}/{inRangeMonsters}, OutRange: {outsideRangeCount}, AfterLOS: {afterLineOfSight}, AfterRarity: {afterRarity}, Target: {(bestTarget != null ? "YES" : "NO")}, TargetRange: {this.Settings.TargetingRange:F0}, LOS_Enabled: {this.Settings.EnableLineOfSight}, Blocked: {inRangeMonsters - afterLineOfSight}, Closest: {(debugDistances.Any() ? minDist.ToString("F1") : "N/A")}, Farthest: {(debugDistances.Any() ? maxDist.ToString("F1") : "N/A")}";

            return bestTarget;
        }


        private bool IsValidTarget(Entity entity)
        {
            if (entity == null || !entity.IsValid)
                return false;

            // Basic monster validation
            if (entity.EntityType != EntityTypes.Monster)
                return false;

            // Smart Targetable checking - instead of using the restrictive IsTargetable property,
            // we check individual conditions to allow more flexibility
            if (entity.TryGetComponent<Targetable>(out var targetable))
            {
                // First check if the entity even has the basic targetable flag from game memory
                // We need to access the cache field to get the raw data
                var targetableType = typeof(Targetable);
                var cacheField = targetableType.GetField("cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (cacheField != null)
                {
                    var cache = cacheField.GetValue(targetable);
                    var cacheType = cache.GetType();
                    
                    // Get the basic IsTargetable from game memory (not the processed property)
                    var isTargetableField = cacheType.GetField("IsTargetable");
                    var hiddenFromPlayerField = cacheType.GetField("HiddenfromPlayer");
                    
                    if (isTargetableField != null && hiddenFromPlayerField != null)
                    {
                        bool basicTargetable = (bool)isTargetableField.GetValue(cache);
                        bool hiddenFromPlayer = (bool)hiddenFromPlayerField.GetValue(cache);
                        
                        // Only exclude if fundamentally not targetable or hidden
                        if (!basicTargetable || hiddenFromPlayer)
                            return false;
                    }
                }
                else
                {
                    // Fallback to the restrictive check if we can't access cache
                    if (!targetable.IsTargetable)
                        return false;
                }
            }

            // Check for hidden monsters buff
            if (entity.TryGetComponent<Buffs>(out var buffs))
            {
                if (buffs.StatusEffects.ContainsKey("hidden_monster_6B"))
                    return false;
            }
           
            // Check if monster is alive
            if (entity.TryGetComponent<Life>(out var life))
            {
                if (!life.IsAlive)
                    return false;
            }

            // Check if monster is inside monolith (untargetable)
            if (entity.TryGetComponent<Stats>(out var stats))
            {
                // Check for monster_inside_monolith stat in StatsChangedByItems
                if (stats.StatsChangedByItems.TryGetValue(GameHelper.RemoteEnums.GameStats.monster_inside_monolith, out var monolithStat))
                {
                    if (monolithStat > 0) // If monster_inside_monolith = 1, skip it
                        return false;
                }
            }

            return true;
        }


        private bool IsRarityAllowed(Entity entity)
        {
            if (!entity.TryGetComponent<ObjectMagicProperties>(out var magicProps))
                return this.Settings.TargetNormal; 

            var rarity = magicProps.Rarity;

            return rarity switch
            {
                Rarity.Normal => this.Settings.TargetNormal,
                Rarity.Magic => this.Settings.TargetMagic,
                Rarity.Rare => this.Settings.TargetRare,
                Rarity.Unique => this.Settings.TargetUnique,
                _ => false
            };
        }


        private void MoveMouseToTarget(Entity target)
        {
            try
            {
                if (target == null || !target.TryGetComponent<Render>(out var render))
                {
                    this._debugInfo2 = "Target null or no render component";
                    return;
                }

                var gameState = Core.States.InGameStateObject;
                var currentArea = gameState.CurrentAreaInstance;
                var player = currentArea.Player;
                
                if (!player.TryGetComponent<Render>(out var playerRender))
                {
                    this._debugInfo2 = "Player has no render component";
                    return;
                }

                var targetWorldPos = render.WorldPosition;
                var targetGridPos = render.GridPosition;
                
                this._debugInfo2 = $"World: {targetWorldPos.X:F1},{targetWorldPos.Y:F1} Grid: {targetGridPos.X:F1},{targetGridPos.Y:F1}";

                var targetScreenPos = gameState.CurrentWorldInstance.WorldToScreen(targetWorldPos, targetWorldPos.Z);
                
                var gameWindowRect = Core.Process.WindowArea;
                var targetScreenX = (int)(gameWindowRect.X + targetScreenPos.X);
                var targetScreenY = (int)(gameWindowRect.Y + targetScreenPos.Y);
                
                this._debugInfo2 += $" ScreenPos: {targetScreenPos.X:F0},{targetScreenPos.Y:F0}";
                
                if (targetScreenPos.X < 0 || targetScreenPos.Y < 0 || 
                    targetScreenPos.X > gameWindowRect.Width || targetScreenPos.Y > gameWindowRect.Height)
                {
                    this._debugInfo2 += " (OUT OF BOUNDS)";
                    return;
                }


                GetCursorPos(out POINT currentMouse);
                
                SetCursorPos(targetScreenX, targetScreenY);
                this._debugInfo2 += $" Final: {targetScreenX},{targetScreenY} (was {currentMouse.X},{currentMouse.Y}) MOVED";
            }
            catch (Exception ex)
            {
                this._debugInfo2 = $"ERRO: {ex.Message}";
            }
        }

        private void DrawVisualizationForDebug()
        {
            try
            {
                // Check if we're in game and have basic game state
                if (Core.States.GameCurrentState != GameStateTypes.InGameState)
                    return;
                    
                var currentAreaInstance = Core.States.InGameStateObject?.CurrentAreaInstance;
                if (currentAreaInstance == null)
                    return;
                    
                var player = currentAreaInstance.Player;
                if (!player.TryGetComponent<Render>(out var playerRender))
                    return;

                var playerPos = new Vector2(playerRender.GridPosition.X, playerRender.GridPosition.Y);
                
                // Always draw range circles for debug (no foreground or enabled restrictions)
                DrawRaycastRangeCircle(playerPos, currentAreaInstance, targetedMonster);

                // Draw walkable grid if enabled
                if (this.Settings.ShowWalkableGrid)
                {
                    DrawWalkableGrid(playerPos, currentAreaInstance);
                }

                // Draw debug window if enabled (always works, even when game is paused)
                if (this.Settings.ShowDebugWindow)
                {
                    DrawDebugWindow(currentAreaInstance, player, playerPos);
                }
            }
            catch
            {
                // Silently handle any errors to avoid crashing debug visualization
            }
        }

        private void DrawKeyBindButton(string label, ref int currentKey, string keyId)
        {
            // Initialize if not exists
            if (!isCapturingKey.ContainsKey(keyId))
            {
                isCapturingKey[keyId] = false;
                // Initialize with simple key combination
                if (!keyCombinations.ContainsKey(keyId))
                {
                    keyCombinations[keyId] = new KeyCombination(currentKey);
                }
                keyBindingLabels[keyId] = GetCombinationName(keyCombinations[keyId]);
            }

            // Update label if key changed externally
            if (!isCapturingKey[keyId])
            {
                keyBindingLabels[keyId] = GetCombinationName(keyCombinations[keyId]);
            }
            
            var buttonText = isCapturingKey[keyId] ? "Press key combination..." : $"{keyBindingLabels[keyId]}";
            var buttonColor = isCapturingKey[keyId] ? 
                new System.Numerics.Vector4(1.0f, 0.5f, 0.0f, 1.0f) : // Orange when capturing
                new System.Numerics.Vector4(0.2f, 0.6f, 1.0f, 1.0f);   // Blue when not capturing
            
            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor * 1.2f);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonColor * 0.8f);
            
            if (ImGui.Button($"{buttonText}##{keyId}", new System.Numerics.Vector2(150, 25)))
            {
                isCapturingKey[keyId] = true;
            }
            
            ImGui.PopStyleColor(3);
            
            // Capture key input when in capture mode
            if (isCapturingKey[keyId])
            {
                var capturedCombination = GetPressedKeyCombination();
                if (capturedCombination.MainKey != 0)
                {
                    keyCombinations[keyId] = capturedCombination;
                    currentKey = capturedCombination.MainKey; // For backward compatibility
                    keyBindingLabels[keyId] = GetCombinationName(capturedCombination);
                    isCapturingKey[keyId] = false;
                    SaveCombosToSettings(); // Save when key combination changed
                }
                
                // Cancel capture with Escape
                if ((GetAsyncKeyState(27) & 0x8000) != 0) // ESC key
                {
                    isCapturingKey[keyId] = false;
                }
            }
        }
        
        private string GetCombinationName(KeyCombination combination)
        {
            var parts = new List<string>();
            
            if (combination.UseCtrl) parts.Add("Ctrl");
            if (combination.UseShift) parts.Add("Shift");
            if (combination.UseAlt) parts.Add("Alt");
            
            parts.Add(GetKeyName(combination.MainKey));
            
            return string.Join(" + ", parts);
        }
        
        private string GetKeyName(int vkCode)
        {
            return vkCode switch
            {
                // Function keys
                112 => "F1", 113 => "F2", 114 => "F3", 115 => "F4",
                116 => "F5", 117 => "F6", 118 => "F7", 119 => "F8",
                120 => "F9", 121 => "F10", 122 => "F11", 123 => "F12",
                
                // Letters
                65 => "A", 66 => "B", 67 => "C", 68 => "D", 69 => "E", 70 => "F",
                71 => "G", 72 => "H", 73 => "I", 74 => "J", 75 => "K", 76 => "L",
                77 => "M", 78 => "N", 79 => "O", 80 => "P", 81 => "Q", 82 => "R",
                83 => "S", 84 => "T", 85 => "U", 86 => "V", 87 => "W", 88 => "X",
                89 => "Y", 90 => "Z",
                
                // Numbers
                48 => "0", 49 => "1", 50 => "2", 51 => "3", 52 => "4",
                53 => "5", 54 => "6", 55 => "7", 56 => "8", 57 => "9",
                
                // Special keys
                32 => "Space", 13 => "Enter", 9 => "Tab", 8 => "Backspace",
                16 => "Shift", 17 => "Ctrl", 18 => "Alt", 20 => "CapsLock",
                
                // Mouse buttons
                1 => "LMouse", 2 => "RMouse", 4 => "MMouse",
                5 => "Mouse4", 6 => "Mouse5",
                
                // Arrow keys
                37 => "Left", 38 => "Up", 39 => "Right", 40 => "Down",
                
                // Numpad
                96 => "Num0", 97 => "Num1", 98 => "Num2", 99 => "Num3", 100 => "Num4",
                101 => "Num5", 102 => "Num6", 103 => "Num7", 104 => "Num8", 105 => "Num9",
                
                _ => $"Key{vkCode}"
            };
        }
        
        private KeyCombination GetPressedKeyCombination()
        {
            // Check modifier states
            bool ctrlPressed = (GetAsyncKeyState(17) & 0x8000) != 0;  // VK_CONTROL
            bool shiftPressed = (GetAsyncKeyState(16) & 0x8000) != 0; // VK_SHIFT  
            bool altPressed = (GetAsyncKeyState(18) & 0x8000) != 0;   // VK_MENU (Alt)
            
            // Check all possible keys
            for (int vk = 1; vk <= 254; vk++)
            {
                // Skip modifier keys themselves and special keys
                if (vk == 16 || vk == 17 || vk == 18) continue; // Shift, Ctrl, Alt
                if (vk == 27) continue; // ESC (used for cancel)
                if (vk == 91 || vk == 92) continue; // Windows keys
                if (vk == 93) continue; // Menu key
                if (vk == 160 || vk == 161) continue; // Left/Right Shift
                if (vk == 162 || vk == 163) continue; // Left/Right Ctrl
                if (vk == 164 || vk == 165) continue; // Left/Right Alt
                
                if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                {
                    // Wait for key release to avoid multiple captures
                    while ((GetAsyncKeyState(vk) & 0x8000) != 0)
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                    
                    return new KeyCombination(vk, ctrlPressed, shiftPressed, altPressed);
                }
            }
            
            return new KeyCombination(0);
        }

        private void DrawDebugWindow(AreaInstance currentAreaInstance, Entity player, Vector2 playerPos)
        {
            try
            {
                ImGui.Begin("AutoAim Debug", ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Text($"Auto Aim: {(this.Settings.IsEnabled ? "ENABLED" : "DISABLED")}");
                ImGui.Text($"Toggle Key: F{this.Settings.ToggleKey - 111}");
                ImGui.Text($"Range: {this.Settings.RayCastRange:F1}");
                ImGui.Text($"Current Target: {(targetedMonster != null ? "Yes" : "No")}");
                ImGui.Text($"Target Info: {this._debugInfo}");
                
                if (!string.IsNullOrEmpty(_debugInfo2))
                {
                    ImGui.Separator();
                    ImGui.Text("Mouse Movement:");
                    ImGui.TextWrapped(_debugInfo2);
                }

                var nearbyMonsters = 0;
                var totalEntities = currentAreaInstance.AwakeEntities.Count;
                var monstersFound = 0;
                
                if (ImGui.IsWindowFocused())
                {
                    foreach (var entity in currentAreaInstance.AwakeEntities.Values)
                    {
                        if (!entity.IsValid || entity.Id == player.Id || 
                            entity.EntityType != EntityTypes.Monster ||
                            entity.EntityState == EntityStates.MonsterFriendly) 
                            continue;
                            
                        monstersFound++;
                        
                        if (entity.TryGetComponent<Render>(out var render))
                        {
                            var distance = Vector2.Distance(playerPos, new Vector2(render.GridPosition.X, render.GridPosition.Y));
                            if (distance <= this.Settings.RayCastRange)
                                nearbyMonsters++;
                        }
                    }
                }
                ImGui.Text($"Nearby Monsters: {nearbyMonsters}");
                ImGui.Text($"Total Entities: {totalEntities}");
                ImGui.Text($"Monsters Found: {monstersFound}");
                
                // Auto-Skill Debug Info
                if (this.Settings.EnableAutoSkill)
                {
                    ImGui.Separator();
                    ImGui.Text("Auto-Skill:");
                    ImGui.Text($"Enabled: {this.Settings.EnableAutoSkill}");
                    if (keyCombinations.ContainsKey("autoSkillKey"))
                    {
                        ImGui.Text($"Key: {GetCombinationName(keyCombinations["autoSkillKey"])}");
                    }
                    ImGui.Text($"Range: {this.Settings.AutoSkillRange:F1}");
                    ImGui.Text($"Cooldown: {this.Settings.AutoSkillCooldown:F1}s");
                    ImGui.Text($"Mode: {(this.Settings.AutoSkillHoldKey ? "Hold" : "Press/Release")}");
                    ImGui.Text($"Key Held: {isSkillKeyHeld}");
                    
                    // Combo system debug
                    ImGui.Text($"Combos Configured: {skillCombos.Count}");
                    bool hasCombosEnabled = skillCombos.Any(c => c.Enabled && c.Skills.Count > 0);
                    ImGui.Text($"Combos Override Single Key: {hasCombosEnabled}");
                    ImGui.Text($"Executing Combo: {isExecutingCombo}");
                    if (isExecutingCombo)
                    {
                        var combo = skillCombos[currentComboIndex];
                        ImGui.Text($"Current Combo: {combo.Name}");
                        ImGui.Text($"Skill: {currentSkillInCombo + 1}/{combo.Skills.Count}");
                        
                        if (currentComboTarget != null && currentComboTarget.TryGetComponent<ObjectMagicProperties>(out var comboMagicProps))
                        {
                            ImGui.Text($"Target Rarity: {comboMagicProps.Rarity}");
                            ImGui.Text($"Combo Rarity: {combo.TargetRarity}");
                            
                            // Show if using fallback
                            if (combo.TargetRarity != comboMagicProps.Rarity)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 0.8f, 0.3f, 1.0f)); // Orange
                                ImGui.Text($"Using Fallback: {comboMagicProps.Rarity} -> {combo.TargetRarity}");
                                ImGui.PopStyleColor();
                            }
                        }
                    }
                    
                    var timeSinceLastUse = DateTime.Now - lastSkillUse;
                    var cooldownRemaining = Math.Max(0, this.Settings.AutoSkillCooldown - timeSinceLastUse.TotalSeconds);
                    ImGui.Text($"Cooldown Remaining: {cooldownRemaining:F1}s");
                    
                    if (targetedMonster != null && targetedMonster.TryGetComponent<Render>(out var skillTargetRender))
                    {
                        var skillDistance = Vector2.Distance(playerPos, new Vector2(skillTargetRender.GridPosition.X, skillTargetRender.GridPosition.Y));
                        var inSkillRange = skillDistance <= this.Settings.AutoSkillRange;
                        ImGui.Text($"Target Distance: {skillDistance:F1} (In Range: {inSkillRange})");
                    }
                }
                
                // Auto-Chest Debug Info
                if (this.Settings.EnableAutoChest)
                {
                    ImGui.Separator();
                    ImGui.Text("Auto-Chest:");
                    ImGui.Text($"Enabled: {this.Settings.EnableAutoChest}");
                    ImGui.Text($"Regular Chests: {this.Settings.OpenRegularChests}");
                    ImGui.Text($"Strongboxes: {this.Settings.OpenStrongboxes}");
                    ImGui.Text($"Range: {this.Settings.AutoChestRange:F1}");
                    ImGui.Text($"Safety Check: {this.Settings.OnlyOpenWhenSafe}");
                    if (this.Settings.OnlyOpenWhenSafe)
                    {
                        ImGui.Text($"Safety Range: {this.Settings.SafetyCheckRange:F1}");
                    }
                    
                    var timeSinceLastChest = DateTime.Now - lastChestInteraction;
                    var chestCooldownRemaining = Math.Max(0, this.Settings.ChestCooldown - timeSinceLastChest.TotalSeconds);
                    ImGui.Text($"Chest Cooldown: {chestCooldownRemaining:F1}s");
                    
                    // Combat status affects chest opening
                    bool inCombat = targetedMonster != null;
                    ImGui.Text($"In Combat: {(inCombat ? "YES (Chest paused)" : "NO")}");
                    
                    ImGui.Text($"Current Target: {(targetedChest != null ? "YES" : "NO")}");
                    
                    if (targetedChest != null && targetedChest.TryGetComponent<Render>(out var chestRender))
                    {
                        var chestDistance = Vector2.Distance(playerPos, new Vector2(chestRender.GridPosition.X, chestRender.GridPosition.Y));
                        ImGui.Text($"Chest Distance: {chestDistance:F1}");
                        
                        if (targetedChest.TryGetComponent<Chest>(out var chestComponent))
                        {
                            ImGui.Text($"Is Strongbox: {chestComponent.IsStrongbox}");
                            ImGui.Text($"Is Opened: {chestComponent.IsOpened}");
                        }
                    }
                    
                    // Count nearby chests
                    int nearbyChests = 0;
                    int nearbyStrongboxes = 0;
                    foreach (var entity in currentAreaInstance.AwakeEntities.Values)
                    {
                        if (IsValidChest(entity) && entity.TryGetComponent<Render>(out var chestRender2))
                        {
                            var distance = Vector2.Distance(playerPos, new Vector2(chestRender2.GridPosition.X, chestRender2.GridPosition.Y));
                            if (distance <= this.Settings.AutoChestRange)
                            {
                                if (entity.TryGetComponent<Chest>(out var chest) && chest.IsStrongbox)
                                    nearbyStrongboxes++;
                                else
                                    nearbyChests++;
                            }
                        }
                    }
                    ImGui.Text($"Nearby: {nearbyChests} chests, {nearbyStrongboxes} strongboxes");
                }
                
                var debugDistances = new List<float>();
                foreach (var entity in currentAreaInstance.AwakeEntities.Values.Take(5))
                {
                    if (entity.IsValid && entity.EntityType == EntityTypes.Monster && 
                        entity.EntityState != EntityStates.MonsterFriendly && 
                        entity.TryGetComponent<Render>(out var debugRender))
                    {
                        var debugDist = Vector2.Distance(playerPos, new Vector2(debugRender.GridPosition.X, debugRender.GridPosition.Y));
                        debugDistances.Add(debugDist);
                    }
                }
                if (debugDistances.Any())
                {
                    ImGui.Text($"Closest 5 distances: {string.Join(", ", debugDistances.Select(d => d.ToString("F1")))}");
                    ImGui.Text($"Min: {debugDistances.Min():F1}, Max: {debugDistances.Max():F1}");
                }
                ImGui.Text($"Player Entity ID: {player.Id}");
                
                var gameWindowRect = Core.Process.WindowArea;
                ImGui.Text($"Game Window: {gameWindowRect.X},{gameWindowRect.Y} {gameWindowRect.Width}x{gameWindowRect.Height}");
                
                var debugPlayerScreenPos = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
                    new Vector2(playerPos.X, playerPos.Y), 0f);
                ImGui.Text($"Player Screen: {debugPlayerScreenPos.X:F0}, {debugPlayerScreenPos.Y:F0}");
                
                if (targetedMonster != null)
                {
                    ImGui.Separator();
                    ImGui.Text("Target Info:");
                    if (targetedMonster.TryGetComponent<Render>(out var targetRender))
                    {
                        var targetPos = new Vector2(targetRender.GridPosition.X, targetRender.GridPosition.Y);
                        var distance = Vector2.Distance(playerPos, targetPos);
                        ImGui.Text($"Distance: {distance:F1}");
                    }
                    
                    if (targetedMonster.TryGetComponent<ObjectMagicProperties>(out var magicProps))
                    {
                        ImGui.Text($"Rarity: {magicProps.Rarity}");
                    }
                    
                    if (targetedMonster.TryGetComponent<Life>(out var life))
                    {
                        var healthPercent = (life.Health.Current / (float)life.Health.Total) * 100f;
                        ImGui.Text($"Health: {healthPercent:F1}%");
                    }
                    
                    if (targetedMonster.TryGetComponent<Render>(out var debugRender))
                    {
                        var targetWorldPos = new Vector2(debugRender.GridPosition.X, debugRender.GridPosition.Y);
                        var targetScreenPos = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(targetWorldPos, 0f);
                        ImGui.Text($"Target Screen Pos: {targetScreenPos.X:F0}, {targetScreenPos.Y:F0}");
                        
                        GetCursorPos(out POINT currentMousePos);
                        ImGui.Text($"Current Mouse: {currentMousePos.X}, {currentMousePos.Y}");
                        
                        var finalX = (int)(Core.Process.WindowArea.X + targetScreenPos.X);
                        var finalY = (int)(Core.Process.WindowArea.Y + targetScreenPos.Y);
                        ImGui.Text($"Target Final: {finalX}, {finalY}");
                        
                        var distance = Math.Sqrt(Math.Pow(finalX - currentMousePos.X, 2) + Math.Pow(finalY - currentMousePos.Y, 2));
                        ImGui.Text($"Mouse Distance: {distance:F1}px");
                    }
                }
                ImGui.End();
            }
            catch
            {
                // Handle errors silently
            }
        }

        private void DrawRaycastRangeCircle(Vector2 playerPos, AreaInstance currentArea, Entity targetedMonster = null)
        {
            try
            {
                var gameState = Core.States.InGameStateObject;
                if (gameState?.CurrentWorldInstance == null)
                    return;

                var player = currentArea.Player;
                if (!player.TryGetComponent<Render>(out var playerRender))
                    return;

                // Use background draw list to always show (like Core nearby monster visualization)
                var drawList = ImGui.GetBackgroundDrawList();
                
                var playerWorldPos = playerRender.WorldPosition;
                var playerScreenPos = gameState.CurrentWorldInstance.WorldToScreen(playerWorldPos, playerWorldPos.Z);
                
            
                var targetRangeInWorld = this.Settings.TargetingRange * currentArea.WorldToGridConvertor;
                var targetRangePointWorld = new GameOffsets.Natives.StdTuple3D<float> 
                { 
                    X = playerWorldPos.X + targetRangeInWorld, 
                    Y = playerWorldPos.Y, 
                    Z = playerWorldPos.Z 
                };
                var targetRangePointScreen = gameState.CurrentWorldInstance.WorldToScreen(targetRangePointWorld, playerWorldPos.Z);
                var targetRadiusInPixels = Math.Abs(targetRangePointScreen.X - playerScreenPos.X);
                
                // Draw Targeting Range Circle (always visible when ShowRangeCircle is enabled)
                if (this.Settings.ShowRangeCircle)
                {
                    var targetCircleColor = this.Settings.IsEnabled ? 
                        ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 1, 0, 0.8f)) : // Green
                        ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.5f)); // Gray
                    
                    drawList.AddCircle(
                        new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 
                        targetRadiusInPixels, 
                        targetCircleColor, 
                        32, 
                        2.0f
                    );
                    
                    // Add label for targeting range
                    drawList.AddText(
                        new System.Numerics.Vector2(playerScreenPos.X + targetRadiusInPixels - 50, playerScreenPos.Y - 10),
                        targetCircleColor,
                        $"Targeting ({this.Settings.TargetingRange:F0})"
                    );
                }
                
                // Draw RayCast Range Circle (visible when ShowRangeCircle OR ShowWalkableGrid is enabled)
                if (this.Settings.ShowRangeCircle || this.Settings.ShowWalkableGrid)
                {
                    var rayRangePointWorld = new GameOffsets.Natives.StdTuple3D<float> 
                    { 
                        X = playerWorldPos.X + this.Settings.RayCastRange, 
                        Y = playerWorldPos.Y, 
                        Z = playerWorldPos.Z 
                    };
                    var rayRangePointScreen = gameState.CurrentWorldInstance.WorldToScreen(rayRangePointWorld, playerWorldPos.Z);
                    var rayRadiusInPixels = Math.Abs(rayRangePointScreen.X - playerScreenPos.X);
                    
                    var rayCircleColor = this.Settings.ShowWalkableGrid ? 
                        ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 0.8f, 1, 0.6f)) :
                        ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f, 0.3f, 0.5f, 0.3f));
                    
                    if (rayRadiusInPixels < 1000)
                    {
                        drawList.AddCircle(
                            new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 
                            rayRadiusInPixels, 
                            rayCircleColor, 
                            64, 
                            3.0f 
                        );
                        
                        // Add label for raycast range
                        drawList.AddText(
                            new System.Numerics.Vector2(playerScreenPos.X + rayRadiusInPixels - 50, playerScreenPos.Y + 15),
                            rayCircleColor,
                            $"RayCast ({this.Settings.RayCastRange:F0})"
                        );
                    }
                }
                
                // Draw Auto-Skill Range Circle
                if (this.Settings.ShowAutoSkillRange && this.Settings.EnableAutoSkill)
                {
                    var skillRangeInWorld = this.Settings.AutoSkillRange * currentArea.WorldToGridConvertor;
                    var skillRangePointWorld = new GameOffsets.Natives.StdTuple3D<float> 
                    { 
                        X = playerWorldPos.X + skillRangeInWorld, 
                        Y = playerWorldPos.Y, 
                        Z = playerWorldPos.Z 
                    };
                    var skillRangePointScreen = gameState.CurrentWorldInstance.WorldToScreen(skillRangePointWorld, playerWorldPos.Z);
                    var skillRadiusInPixels = Math.Abs(skillRangePointScreen.X - playerScreenPos.X);
                    
                    // Purple/magenta color for auto-skill range
                    var skillCircleColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 0.0f, 1.0f, 0.7f));
                    
                    if (skillRadiusInPixels < 1000)
                    {
                        drawList.AddCircle(
                            new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 
                            skillRadiusInPixels, 
                            skillCircleColor, 
                            48, 
                            2.5f 
                        );
                        
                        // Add label for auto-skill range
                        drawList.AddText(
                            new System.Numerics.Vector2(playerScreenPos.X - skillRadiusInPixels + 10, playerScreenPos.Y - 10),
                            skillCircleColor,
                            $"Auto-Skill ({this.Settings.AutoSkillRange:F0})"
                        );
                    }
                }
                
                // Draw Auto-Chest Range Circles
                if (this.Settings.EnableAutoChest)
                {
                    // Chest detection range
                    if (this.Settings.ShowChestRange)
                    {
                        var chestRangeInWorld = this.Settings.AutoChestRange * currentArea.WorldToGridConvertor;
                        var chestRangePointWorld = new GameOffsets.Natives.StdTuple3D<float> 
                        { 
                            X = playerWorldPos.X + chestRangeInWorld, 
                            Y = playerWorldPos.Y, 
                            Z = playerWorldPos.Z 
                        };
                        var chestRangePointScreen = gameState.CurrentWorldInstance.WorldToScreen(chestRangePointWorld, playerWorldPos.Z);
                        var chestRadiusInPixels = Math.Abs(chestRangePointScreen.X - playerScreenPos.X);
                        
                        // Brown/gold color for chest range
                        var chestCircleColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.8f, 0.6f, 0.2f, 0.7f));
                        
                        if (chestRadiusInPixels < 1000)
                        {
                            drawList.AddCircle(
                                new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 
                                chestRadiusInPixels, 
                                chestCircleColor, 
                                48, 
                                2.5f 
                            );
                            
                            // Add label for chest range
                            drawList.AddText(
                                new System.Numerics.Vector2(playerScreenPos.X - chestRadiusInPixels + 10, playerScreenPos.Y + 15),
                                chestCircleColor,
                                $"Auto-Chest ({this.Settings.AutoChestRange:F0})"
                            );
                        }
                    }
                    
                    // Safety check range
                    if (this.Settings.ShowSafetyRange && this.Settings.OnlyOpenWhenSafe)
                    {
                        var safetyRangeInWorld = this.Settings.SafetyCheckRange * currentArea.WorldToGridConvertor;
                        var safetyRangePointWorld = new GameOffsets.Natives.StdTuple3D<float> 
                        { 
                            X = playerWorldPos.X + safetyRangeInWorld, 
                            Y = playerWorldPos.Y, 
                            Z = playerWorldPos.Z 
                        };
                        var safetyRangePointScreen = gameState.CurrentWorldInstance.WorldToScreen(safetyRangePointWorld, playerWorldPos.Z);
                        var safetyRadiusInPixels = Math.Abs(safetyRangePointScreen.X - playerScreenPos.X);
                        
                        // Red/orange color for safety range
                        var safetyCircleColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 0.4f, 0.0f, 0.6f));
                        
                        if (safetyRadiusInPixels < 1000)
                        {
                            drawList.AddCircle(
                                new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 
                                safetyRadiusInPixels, 
                                safetyCircleColor, 
                                64, 
                                1.5f 
                            );
                            
                            // Add label for safety range
                            drawList.AddText(
                                new System.Numerics.Vector2(playerScreenPos.X + safetyRadiusInPixels - 80, playerScreenPos.Y - 25),
                                safetyCircleColor,
                                $"Safety ({this.Settings.SafetyCheckRange:F0})"
                            );
                        }
                    }
                }
                
                // Always draw player position when any visualization is active
                if (this.Settings.ShowRangeCircle || this.Settings.ShowWalkableGrid || 
                    (this.Settings.EnableAutoSkill && this.Settings.ShowAutoSkillRange) ||
                    (this.Settings.EnableAutoChest && this.Settings.ShowChestRange) ||
                    (this.Settings.EnableAutoChest && this.Settings.ShowSafetyRange && this.Settings.OnlyOpenWhenSafe))
                {
                    var playerColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 0.0f, 1.0f)); // Yellow
                    drawList.AddCircleFilled(new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 8.0f, playerColor);
                }

                if (this.Settings.ShowTargetLines && targetedMonster != null)
                {
                    if (targetedMonster.TryGetComponent<Render>(out var targetRender))
                    {
                        var targetWorldPos = targetRender.WorldPosition;
                        var targetScreenPos = gameState.CurrentWorldInstance.WorldToScreen(targetWorldPos, targetWorldPos.Z);
                        
   
                        var lineColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 0.5f, 0.0f, 0.8f)); // Orange
                        drawList.AddLine(
                            new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y),
                            new System.Numerics.Vector2(targetScreenPos.X, targetScreenPos.Y),
                            lineColor, 
                            3.0f
                        );
                        
   
                        var targetColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 0.0f, 1.0f, 1.0f)); // Magenta
                        drawList.AddCircleFilled(new System.Numerics.Vector2(targetScreenPos.X, targetScreenPos.Y), 10.0f, targetColor);
                    }
                }
                
                if (this.Settings.ShowWalkableGrid)
                {
                    DrawWalkableGrid(new Vector2(playerWorldPos.X, playerWorldPos.Y), currentArea);
                }
                
            }
            catch
            {
         
            }
        }
        

        private void DrawWalkableGrid(Vector2 playerPos, AreaInstance currentArea)
        {
            try
            {
                var gameState = Core.States.InGameStateObject;
                if (gameState?.CurrentWorldInstance == null)
                    return;

                // Use foreground draw list to show on top of GameHelper UI
                var drawList = ImGui.GetForegroundDrawList();
                
                var playerGridX = (int)(playerPos.X / currentArea.WorldToGridConvertor);
                var playerGridY = (int)(playerPos.Y / currentArea.WorldToGridConvertor);
                
                var gridSize = (int)(this.Settings.RayCastRange / currentArea.WorldToGridConvertor);
                
                
                for (var y = -gridSize; y <= gridSize; y++)
                for (var x = -gridSize; x <= gridSize; x++)
                {
                    
                    if (x * x + y * y > gridSize * gridSize) continue;

                    var gridX = playerGridX + x;
                    var gridY = playerGridY + y;
                    
                   
                    var walkableValue = RayCaster.GetWalkableValue(currentArea, gridX, gridY);
                    if (walkableValue < 0) continue; 
                    
                    
                    var worldX = gridX * currentArea.WorldToGridConvertor;
                    var worldY = gridY * currentArea.WorldToGridConvertor;
                    
                    
                    var worldPos3D = new GameOffsets.Natives.StdTuple3D<float> { X = worldX, Y = worldY, Z = 0f };
                    var screenPos = gameState.CurrentWorldInstance.WorldToScreen(worldPos3D, 0f);
                    
                    uint color;
                    
                    if (x == 0 && y == 0)
                    {
                        color = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 1.0f)); // Yellow
                    }
                    else
                    {
                        color = walkableValue switch
                        {
                            0 => ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 0.8f)), // Red - blocked
                            1 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 0.8f)), // Green - walkable
                            2 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.8f, 0.2f, 0.8f)), // Light green
                            3 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.6f, 0.4f, 0.8f)), // Green-blue
                            4 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.4f, 0.8f, 0.8f)), // Blue
                            5 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.2f, 1.0f, 0.8f)), // Bright blue
                            _ => ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.5f))  // Gray - unknown
                        };
                    }
                    
                    
                    var pointSize = (x == 0 && y == 0) ? 8f : 3f;
                    drawList.AddCircleFilled(screenPos, pointSize, color);
                    
                    
                    if ((Math.Abs(x) % 5 == 0 && Math.Abs(y) % 5 == 0) || (x == 0 && y == 0))
                    {
                        drawList.AddText(new Vector2(screenPos.X + 6, screenPos.Y - 8), 
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.9f)), 
                            $"{walkableValue}");
                    }
                }
                
               
                var playerWorldPos3D = new GameOffsets.Natives.StdTuple3D<float> { X = playerPos.X, Y = playerPos.Y, Z = 0f };
                var playerScreenPos = gameState.CurrentWorldInstance.WorldToScreen(playerWorldPos3D, 0f);
                
                drawList.AddText(new Vector2(playerScreenPos.X, playerScreenPos.Y - 60), 
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), 
                    $"WALKABLE GRID - Range: {this.Settings.RayCastRange} | Grid Size: {gridSize}");
                    
                drawList.AddText(new Vector2(playerScreenPos.X, playerScreenPos.Y - 45), 
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f)), 
                    "Red=0(blocked) ");
                    
                drawList.AddText(new Vector2(playerScreenPos.X + 100, playerScreenPos.Y - 45), 
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f)), 
                    "Green=1-2(walk) ");
                    
                drawList.AddText(new Vector2(playerScreenPos.X + 220, playerScreenPos.Y - 45), 
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.2f, 1.0f, 1.0f)), 
                    "Blue=3-5(various)");
                    
                drawList.AddText(new Vector2(playerScreenPos.X, playerScreenPos.Y - 30), 
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), 
                    $"Player Grid: {playerGridX}, {playerGridY} | WorldToGrid: {currentArea.WorldToGridConvertor:F2}");
            }
            catch (Exception ex)
            {
               
                var gameState = Core.States.InGameStateObject;
                if (gameState?.CurrentWorldInstance != null)
                {
                    var playerWorldPos3D = new GameOffsets.Natives.StdTuple3D<float> { X = playerPos.X, Y = playerPos.Y, Z = 0f };
                    var playerScreenPos = gameState.CurrentWorldInstance.WorldToScreen(playerWorldPos3D, 0f);
                    var drawList = ImGui.GetBackgroundDrawList();
                    drawList.AddText(new Vector2(playerScreenPos.X, playerScreenPos.Y - 70), 
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f)), 
                        $"GRID ERROR: {ex.Message}");
                }
            }
        }
         
    }
}