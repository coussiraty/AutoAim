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
using System.Runtime.InteropServices;  // P/Invoke támogatás szükséges [web:32]

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
        public override void DrawSettings()
        {
            ImGui.Text("=== AUTO AIM SETTINGS ===");
            ImGui.TextWrapped("This plugin automatically aims at nearby monsters. Use the toggle key to enable/disable.");

            ImGui.Separator();
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Keybind Settings"))
            {
                ImGui.Text("Toggle Key:");
                if (ImGui.RadioButton("F1", this.Settings.ToggleKey == 112)) this.Settings.ToggleKey = 112;
                ImGui.SameLine();
                if (ImGui.RadioButton("F2", this.Settings.ToggleKey == 113)) this.Settings.ToggleKey = 113;
                ImGui.SameLine();
                if (ImGui.RadioButton("F3", this.Settings.ToggleKey == 114)) this.Settings.ToggleKey = 114;
                ImGui.SameLine();
                if (ImGui.RadioButton("F4", this.Settings.ToggleKey == 115)) this.Settings.ToggleKey = 115;
                ImGui.SameLine();
                if (ImGui.RadioButton("Mouse4", this.Settings.ToggleKey == VK_XBUTTON1))
                    this.Settings.ToggleKey = VK_XBUTTON1;
                ImGui.SameLine();
                if (ImGui.RadioButton("Mouse5", this.Settings.ToggleKey == VK_XBUTTON2))
                    this.Settings.ToggleKey = VK_XBUTTON2;
                ImGui.Separator();
                ImGui.Separator();
                bool isEnabled = this.Settings.IsEnabled;
                if (ImGui.Checkbox("Enable Auto Aim", ref isEnabled))
                {
                    this.Settings.IsEnabled = isEnabled;
                }
                ImGuiHelper.ToolTip("Enable or disable the auto aim functionality");
            }
            ImGui.Separator();
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Targeting Settings"))
            {
                ImGui.SliderFloat("Targeting Range", ref this.Settings.TargetingRange, 10f, 200f);
                ImGuiHelper.ToolTip("Maximum range for targeting monsters (in grid units)");

                ImGui.SliderFloat("RayCast Range (Visual)", ref this.Settings.RayCastRange, 50f, 1000f);
                ImGuiHelper.ToolTip("Range for walkable grid visualization and line-of-sight checks");
                ImGui.Separator();
                ImGui.Separator();
                ImGui.Checkbox("Target Normal (White)", ref this.Settings.TargetNormal);
                ImGui.Checkbox("Target Magic (Blue)", ref this.Settings.TargetMagic);
                ImGui.Checkbox("Target Rare (Yellow)", ref this.Settings.TargetRare);
                ImGui.Checkbox("Target Unique (Orange)", ref this.Settings.TargetUnique);
            }



            ImGui.Separator();
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Movement / Speed Settings"))
            { 
                ImGui.SliderFloat("Mouse Speed", ref this.Settings.MouseSpeed, 0.1f, 5.0f);
                ImGuiHelper.ToolTip("How fast the mouse moves to targets");

                bool smoothMovement = this.Settings.SmoothMovement;
                if (ImGui.Checkbox("Smooth Movement", ref smoothMovement))
                {
                    this.Settings.SmoothMovement = smoothMovement;
                }
                ImGuiHelper.ToolTip("Smooth mouse movement vs direct movement");
            }
            ImGui.Separator();
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Visual Settings"))
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
            
                ImGui.Checkbox("Enable Line of Sight", ref this.Settings.EnableLineOfSight);
                ImGuiHelper.ToolTip("Enable line-of-sight checks (uncheck to target through walls)");
            
                ImGui.Text($"Grid Debug Status: {(this.Settings.ShowWalkableGrid ? "ON" : "OFF")}");
            }
            ImGui.Separator();
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Advanced Settings"))
            {
                ImGui.SliderFloat("Target Switch Delay (ms)", ref this.Settings.TargetSwitchDelay, 0f, 1000f);
                ImGuiHelper.ToolTip("Delay before switching to a new target");
                
                ImGui.Checkbox("Prefer Closest Target", ref this.Settings.PreferClosest);
                ImGuiHelper.ToolTip("Always target the closest monster vs keeping current target");

                ImGui.Separator();
                ImGui.Text("Audio Settings");
                ImGui.SameLine();
                if (ImGui.RadioButton("Beep Off", this.Settings.BeepSound == false))
                    this.Settings.BeepSound = false;
                ImGui.SameLine();
                if (ImGui.RadioButton("Beep On", this.Settings.BeepSound))
                    this.Settings.BeepSound = true;

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
        }



        public override void DrawUI()
        {
         
            HandleToggleKey();

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
            if (targetedMonster != null && !IsValidMonster(targetedMonster))
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

            if (this.Settings.ShowRangeCircle)
            {
                DrawRaycastRangeCircle(playerPos, currentAreaInstance, targetedMonster);
            }

            // Draw walkable grid if enabled
            if (this.Settings.ShowWalkableGrid)
            {
                DrawWalkableGrid(playerPos, currentAreaInstance);
            }

            // Draw debug info
            if (this.Settings.ShowDebugWindow)
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
            bool isToggleKeyPressed = (GetAsyncKeyState(this.Settings.ToggleKey) & 0x8000) != 0;
            
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

                if (!IsValidMonster(entity))
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
            
            this._debugInfo = $"Total: {totalMonsters}, Valid: {validMonsters}, InRange: {inRangeCount}/{inRangeMonsters}, OutRange: {outsideRangeCount}, AfterLOS: {afterLineOfSight}, AfterRarity: {afterRarity}, Target: {(bestTarget != null ? "YES" : "NO")}, TargetRange: {this.Settings.TargetingRange:F0}, Blocked: {inRangeMonsters - afterLineOfSight}, Closest: {(debugDistances.Any() ? minDist.ToString("F1") : "N/A")}, Farthest: {(debugDistances.Any() ? maxDist.ToString("F1") : "N/A")}";

            return bestTarget;
        }


        private bool IsValidMonster(Entity entity)
        {
            if (entity == null || !entity.IsValid)
                return false;


            if (entity.EntityType != EntityTypes.Monster)
                return false;

           
            if (entity.TryGetComponent<Life>(out var life))
            {
                if (!life.IsAlive)
                    return false;
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
                
                var targetCircleColor = this.Settings.IsEnabled ? 
                    ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 1, 0, 0.8f)) : // 
                    ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.5f)); // 
                
                drawList.AddCircle(
                    new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 
                    targetRadiusInPixels, 
                    targetCircleColor, 
                    32, 
                    2.0f
                );
                
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
                }
                
                var playerColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 0.0f, 1.0f)); // Yellow
                drawList.AddCircleFilled(new System.Numerics.Vector2(playerScreenPos.X, playerScreenPos.Y), 8.0f, playerColor);

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

                var drawList = ImGui.GetBackgroundDrawList();
                
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