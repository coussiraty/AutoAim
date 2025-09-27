# AutoAim Plugin v1.0

Advanced auto-aim plugin for Path of Exile using GameHelper framework. Automatically targets and aims at nearby monsters with intelligent line-of-sight detection and customizable range settings.

## ✨ Features

### 🎯 **Auto Targeting**
- **Smart Monster Detection**: Automatically finds and targets valid monsters within configurable range
- **Distance-Based Priority**: Always targets the closest valid monster first
- **Real-time Target Switching**: Instantly switches to new targets when closer monsters appear

### 🎮 **Movement Control**
- **Instant Mouse Movement**: Direct cursor positioning to target location
- **Smooth Movement Options**: Toggle between instant and smooth mouse movement
- **Adjustable Mouse Speed**: Configurable movement speed multiplier (0.1x to 5.0x)

### 🔍 **Line of Sight System**
- **Intelligent Wall Detection**: Uses advanced raycasting to avoid targeting through walls
- **Walkable Grid Analysis**: Analyzes terrain data to ensure clear line of sight
- **Bypass Option**: Optional toggle to target through walls for testing

### ⚙️ **Range Configuration**
- **Targeting Range**: Configurable monster detection range (10-200 grid units)
- **Visual Range**: Separate range for visual debugging and grid display (50-1000 units)
- **Real-time Adjustment**: Change ranges on-the-fly without restart

### 👾 **Monster Rarity Filters**
- **Normal Monsters** (White): Target common monsters
- **Magic Monsters** (Blue): Target magic monsters  
- **Rare Monsters** (Yellow): Target rare monsters
- **Unique Monsters** (Orange): Target unique monsters
- **Mix & Match**: Enable any combination of rarity types

### 🎨 **Visual Indicators**
- **Range Circles**: Visual representation of targeting and raycast ranges
- **Target Lines**: Lines from player to current target
- **Walkable Grid**: Debug overlay showing terrain walkability values
- **Target Markers**: Highlight current target with colored indicators

### 🛠️ **Debug & Monitoring**
- **Real-time Statistics**: Live monster count, targeting info, and performance metrics
- **Distance Tracking**: Shows closest/farthest monster distances
- **Filter Analytics**: Breakdown of monsters by validation stage
- **Mouse Coordinates**: Current and target mouse positions
- **Line of Sight Status**: Visual feedback on targeting decisions

## 🚀 Installation

1. Place `AutoAim.dll` in your GameHelper `Plugins` folder
2. Launch GameHelper
3. Navigate to **Plugins** → **AutoAim** in the settings
4. Configure your preferred settings
5. Press **F3** (default) to toggle auto-aim on/off

## ⚙️ Configuration

### Basic Settings
- **Toggle Key**: F1, F2, F3, or F4 (default: F3)
- **Enable Auto Aim**: Master on/off switch
- **Targeting Range**: Maximum distance for monster detection (default: 80)
- **RayCast Range**: Range for visual debugging (default: 50)

### Target Filters
- **Target Normal**: Include white monsters ✅
- **Target Magic**: Include blue monsters ✅  
- **Target Rare**: Include yellow monsters ✅
- **Target Unique**: Include orange monsters ✅

### Movement
- **Mouse Speed**: Movement speed multiplier (default: 1.0)
- **Smooth Movement**: Toggle smooth vs instant movement

### Visual Options
- **Show Range Circle**: Display targeting range visualization
- **Show Walkable Grid**: Show terrain walkability overlay
- **Show Target Lines**: Draw lines to current target
- **Show Debug Window**: Display real-time statistics
- **Enable Line of Sight**: Toggle wall detection

### Advanced
- **Target Switch Delay**: Delay before switching targets (0-1000ms)
- **Prefer Closest Target**: Always prioritize closest monster

## 🎮 Controls

| Key | Action |
|-----|--------|
| **F3** | Toggle AutoAim on/off (configurable) |
| **Settings Menu** | Access full configuration options |

## 📊 Debug Information

The debug window provides comprehensive targeting information:

```
Target Info: Total: 15, Valid: 12, InRange: 5/5, OutRange: 7, AfterLOS: 3, AfterRarity: 3, Target: YES
TargetRange: 100, Blocked: 2, Closest: 45.2, Farthest: 156.8
Mouse Movement: World: 1245.6,892.1 -> Screen: 1024,768 MOVED
```

- **Total**: Total monsters found in area
- **Valid**: Monsters passing basic validation  
- **InRange**: Monsters within targeting range
- **AfterLOS**: Monsters with clear line of sight
- **AfterRarity**: Monsters matching rarity filters
- **Target**: Whether a valid target was found
- **Blocked**: Monsters blocked by walls

## 🔧 Technical Details

### Architecture
- **Entity System**: Utilizes GameHelper's entity framework
- **Component-Based**: Accesses Render, Life, and ObjectMagicProperties components
- **Grid-Based Calculations**: Uses Path of Exile's internal grid system
- **Screen Coordinate Mapping**: Converts world positions to screen coordinates

### Performance
- **Optimized Scanning**: Efficient entity filtering and distance calculations  
- **Selective Updates**: Only processes entities when needed
- **Memory Efficient**: Minimal memory footprint and garbage collection
- **Thread Safe**: Designed for GameHelper's threading model

### Compatibility
- **Path of Exile**: All current versions
- **GameHelper**: Compatible with latest GameHelper framework
- **Windows**: Windows 10/11 with .NET 8.0

## 📋 Requirements
- **GameHelper2**: Latest version
- **.NET 8.0**: Runtime required
- **Windows 10/11**: Operating system
- **Administrator Privileges**: For mouse control

## 🐛 Troubleshooting

### Auto-aim not working?
1. Check if **Enable Auto Aim** is checked
2. Verify at least one **Target Rarity** is enabled
3. Ensure **Targeting Range** is sufficient (try 100+)
4. Check if **Line of Sight** is blocking targets (disable for testing)

### Mouse not moving?
1. Verify GameHelper is running with admin privileges
2. Check **Mouse Speed** setting (try increasing to 2.0+)
3. Ensure Path of Exile window is focused and in foreground

### No targets found?
1. Increase **Targeting Range** value
2. Enable all **Target Rarities** for testing
3. Disable **Line of Sight** temporarily
4. Check debug window for monster counts

## 📝 Changelog

### v1.0.0 (2025-09-27)
- ✨ Initial release
- 🎯 Core auto-targeting functionality
- 🔍 Advanced line-of-sight system
- 🎨 Complete visual debugging suite
- ⚙️ Comprehensive configuration options
- 👾 Monster rarity filtering
- 📊 Real-time debug information
- 🚀 Optimized performance

## 🤝 Contributing

Contributions are welcome! Please feel free to:
- Report bugs via GitHub issues
- Suggest new features
- Submit pull requests
- Improve documentation

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **GameHelper2 Team** - For the excellent framework
- **Community Contributors** - For testing and feedback  
- **ImGui.NET** - For the immediate mode GUI framework

## 📞 Support

- **Discord**: [GameHelper2 Community](https://discord.com/invite/RShVpaEBV3)

## 📋 Requirements

- GameHelper2 framework
- .NET 8.0
- Windows 10/11

## ⚠️ Disclaimer

This tool is designed for educational and accessibility purposes. Use at your own discretion and in accordance with Path of Exile's Terms of Service.

## 👨‍💻 Author

**coussirat** - [coussiraty@gmail.com](mailto:coussiraty@gmail.com)

---

*Made with ❤️ for the GameHelper2 community*