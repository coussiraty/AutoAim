# AutoAim Plugin v2.0 🎯

Advanced auto-aim plugin for Path of Exile with revolutionary **Parallel Combo System** and intelligent targeting. Built on GameHelper2 framework with professional English interface and optimized performance.

## 🚀 What's New in v2.0

### ⚡ **Revolutionary Parallel Combo System**
- **Independent Skill Cooldowns**: Each skill operates on its own timer
- **Sequential Execution**: Skills execute based on ready-time priority (first-come-first-serve)
- **Non-Blocking Queue**: No skill blocks others from executing
- **Real-time Conversion**: Automatically converts old combos to new parallel system
- **Advanced Debug**: Enhanced debugging with parallel combo status tracking

### � **Enhanced Auto-Skill Logic**
- **Hold-While-Targeting**: Improved from toggle-based to hold-while-targeting system
- **Smart Priority System**: Culling Strike → Parallel Combos → Auto-Skill → Default behavior
- **Intelligent Target Detection**: Mouse-based targeting with fallback to nearest monster
- **Cooldown Management**: 200ms minimum delay between skill activations for human-like behavior

### ⚔️ **Advanced Culling Strike System**
- **AOE Monster Detection**: Finds ALL nearby monsters, not just mouse target
- **Rarity-Based Thresholds**: Individual execution thresholds per monster rarity
- **Priority Targeting**: Automatically selects best execution target
- **Smart Life Detection**: Accurate health percentage calculations

### 🌐 **Professional English Interface**
- **Complete Translation**: 100% English interface, no Portuguese text remaining
- **Simplified UI**: Removed complex configuration sections for better UX
- **Consistent Terminology**: Professional button labels and tooltips
- **Enhanced Debug**: Repositioned debug information for better visibility

### 🔧 **Technical Improvements**
- **Optimized Performance**: More efficient entity processing and memory usage
- **Enhanced Stability**: Improved error handling and edge case management
- **Better Architecture**: Cleaner code structure with improved maintainability
- **Advanced Logging**: Comprehensive debug information for troubleshooting

---

## ✨ Core Features

### 🎯 **Smart Auto-Targeting**
- **Advanced Monster Detection**: Automatically finds and prioritizes valid targets
- **Distance-Based Priority**: Closest monster gets priority, with health-based tiebreaking
- **Real-time Target Switching**: Instant target updates when better options appear
- **Rarity-Aware Targeting**: Different behaviors per monster rarity level

### 🚀 **Parallel Combo System**
- **Multi-Skill Coordination**: Execute multiple skills simultaneously with individual cooldowns
- **Sequential Priority**: Skills execute in order of readiness, not static priority
- **Flexible Configuration**: Mix sequential and parallel combos as needed
- **Smart Conversion**: Automatically upgrades old combo configurations

### ⚔️ **Intelligent Culling Strike**
- **AOE Detection**: Scans all nearby monsters for execution opportunities
- **Rarity Thresholds**: Configurable execution percentages per rarity
  - **Normal**: 10% health threshold
  - **Magic**: 15% health threshold  
  - **Rare**: 20% health threshold
  - **Unique**: 25% health threshold
- **Priority Targeting**: Automatically finds best execution target

### 🎮 **Enhanced Movement Control**
- **Instant Positioning**: Direct cursor positioning to target location
- **Smooth Movement**: Optional smooth movement with configurable speed
- **Screen Boundary Safety**: Prevents mouse from moving outside game window
- **Multi-Monitor Support**: Works correctly across multiple displays

### � **Advanced Line of Sight**
- **Wall Detection**: Sophisticated raycasting prevents targeting through obstacles
- **Walkable Analysis**: Uses terrain data for accurate pathfinding
- **Performance Optimized**: Efficient LOS calculations with caching
- **Debug Visualization**: Real-time LOS status display

### 👾 **Monster Filtering**
- **Rarity-Based Filters**: Target specific monster rarities
- **Health-Based Culling**: Different execution thresholds per rarity
- **Targetable Detection**: Respects monster targetability flags
- **Range-Based Selection**: Configurable detection ranges

### 🎨 **Visual Debugging**
- **Enhanced Debug Window**: Comprehensive real-time information
- **Parallel Combo Status**: Live tracking of skill cooldowns and execution
- **Target Visualization**: Clear indication of current targets
- **Performance Metrics**: FPS impact and processing statistics

---

## �️ Installation

