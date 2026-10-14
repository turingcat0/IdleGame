import csv
import os
import sys

# 配置路径
PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
EXCEL_DIR = os.path.join(PROJECT_ROOT, "Config", "Excel")
OUTPUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "Lua", "Config", "Generated")

# 枚举列映射：CSV 字符串 -> Lua 数字枚举值
ENUM_COLUMNS = {
    "skillType": {
        "Normal": 1,
        "Combat": 2,
        "Signature": 3,
    },
    "attribute": {
        "MaxHealth": 1,
        "MaxMana": 2,
        "Attack": 3,
        "Defense": 4,
        "CritChance": 5,
        "CritDamage": 6,
        "Speed": 7,
        "AttackRange": 8,
        "AttackMultiplier": 9,
        "ManaCost": 10,
        "KnockbackSpeed": 11,
        "KnockbackDistance": 12,
    },
    "modifierType": {
        "FlatAdd": 1,
        "Multiply": 2,
    },
}


def parse_value(raw: str, column: str = None):
    """把 CSV 单元格字符串转成 Lua 字面量对应的 Python 值"""
    if raw is None:
        return None
    raw = raw.strip()
    if raw == "":
        return None

    # 枚举列：先查映射
    if column and column in ENUM_COLUMNS:
        enum_map = ENUM_COLUMNS[column]
        if raw in enum_map:
            return enum_map[raw]
        # 也支持直接填数字
        try:
            return int(raw)
        except ValueError:
            pass
        raise ValueError(f"未知枚举值 '{raw}' for column '{column}'. 可用: {list(enum_map.keys())}")

    lower = raw.lower()
    if lower == "true":
        return True
    if lower == "false":
        return False
    # 尝试数字
    try:
        if "." in raw:
            return float(raw)
        return int(raw)
    except ValueError:
        pass
    return raw


def to_lua_literal(value):
    """把 Python 值转成 Lua 字面量字符串"""
    if value is None:
        return "nil"
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (int, float)):
        return str(value)
    # 字符串：转义反斜杠和双引号
    s = str(value).replace("\\", "\\\\").replace('"', '\\"')
    return f'"{s}"'


def read_csv(path: str):
    """读取 CSV，返回 (表名, [{列名: 值}])"""
    name = os.path.splitext(os.path.basename(path))[0]
    rows = []
    with open(path, "r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            parsed = {k.strip(): parse_value(v, k.strip()) for k, v in row.items() if k and k.strip()}
            rows.append(parsed)
    return name, rows


def generate_lua(name: str, rows: list):
    """生成 Lua 文件内容"""
    if not rows:
        return f"local {name} = {{}}\nreturn {name}\n"

    headers = list(rows[0].keys())
    # id 列放前面，其余按表头顺序
    if "id" in headers:
        headers.remove("id")
        headers.insert(0, "id")

    lines = [f"local {name} = {{", ""]
    for row in rows:
        row_id = row.get("id")
        if row_id is None:
            continue
        lines.append(f"    [\"{row_id}\"] = {{")
        for key in headers:
            if key == "id":
                continue
            value = row.get(key)
            if value is None:
                continue
            lines.append(f"        {key} = {to_lua_literal(value)},")
        lines.append("    },")
    lines.append("}")
    lines.append("")
    lines.append(f"return {name}")
    lines.append("")
    return "\n".join(lines)


def generate_enum_lua(enum_name: str, rows: list):
    """根据 id 列生成枚举 Lua 文件"""
    lines = [f"local {enum_name} = {{", ""]
    index = 1
    for row in rows:
        row_id = row.get("id")
        if row_id is None:
            continue
        lines.append(f"    {row_id} = {index},")
        lines.append(f"    [{index}] = \"{row_id}\",")
        index += 1
    lines.append("}")
    lines.append("")
    lines.append(f"return {enum_name}")
    lines.append("")
    return "\n".join(lines)


def main():
    if not os.path.exists(EXCEL_DIR):
        print(f"Excel 目录不存在: {EXCEL_DIR}")
        sys.exit(1)

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    generated = []
    for filename in sorted(os.listdir(EXCEL_DIR)):
        if not filename.lower().endswith(".csv"):
            continue
        csv_path = os.path.join(EXCEL_DIR, filename)
        name, rows = read_csv(csv_path)
        lua_content = generate_lua(name, rows)
        out_path = os.path.join(OUTPUT_DIR, f"{name}.lua.txt")
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(lua_content)
        generated.append(out_path)
        print(f"Generated: {out_path}")

        # Effects.csv 额外生成 EffectTypes 枚举
        if name == "Effects":
            enum_name = "EffectTypes"
            enum_content = generate_enum_lua(enum_name, rows)
            enum_path = os.path.join(OUTPUT_DIR, f"{enum_name}.lua.txt")
            with open(enum_path, "w", encoding="utf-8") as f:
                f.write(enum_content)
            generated.append(enum_path)
            print(f"Generated: {enum_path}")

    if not generated:
        print(f"在 {EXCEL_DIR} 中没有找到 CSV 文件")
    else:
        print(f"\n共生成 {len(generated)} 个 Lua 配置文件")


if __name__ == "__main__":
    main()
