# 约定速查（CONVENTIONS）

记录运行时所有「文件名 / 路径 / 动画参数 / id」约定。改资源或加新内容时按此命名即可，无需改代码注册。

## 1. Lua 模块加载
- 文件后缀 `.lua.txt`（xLua 以 TextAsset 加载）。
- `require` 路径用点号、不带后缀：`require("Gameplay.Vfx.VfxSpawner")` → `Assets/Resources/Lua/Gameplay/Vfx/VfxSpawner.lua.txt`。
- 类系统 rxi classic：`local X = Object:extend()`；构造 `function X:New(...)`；实例化直接 `X(...)`；调基类构造 `X.super.New(self)`。

## 2. Resources 路径约定（id 即路径段）
所有 `Resources.Load` 都按「固定前缀 + id」拼路径，**不逐项硬编码**。

| 类别 | 基路径 | id 来源 | 完整示例 |
|---|---|---|---|
| 敌人 AnimatorController | `AnimatorControllers/` | `enemy.id` | `AnimatorControllers/dino` |
| 敌人预制体 | `Prefabs/EnemyBase`（共用） | — | `Prefabs/EnemyBase` |
| 抛射物预制体 | `Prefabs/Projectiles/` | `projectile.id` | `Prefabs/Projectiles/default` |
| 特效预制体 | `Prefabs/Vfx/` | `vfxId`（见 §3、§7） | `Prefabs/Vfx/skill_player_default_cast` |
| Buff 图标 | `Textures/Buffs/` | `effect.id` | `Textures/Buffs/poison` |
| Buff 图标 prefab | `Prefabs/Buffs/` | 固定 | `Prefabs/Buffs/BuffIcon` |
| 技能图标 | `Textures/Skills/` | `skill.id` | `Textures/Skills/player_default` |

## 3. 特效命名约定（约定优先于注册）
技能特效 id 由 `skill.id` 派生（skillId 为 §5 的 snake_case 字符串）。**文件存在才播，缺失则静默跳过**（`VfxSpawner:Spawn` 缓存缺失结果，不重复 Load）。示例：技能 `player_default` → 释放 `skill_player_default_cast`、命中 `skill_player_default_hit`。

| 槽位 | id 格式 | 触发时机 | 锚点 |
|---|---|---|---|
| 释放 | `skill_<skillId>_cast` | `CombatManager:UseSkill` 入口（弹射物+非弹射物） | `source.position` |
| 命中 | `skill_<skillId>_hit` | `CombatManager:ResolveHit`（非弹射物延迟 + 弹射物 collider 共用） | `target.position` |
| 飞行 | （抛射物 prefab 自身） | 抛射物飞行全程 | 抛射物 transform |

- 特效生命周期由 prefab 自管：ParticleSystem 设 `Stop Action = Destroy`，或 Animator 一次性播完自毁。
- 战斗线世界 Y = `-21`（`ProjectileSpawner.projectileY` / `VfxSpawner.effectY`）。

## 4. 动画参数约定
| 角色 | 参数名 | 类型 | 取值 |
|---|---|---|---|
| Player | `PlayerState` | int | `EntityState` |
| Player | `PlayerSubstate` | int | `PlayerState=0` 时为 `0`；`PlayerState=4` 时为当前技能类型 `SkillType`（`Normal=1 / Combat=2 / Signature=3`）；`PlayerState=3` 时为具体控制效果类型（如 `Stun=1`） |
| Enemy | `States` | int | `enemyInstance.state` |

`EntityState`（`Gameplay/Entities/EntityState.lua`）：
| 名 | 值 |
|---|---|
| Idling | 0 |
| Walking | 1 |
| Dead | 2 |
| ExlusiveEffect | 3 | 无法行动状态，配合 `PlayerSubstate` 区分具体类型 |
| Attacking | 4 |
| Hurt | 5 |

> `ExlusiveEffect` 是源码原拼写，保持原样，不要"修正"。

## 5. 技能钩子与子类

