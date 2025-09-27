using System.Numerics;
using GameHelper.Plugin;
using GameHelper.RemoteEnums;

namespace AutoAim
{
    /// <summary>
    /// Target priority modes for auto aim
    /// </summary>
    public enum TargetPriority
    {
        Closest,
        LowestHealth,
        HighestRarity
    }

    /// <summary>
    /// AutoAim plugin settings
    /// </summary>
    public sealed class AutoAimSettings : IPSettings
    {
        /// <summary>
        /// Enable/disable auto aim system
        /// </summary>
        public bool EnableAutoAim = false;

        /// <summary>
        /// Enable/disable auto aim system (alias for EnableAutoAim)
        /// </summary>
        public bool IsEnabled
        {
            get => EnableAutoAim;
            set => EnableAutoAim = value;
        }

        /// <summary>
        /// Virtual key code for toggle (F1=112, F2=113, etc.)
        /// </summary>
        public int ToggleKey = 114; // F3 by default

        /// <summary>
        /// Mouse movement speed multiplier
        /// </summary>
        public float MouseSpeed = 1.0f;

        /// <summary>
        /// Enable smooth mouse movement
        /// </summary>
        public bool SmoothMovement = true;

        /// <summary>
        /// Show walkable grid debug overlay
        /// </summary>
        public bool ShowWalkableGrid = true;

        /// <summary>
        /// Enable line of sight checks (disable to target through walls)
        /// </summary>
        public bool EnableLineOfSight = true;

        /// <summary>
        /// Show range circle around player
        /// </summary>
        public bool ShowRangeCircle = true;

        /// <summary>
        /// Show target lines from player to target
        /// </summary>
        public bool ShowTargetLines = true;

        /// <summary>
        /// Show debug window with targeting info
        /// </summary>
        public bool ShowDebugWindow = true;

        /// <summary>
        /// Current toggle state (internal)
        /// </summary>
        public bool IsToggleActive = false;

        /// <summary>
        /// Maximum range for targeting monsters (in grid units)
        /// </summary>
        public float TargetingRange = 80f;

        /// <summary>
        /// Range for ray casting line of sight checks and walkable grid
        /// </summary>
        public float RayCastRange = 50f;

        /// <summary>
        /// Target normal rarity monsters (white)
        /// </summary>
        public bool TargetNormal = true;

        /// <summary>
        /// Target magic rarity monsters (blue)
        /// </summary>
        public bool TargetMagic = true;

        /// <summary>
        /// Target rare rarity monsters (yellow)
        /// </summary>
        public bool TargetRare = true;

        /// <summary>
        /// Target unique rarity monsters (orange)
        /// </summary>
        public bool TargetUnique = true;

        /// <summary>
        /// Speed of mouse movement (higher = faster)
        /// </summary>
        public float AimSpeed = 1.0f;

        /// <summary>
        /// Smoothing factor for aiming (0 = instant, 1 = very smooth)
        /// </summary>
        public float AimSmoothing = 0.3f;

        /// <summary>
        /// Aim at center of monster instead of bottom
        /// </summary>
        public bool AimAtCenter = true;

        /// <summary>
        /// Target selection priority
        /// </summary>
        public TargetPriority TargetPriority = TargetPriority.Closest;

        /// <summary>
        /// Show visual indicator on current target
        /// </summary>
        public bool ShowTargetIndicator = true;

        /// <summary>
        /// Delay before switching targets (milliseconds)
        /// </summary>
        public float TargetSwitchDelay = 100f;

        /// <summary>
        /// Always prefer closest target over current target
        /// </summary>
        public bool PreferClosest = false;
    }
}