1. **Download** the latest `AutoAim.dll` from releases
2. **Place** the file in your GameHelper2 `Plugins/AutoAim/` folder
3. **Launch** GameHelper2 with administrator privileges
4. **Navigate** to **Plugins** → **AutoAim** in settings
5. **Configure** your preferred settings
6. **Press F3** (default) to toggle auto-aim functionality

---

## ⚙️ Configuration Guide

### 🎯 **Basic Auto-Aim Settings**
- **Enable Auto Aim**: Master toggle for all auto-aim functionality
- **Toggle Key**: Choose from F1-F4 keys (default: F3)
- **Targeting Range**: Monster detection distance (20-200 units)
- **Mouse Speed**: Movement speed multiplier (0.1x to 5.0x)

### ⚔️ **Culling Strike Configuration**
```
Rarity-Based Thresholds:
┌─────────┬─────────────┬─────────────┐
│ Rarity  │ Default %   │ Range       │
├─────────┼─────────────┼─────────────┤
│ Normal  │ 10%         │ 5% - 30%    │
│ Magic   │ 15%         │ 5% - 30%    │
│ Rare    │ 20%         │ 5% - 30%    │
│ Unique  │ 25%         │ 5% - 30%    │
└─────────┴─────────────┴─────────────┘
```

### 🚀 **Parallel Combo System**
- **Enable Combo System**: Activate advanced combo functionality
- **Individual Skill Cooldowns**: Each skill has independent timing
- **Sequential Execution**: Skills execute based on ready-time priority
- **Auto-Conversion**: Old combos automatically upgrade to parallel system

### 🎮 **Movement & Controls**
- **Smooth Movement**: Toggle between instant and smooth mouse movement
- **Movement Safety**: Prevents cursor from leaving game window
- **Auto-Skill Mode**: Choose between hold-while-targeting or press-release

### 👾 **Target Filters**
- **Target Normal**: Include white monsters
- **Target Magic**: Include blue monsters
- **Target Rare**: Include yellow monsters  
- **Target Unique**: Include orange monsters
- **Require Targetable**: Only target monsters that can be clicked

### 🎨 **Visual Options**
- **Show Debug Window**: Display comprehensive real-time information
- **Show Range Circle**: Visual representation of targeting range
- **Show Target Lines**: Draw lines from player to current target
- **Enable Line of Sight**: Toggle wall detection system

---

## 🎮 Controls & Usage

| Key | Action |
|-----|--------|
| **F3** | Toggle AutoAim on/off |
| **Hold F3** | Temporary activation (if configured) |
| **Settings Menu** | Access full configuration |

### 🎯 **Auto-Aim Behavior**
1. **Priority 1**: Culling Strike (if monsters below threshold)
2. **Priority 2**: Parallel Combos (if configured and available)
3. **Priority 3**: Standard Auto-Skill (default behavior)
4. **Priority 4**: Manual control (when no valid targets)

### ⚔️ **Culling Strike Usage**
- Automatically scans for low-health monsters
- Executes based on rarity-specific thresholds
- Prioritizes closest valid targets
- Works independently of mouse position

---

## 📊 Advanced Debug Information

### 🎯 **Targeting Statistics**
```
🎯 Target Info: Total: 15, Valid: 12, InRange: 5/5
📊 Distance: Closest: 45.2, Farthest: 156.8  
🖱️ Mouse: World(1245.6,892.1) → Screen(1024,768)
⚔️ Culling: 3 candidates, executing Rare(23% HP)
```

### 🚀 **Parallel Combo Status**
```
🚀 Parallel Combo: [Active] Testing Normal Monsters
   └─ Skill 1: Ready (0.0s) ✅
   └─ Skill 2: Cooldown (2.3s) ⏳  
   └─ Skill 3: Ready (0.0s) ✅
   └─ Next Execution: Skill 1 (immediate)
```

### 📈 **Performance Metrics**
- **Processing Time**: Entity scan duration
- **Memory Usage**: Current memory footprint
- **FPS Impact**: Performance cost measurement
- **Target Switching**: Rate of target changes

---

## 🔧 Technical Specifications

### 🏗️ **Architecture**
- **Framework**: GameHelper2 with .NET 8.0
- **Entity System**: Component-based monster detection
- **Threading**: Async-safe operations with proper synchronization
- **Memory Management**: Optimized garbage collection patterns

### ⚡ **Performance**
- **Entity Processing**: ~1ms average scan time
- **Memory Footprint**: <50MB typical usage
- **CPU Usage**: <2% on modern systems
- **FPS Impact**: <5% performance cost

### 🔌 **Compatibility**
- **Path of Exile**: All current versions
- **GameHelper2**: Latest framework versions
- **Operating System**: Windows 10/11 x64
- **Runtime**: .NET 8.0 Desktop Runtime