复杂技能不再完全依赖配置表，通过 `Skill` 子类实现自定义逻辑。

`Skill` 基类提供两个钩子：

| 钩子 | 触发点 | 用途 |
|---|---|---|
| `OnCast(source, target?)` | `CombatManager:UseSkill` 释放技能后 | 给自己上 buff、AOE 效果等 |
| `OnHit(source, target, ctx)` | `CombatManager:ResolveHit` 命中后 | 给目标上 debuff、吸血等 |

注册方式：`Skills.lua` 先用配置表生成基础 `Skill` 实例，再用 `OverrideSkill(id, SubClass)` 替换为子类实例：

```lua
-- 玩家技能
OverrideSkill("player_slash", RendingSlash)
OverrideSkill("player_ranged", ThunderArrow)
OverrideSkill("player_signature", HolyShield)
-- 敌人/Boss 技能
OverrideSkill("enemy_curse", CurseTouch)
OverrideSkill("boss_1_breath", SludgeBreath)
```

`player_default` 无子类（纯回蓝兜底），使用基类 `Skill` 的空钩子。

**攻击距离层级**：掌控技(3) <= 普通技(5) = 主动技(5)。普通技发射弹射物（投掷刃），命中由弹射物 collider 裁决；主动技为非弹射物远距离近身攻击，走 `delay` 定时结算（天降剑/陨星等，cast 后 delay 秒对所有射程内目标同时命中）；掌控技无弹射物。敌人技能为真近战(range 1-1.5)；Boss 技能含弹射物远程(range 5)与非弹射物远程(range 4-5)两类。

**技能** `Skills`（`Gameplay/Skills/Skills.lua`）：

**玩家普通技**（回蓝兜底，cooldown < duration 保证可用）：
| id | 名称 | 效果 | unlockLevel |
|---|---|---|---|
| player_default | 火球 | 无特殊效果，纯回蓝 | 1 |
| player_slash | 碎甲球 | 命中降防 30%/3s | 5 |
| player_combo | 毒液球 | 命中上毒 10% atk DPS/5s | 15 |

**玩家主动技**（消耗法力，主力输出）：
| id | 名称 | 效果 | unlockLevel |
|---|---|---|---|
| player_ranged | 剑雨 | 自身加攻 50%/5s，命中眩晕 1s | 1 |
| player_pierce | 陨星 | 命中降攻 30%/4s | 8 |
| player_poison_fog | 毒雾 | 命中上毒 60% atk DPS/5s，AOE 范围敌人同时中毒 | 20 |

**玩家掌控技**（无消耗，冷却驱动，控制/辅助）：
| id | 名称 | 效果 | unlockLevel |
|---|---|---|---|
| player_signature | 天盾 | 自身套盾 2x atk/15s，无伤害 | 1 |
| player_frost | 霜坠 | 命中降速 50%/5s，击退 | 12 |
| player_storm | 剑暴 | 自身加攻 80%/8s，命中眩晕 2s，强击退 | 25 |

**敌人/侍从/Boss 技能**（unlockLevel 不适用）：
| id | 名称 | 效果 |
|---|---|---|
| enemy_default | 敌人普攻 | 无 |
| enemy_bite | 撕咬 | 快速低伤 |
| enemy_curse | 诅咒利爪 | 命中降攻 30%/3s |
| enemy_enrage | 狂暴 | 自身加攻 50%/10s |
| enemy_poison | 毒牙 | 命中上毒 15% atk DPS/4s |
| servant_slash | 侍从·挥砍 | 无 |
| servant_pierce | 侍从·突刺 | 无 |
| servant_smash | 侍从·重击 | 无 |
| boss_1_breath | 巨龙吐息 | 命中上毒 15% atk DPS/5s |
| boss_2_bolt | 深渊箭 | 命中眩晕 1.5s |
| boss_3_smash | 熔岩重击 | 命中降防 30%/4s |
| boss_4_dark | 暗影箭 | 命中降速 40%/4s |
| boss_5_flame | 烈焰吐息 | 自身加攻 50%/10s |
| boss_6_fury | 毁灭冲击 | 命中眩晕 2s |

