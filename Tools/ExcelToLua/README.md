# Excel/CSV → Lua 配置生成工具

把策划表格转成项目里可 `require` 的 Lua 配置文件。

## 目录结构

```
Config/Excel/              <-- 策划放 CSV 的位置
├── Skills.csv
├── Projectiles.csv
├── Enemies.csv
├── Servants.csv
└── Items.csv

Tools/ExcelToLua/
└── generator.py           <-- 生成器脚本

Assets/Resources/Lua/Config/Generated/  <-- 生成的 Lua 文件
├── Skills.lua.txt
├── Projectiles.lua.txt
├── Enemies.lua.txt
├── Servants.lua.txt
└── Items.lua.txt
```

## 使用方式

### 自动（推荐）

Unity Editor 启动或点击 Play 时，会自动检查 CSV 是否比已生成的 Lua 文件新；如果脏了，会自动调用 Python 生成器。无需手动操作。

### 手动

确保已安装 Python 3，然后在项目根目录执行：

```bash
python Tools/ExcelToLua/generator.py
```

Windows 下也可以双击 `Tools/ExcelToLua/generate.bat`，或在 Unity 菜单选择 `Tools/ExcelToLua/Generate Now`。

## 新增配置表

1. 在 `Config/Excel/` 下新建 `<Name>.csv`，第一列必须是 `id`。
2. 运行生成器，会自动生成 `Assets/Resources/Lua/Config/Generated/<Name>.lua.txt`。
3. 在对应的注册模块（如 `Gameplay.Skills.Skills`）里 `require("Config.Generated.<Name>")` 并循环 `Register`。

## CSV 约定

- 字段类型自动推断：空值 → `nil`，`true`/`false` → 布尔，数字 → 数字，其它 → 字符串。
- 枚举类型（如 `SkillType`、`EquipmentSlot`、`Rarity`）在 CSV 中写字符串名，Lua 注册模块里用 `SkillType[config.skillType]` 转换。
- 外键类型（如 `projectileId`、`skillId`、`equipmentSetId`）在 CSV 中写目标 id，Lua 注册模块里用 `Projectiles[config.projectileId]` 转换。
