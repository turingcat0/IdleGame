# 存档系统框架

模块化、版本化的存档系统。每个子系统独立负责自己的 Collect / Apply / Default，便于未来扩展关卡系统等功能。

## 文件结构

```
Gameplay/Save/
├── SaveSystem.lua.txt      -- 协调器：注册模块、收集、应用、读写磁盘
├── SaveMigration.lua.txt   -- 版本迁移器
├── PlayerSave.lua.txt      -- 玩家状态（等级、经验、血量、蓝量、位置等）
├── InventorySave.lua.txt   -- 背包与装备（金币、水晶、装备槽位）
├── SkillSave.lua.txt       -- 技能配置与冷却
├── ServantSave.lua.txt     -- 侍从解锁状态
├── WorldSave.lua.txt       -- 世界/关卡进度（占位，待接入关卡系统）
├── MetaSave.lua.txt        -- 元数据（总时长、死亡次数等）
└── README.md
```

C# 桥接：

```
Assets/Resources/Scripts/SaveManager.cs
```

## 使用方式（尚未接入现有流程）

### 1. 注册模块

在启动时（例如 GameManager），注册需要的存档模块：

```lua
local SaveSystem = require("Gameplay.Save.SaveSystem")
local PlayerSave = require("Gameplay.Save.PlayerSave")
local InventorySave = require("Gameplay.Save.InventorySave")
-- ...

SaveSystem.RegisterModule("player", PlayerSave)
SaveSystem.RegisterModule("inventory", InventorySave)
```

### 2. 保存

```lua
local context = { player = GameManager.player }
SaveSystem.Save(context)
```

### 3. 加载

```lua
local context = { player = GameManager.player }
if SaveSystem.HasSave() then
    SaveSystem.Load(context)
end
```

## 模块接口

每个模块需要实现以下函数：

```lua
local M = {}

-- 从 context 收集数据，返回可 JSON 序列化的 table
function M.Collect(context) end

-- 将 data 应用到 context
function M.Apply(context, data) end

-- （可选）返回该模块的默认数据
function M.Default() end

return M
```

## 版本迁移

当存档结构发生变化时：

1. 修改 `SaveSystem.VERSION` 版本号。
2. 在 `SaveMigration.MIGRATIONS` 中新增对应版本的迁移函数。
3. 迁移函数接收并修改整个存档数据表。

示例见 `SaveMigration.lua.txt`。

## 注意事项

- 当前各 `*Save.lua.txt` 模块为占位实现，TODO 标记了接入点。
- 尚未接入现有游戏流程，不会影响当前运行逻辑。
- 从 C# 桥接读取/写入时，Lua 侧通过 `CS.xluaDemo.SaveManager` 访问。