技能 id 为 snake_case 字符串，与其它注册表的小写 id 约定一致。

### 5.1 技能解锁与槽位选择

玩家技能通过 `unlockLevel` 字段控制解锁等级，由 `SkillUnlockManager`（`Gameplay/Skills/SkillUnlockManager.lua`）管理：
- 解锁状态是玩家等级的派生值，不单独存档；读档时根据当前等级重新计算。
- `Player:GetUnlockedSkills(skillType)` 返回已解锁的指定类型技能列表。
- `Player:EquipSkill(slotType, index, skill)` 装备/卸下技能到指定槽位。

**槽位结构**（分类型，`Player:SetSkills` / `EquipSkill`）：

| 槽位 | slotType | 数量 | 说明 |
|---|---|---|---|
| Normal | `normal` | 1 | 普通技（回蓝兜底，可附带小 debuff），cooldown < duration 保证可用 |
| Combat | `combat` | 1 | 主动技（消耗法力，主力输出），cooldown >= duration |
| Signature | `signature` | 1-3 | 掌控技（无消耗，控制/辅助），槽位数量随等级解锁 |

**Signature 槽位解锁阈值**（`Config/SkillSlotConfig.lua`）：

| 等级 | 槽位数 |
|---|---|
| 1 | 1 |
| 10 | 2 |
| 25 | 3 |

`SkillSave` 存档时保存装备的技能 ID，读档时校验是否已解锁，未解锁则回落到默认技能。

**抛射物** `Projectiles`（`Gameplay/Projectiles/Projectiles.lua`）：`default`（speed 12）
**敌人** `Enemies`（`Gameplay/Entities/Enemies.lua`）：
| 分类 | id | 技能 | 定位 |
|---|---|---|---|
| 普通 | dino | enemy_default | 炮灰，低攻低血 |
| 普通 | dog | enemy_default | 肉盾，高防低攻 |
| 普通 | opossum | enemy_bite | 快速脆皮（负鼠） |
| 精英 | bat | enemy_poison | 上毒（蝙蝠） |
| 精英 | bear | enemy_enrage | 自身加攻（熊狂暴） |
| 精英 | eagle | enemy_curse | 降攻 debuff（鹰） |
**侍从** `Servants`（`Gameplay/Entities/Servants/Servants.lua`）：`guard`、`ranger`、`crusher`

### 5.2 难度梯度设计

**梯度链**：普通小怪 → 精英小怪 → 入口 Boss → 章节深处 Boss。每关 5 波，前 2-3 波为普通怪，后 2 波引入精英怪，最后打 Boss。

**敌人类型引入顺序**（`Waves.csv` enemyPool 逐步扩展）：
| 章节 | 引入敌人 | 池子示例 |
|---|---|---|
| 1 | dino;dog | 基础怪，难度 1-4 |
| 2 | +opossum;bat | 快速/上毒 |
| 3 | +bear | 精英自buff |
| 4 | +eagle | 降攻 debuff |
| 5-10 | 全怪混池 | 最高难度 37-41 |

**场景-敌人对应表**（sprite 来源驱动）：
| 场景 | 可用敌人 | 章节 |
|---|---|---|
| plains | dino;dog;opossum;bear;eagle | 1,4,7,10 |
| tropical | dino;opossum;bat;eagle | 2,5,8 |
| gold_mine | dog;opossum;bat;bear | 3,6,9 |

**难度缩放公式**：`EnemySpawner` 用 `level = max(difficulty + random(-3,3), 1)`，`ApplyLevelToAttributes` 乘以 `2*level - 1`（速度除外）。Ch1 起始 difficulty=1 保证 1x 基础属性，不劝退新人。

**Boss 数值归一化**：入口 Boss（boss_1/2/3）统一 80HP/4atk/2def，深处 Boss（boss_4/5/6）统一 150HP/7atk/3def。差异由 `recommendedLevel`（5→100）驱动 `2*level-1` 缩放保证单调递增。Boss 个性体现在 skill 子类和 crit/critDamage 上。