---

## 🐛 Troubleshooting

### ❌ **Auto-Aim Not Working**
1. ✅ Verify **Enable Auto Aim** is checked
2. ✅ Check at least one **Target Rarity** is enabled  
3. ✅ Ensure **Targeting Range** is sufficient (try 100+)
4. ✅ Confirm GameHelper2 has administrator privileges
5. ✅ Test with **Line of Sight** disabled

### 🖱️ **Mouse Movement Issues**
1. ✅ Check **Mouse Speed** setting (try 2.0+)
2. ✅ Verify Path of Exile window focus
3. ✅ Test with different **Movement** modes
4. ✅ Ensure no conflicting mouse software

### ⚔️ **Culling Strike Problems**
1. ✅ Adjust **Rarity Thresholds** (try higher percentages)
2. ✅ Verify monsters are within **Detection Range**
3. ✅ Check **Debug Window** for candidate information
4. ✅ Test with different monster rarities

### 🚀 **Parallel Combo Issues**
1. ✅ Enable **Combo System** in settings
2. ✅ Configure **Individual Skills** with proper cooldowns
3. ✅ Check **Debug Window** for execution status
4. ✅ Verify skill key bindings are correct

---

## 📋 Version History

### 🚀 **v2.0.0** (2025-10-04) - **MAJOR RELEASE**

#### ✨ **New Features**
- **Parallel Combo System**: Revolutionary multi-skill coordination with independent cooldowns
- **Sequential Execution**: Skills execute based on ready-time priority (first-come-first-serve)
- **Enhanced Culling Strike**: AOE monster detection with rarity-based execution thresholds
- **Hold-While-Targeting**: Improved auto-skill logic from toggle-based to hold-while-targeting
- **Professional English UI**: Complete interface translation with simplified UX

#### 🔧 **Technical Improvements**  
- **Non-Blocking Queue System**: Skills execute independently without blocking each other
- **Advanced Priority Logic**: Culling Strike → Parallel Combos → Auto-Skill hierarchy
- **Optimized Performance**: More efficient entity processing and memory management
- **Enhanced Debug System**: Repositioned debug info with parallel combo status tracking

#### 🎨 **Interface Enhancements**
- **Complete English Translation**: 100% professional English interface
- **Simplified Configuration**: Removed complex sections for better user experience  
- **Consistent Terminology**: Professional button labels and tooltips throughout
- **Enhanced Visual Feedback**: Better debug positioning and status indicators

#### 🐛 **Bug Fixes**
- Fixed skill blocking issues in combo execution
- Resolved mouse movement boundary problems
- Improved target selection accuracy
- Enhanced error handling and stability

#### ⚡ **Performance Optimizations**
- Reduced memory allocations in hot paths
- Optimized entity component access patterns
- Improved garbage collection efficiency
- Enhanced thread safety mechanisms

---

### 📚 **v1.0.0** (2025-09-27) - **Initial Release**
- ✨ Core auto-targeting functionality
- 🔍 Advanced line-of-sight system  
- 🎨 Visual debugging suite
- ⚙️ Comprehensive configuration options
- 👾 Monster rarity filtering
- 📊 Real-time debug information

---

## 🤝 Contributing

We welcome contributions! Please:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### 📝 **Development Guidelines**
- Follow existing code style and patterns
- Add comprehensive comments for complex logic
- Include appropriate error handling
- Test thoroughly before submitting
- Update documentation as needed

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **GameHelper2 Team** - Excellent framework foundation
- **Community Contributors** - Valuable testing and feedback
- **ImGui.NET** - Outstanding immediate mode GUI
- **Path of Exile Community** - Inspiration and support

---

## 📞 Support & Community

- **Discord**: [GameHelper2 Community](https://discord.com/invite/RShVpaEBV3)
- **GitHub Issues**: Report bugs and request features
- **Email**: [coussiraty@gmail.com](mailto:coussiraty@gmail.com)

---

## ⚠️ Important Disclaimer

This tool is designed for **educational and accessibility purposes**. Use responsibly and in accordance with Path of Exile's Terms of Service. The authors are not responsible for any consequences of usage.

---

## 👨‍💻 Author

**coussiraty** - *Lead Developer*
- GitHub: [@coussiraty](https://github.com/coussiraty)
- Email: [coussiraty@gmail.com](mailto:coussiraty@gmail.com)

---

<div align="center">

**Made with ❤️ for the GameHelper2 community**

[⬆️ Back to Top](#autoaim-plugin-v20-)

</div>