## 6. 事件约定（解耦接缝）
战斗层只发事件、不感知表现层；表现层 `VfxManager` 以 targetless 监听。
| EventType | 值 | 触发点 | target 键 | 载荷 |
|---|---|---|---|---|
| SkillCast | 11 | `CombatManager:UseSkill` | source | (source, skill) |
| SkillHit | 12 | `CombatManager:ResolveHit` | target | (target, source, skill) |
| EffectAdded | 13 | `EffectManager:AddEffect` | entity | (effect) |
| EffectRemoved | 14 | `EffectManager` 移除时 | entity | (effect) |
| EffectStackChanged | 15 | `EffectManager:AddEffect` 叠层 | entity | (effect, oldStacks, newStacks) |

其余事件见 `Event/EventType.lua`（EntityDied=9、EntityDamageTaken=10、SkillUnlocked=28 等）。

## 7. Buff 视觉约定

### 7.1 视觉映射配置
效果的染色/跟随特效由 `Config/EffectVisuals.lua` 按 `effect.id` 查找决定。未配置的 effect 仍会在 BuffBar 显示图标，但没有染色和跟随特效。

| 字段 | 含义 | 示例 |
|---|---|---|
| `tint` | SpriteRenderer 染色 | `CS.UnityEngine.Color(0.4, 1, 0.4, 1)` |
| `tintPriority` | 多染色冲突时的优先级，高覆盖低 | `1` |
| `vfx` | 跟随特效 prefab id | `buff_poison_loop` |

跟随特效路径：`Prefabs/Vfx/<vfx>`，生命周期由 `EntityVfxBinder` 在 effect 移除时销毁。

## 8. UI 资源约定

所有 UI 图标放在 `Textures/UI/` 下，按用途命名：

| 用途 | 路径 |
|---|---|
| 开始战斗（idle 状态） | `Textures/UI/AttackStart` |
| Normal 技能图标 | `Textures/UI/SkillType_Normal` |
| Combat 技能图标 | `Textures/UI/SkillType_Combat` |
| Signature 技能图标 | `Textures/UI/SkillType_Signature` |

攻击按钮图标规则：
- 玩家 idle 时显示 `AttackStart`（开始战斗图标）。
- 释放中（`activeSkill` 非 nil）显示当前技能类型图标，不随 `PickNext` 变化；同时 `Cooldown` 圆盘按持续时间倒计时（full → empty）。
- 等待中（非 idle 且无 `activeSkill`）显示锁定的下一个技能类型图标（释放结束时一次性决定，锁定直到开始释放）。
- 未配置图标时保持原样。

### 8.2 AutoBattle 按钮层级

`AutoBattleButton` 自身是 `Button`（贴图已在 prefab 里配好，不需要额外 Resources 加载）。

必须包含一个名为 `Image` 的子物体：

- `Image`：`UnityEngine.UI.Image` + `RectTransform`，贴图已在 prefab 中配置。
- 开启自动战斗时，`AutoBattleUI` 会每帧旋转该子物体（顺时针 `180°/秒`）。
- 关闭自动战斗时停止旋转。

### 8.3 AttackButton 层级

`AttackButton` 自身只挂 `Button` + `LuaBehaviour`（`AttackUI.lua.txt`），**真实的技能图标不放在它自身的 `Image` 上**。

必须包含一个名为 `Panel` 的子物体：

- `Panel`：`UnityEngine.UI.Image`，负责显示当前技能/开始战斗图标。
- `AttackUI` 会修改 `Panel.Image.sprite` 与 `Panel.Image.color`（idle 时的发光呼吸效果）。
- 按钮本身的 `Image` 仅作为背景/底框使用，不参与图标切换。
- `Cooldown`：`UnityEngine.UI.Image`，`Type=Filled`、`FillMethod=Radial360`，技能释放期间按持续时间倒计时（full → empty），非释放状态隐藏（`fillAmount=0`）。

### 7.2 BuffIcon prefab 层级
`Prefabs/Buffs/BuffIcon` 必须包含以下子节点：

- `Icon`：`UnityEngine.UI.Image`，显示 buff 图标
- `StackText`：`TMPro.TMP_Text`，叠层数（大于 1 时显示）
- `Cooldown`：`UnityEngine.UI.Image`，`Type=Filled`、`FillMethod=Radial360`，用于倒计时转圈

### 7.3 挂点
- 玩家：`GameObject.Find("BuffBar")`（HUD 上的 `HorizontalLayoutGroup`）
- 敌人：`EnemyBase` prefab 下 `Canvas/BuffBar`（血条同级）
- 染色：`go:GetComponent(typeof(SpriteRenderer))`（角色本体）
- 跟随特效：`Instantiate(prefab, go.transform)` 作为子物体

### 8.4 SkillWindow 层级

`Tools/UI/生成 SkillUI` 会创建根 Canvas `SkillUI` 及两个子物体（`SkillButton` + `SkillWindow`），并生成 `Prefabs/SkillListItem` 预制体。节点命名严格对应 `SkillWindow.lua.txt` 里的 `transform:Find` 路径，改名会导致 Lua `SafeFind` 报错。

```
SkillUI (Canvas + CanvasScaler + GraphicRaycaster)
├ SkillButton          (Button + Image + LuaBehaviour，右上角入口)
│  └ Text              (TMP)
└ SkillWindow          (Canvas + GraphicRaycaster + LuaBehaviour)
   └ Background/WindowPanel
     ├ Title           (TMP)
     ├ CloseButton     (Button + Text)
     ├ SlotsPanel
     │  ├ LabelNormal / SlotNormal    (Button，子节点 Icon + Name)
     │  ├ LabelCombat / SlotCombat     (Button，子节点 Icon + Name)
     │  └ LabelSignature / SlotSignature1..3 (Button，子节点 Icon + Name)
     ├ DetailCard/Detail (TMP，显示选中技能详情)
     └ ScrollView/Viewport/Content  (列表容器)
```

槽位节点命名规则：单槽位类型（normal/combat）节点名无序号（`SlotNormal` / `SlotCombat`），多槽位类型（signature）带序号（`SlotSignature1..3`）。

**技能图标**（`Icon` 节点）：约定优先于注册——路径 `Textures/Skills/<skill.id>`（如 `Textures/Skills/player_default`）。`Resources.Load` 加载为 `Sprite`，设到 `Image.sprite`。文件不存在则 `Image.enabled = false` 静默隐藏，不报错。槽位和列表项均含 `Icon` 子节点（`Image`，占位）。

`Prefabs/SkillListItem` 预制体：自身带 `Button + Image + LayoutElement`，子节点：
- `Icon`：`Image`，技能图标（`Textures/Skills/<skill.id>`）
- `Name`：`TMP_Text`，技能名
- `Info`：`TMP_Text`，类型 + 解锁等级 + 装备状态

入口按钮共两处，均挂 `LuaBehaviour`（`SkillButton.lua.txt`）：
- `SkillUI/SkillButton`：根 Canvas 右上角。
- `UIRoot/Bottom/Group/SkillEnterButton`：底部按钮组内。

## 9. 高级战斗效果约定

### 9.1 击退（Knockback）

击退由 `Skills.csv` 里的 `knockbackSpeed` / `knockbackDistance` 配置，`CombatManager:ResolveHit` 在命中后调用 `target:ApplyKnockback(...)`。

击退方向由攻击者和受击者的位置自动决定：受击者在攻击者右侧则向左击退，反之向右。

### 9.2 晕眩（Stun）与 `PlayerSubstate`

`Entity.stunned` 为状态标记，由 `StunEffectDefinition` 在 `OnApply/OnExpire` 中同步。

- `Entity:CanMove()`：未晕眩时返回 `true`
- `Entity:CanCast()`：未晕眩且未死亡时返回 `true`
- `Player:TryCast()` / `Player:CanAttack()` 会检查 `CanCast()`
- `EnemyInstance:OnUpdate()` 晕眩时强制 `state = ExlusiveEffect`，停止移动与攻击
- `PlayerView` / `EnemyView` 检测到晕眩时切到 `ExlusiveEffect` 动画状态
- `PlayerView` 会把具体控制效果类型写入 `PlayerSubstate`，方便 Animator 做更细的分支

### 9.3 效果系统架构

效果拆成两层：

| 层 | 文件 | 职责 |
|---|---|---|
| **EffectDefinition** | `Gameplay/Effects/EffectDefinition.lua` 及子类 | 效果逻辑/原型，包含 `OnApply` / `OnUpdate` / `OnExpire` / 伤害 pipeline 钩子 |
| **EffectInstance** | `Gameplay/Effects/EffectInstance.lua` | 运行时实例数据：`target` / `source` / `duration` / `remaining` / `stacks` / `params` |

`EffectDefinition` 子类：

- `ShieldEffectDefinition`：护盾吸收伤害
- `PoisonEffectDefinition`：持续伤害
- `StatModEffectDefinition`：属性修正（FlatAdd 加法 / Multiply 乘法）
- `StunEffectDefinition`：晕眩控制
- `TriggerStatModEffectDefinition`：事件驱动属性修正（onHit/onHealed 叠层）

`EffectManager` 只管理 `EffectInstance` 生命周期，调用时把 `target` / `source` / `params` 传给 definition 钩子；`OnUpdate` 按 `stacks` 循环执行。definition **不接触 instance**。

创建实例：

```lua
local EffectTypes = require("Config.Generated.EffectTypes")
local instance = EffectFactory.CreateInstance(EffectTypes.stun, target, attacker, 2)
target:AddEffect(instance)
```

`CreateInstance` 只接受 `EffectTypes.xxx` 枚举。

**效果 params 速查**：

| EffectType | params | 说明 |
|---|---|---|
| shield | `{ amount = X }` | 护盾吸收量 |
| poison | `{ dps = X }` | 每秒伤害，可叠层最多 5 层 |
| stat_mod (FlatAdd) | `{ value = X }` | 正值加 buff，负值减 debuff |
| stat_mod (Multiply) | `{ value = X }` | -0.3 = 降 30%，+0.5 = 增 50% |
| stun | 无 | 纯控制 |

### 9.4 效果配置表

所有效果统一在 `Config/Excel/Effects.csv` 配置，生成到 `Assets/Resources/Lua/Config/Generated/Effects.lua.txt` 与 `EffectTypes.lua.txt`。

CSV 关键列：

| 列名 | 说明 |
|---|---|
| `id` | 效果唯一 id，也是 `EffectTypes` 枚举键 |
| `type` | 效果类型：`shield` / `poison` / `stat_mod` / `stun` / `trigger_stat_mod` |
| `substate` | `PlayerState=3` 时写入 `PlayerSubstate` 的值，`0` 表示不控制 |
| `stackable` | 是否可叠层 |
| `maxStacks` | 最大叠层 |
| `attribute` | `stat_mod` 专用，属性枚举名，如 `Speed` |
| `modifierType` | `stat_mod` 专用：`FlatAdd`（加法，正值 buff 负值 debuff）或 `Multiply`（乘法，-0.3 = 降 30%） |

配置表**不包含** `duration` / `amount` / `dps` / `value` 等运行时数值，这些在调用 `CreateInstance` 时通过 `params` 传入。

`Tools/ExcelToLua/generator.py` 会在 CSV→Lua 阶段把 `skillType` / `attribute` / `modifierType` 列的字符串枚举名解析成数字枚举值。

示例：

```csv
id,type,substate,stackable,maxStacks,attribute,modifierType
stun,stun,1,,,,
speed_up,stat_mod,0,,,Speed,FlatAdd
defense_down,stat_mod,0,,,Defense,Multiply
attack_down,stat_mod,0,,,Attack,Multiply
speed_down,stat_mod,0,,,Speed,Multiply
```
