import os
import re
import csv
from pathlib import Path

root = Path('Assets')
config_root = Path('Config')
chars = set()

# 常用符号
common_symbols = (
    ' \n\t'
    '.,;:!?()[]{}<>\\/|@#$%^&*-_=+'
    '"\'' + '`~'
    '·，。、；：！？（）【】《》“”‘’'
    '—…～'
    '0123456789'
    'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
)
for c in common_symbols:
    chars.add(c)

def add_chars_from_text(text):
    for c in text:
        chars.add(c)

def decode_text(s):
    s = s.strip()
    if s.startswith('"') and s.endswith('"'):
        s = s[1:-1]
    elif s.startswith("'") and s.endswith("'"):
        s = s[1:-1]

    def unescape(match):
        return chr(int(match.group(1), 16))
    s = re.sub(r'\\u([0-9a-fA-F]{4})', unescape, s)
    s = s.replace('\\"', '"')
    s = s.replace('\\\\', '\\')
    return s

# 1. 扫描 prefab / scene 中 TMP 文本
pattern = re.compile(r'^\s*m_text:\s*(.+)$', re.MULTILINE)
for ext in ('*.prefab', '*.unity'):
    for p in root.rglob(ext):
        try:
            text = p.read_text(encoding='utf-8', errors='ignore')
        except Exception as e:
            print('读取失败 {}: {}'.format(p, e))
            continue
        for match in pattern.finditer(text):
            value = match.group(1).strip()
            decoded = decode_text(value)
            add_chars_from_text(decoded)

# 2. 扫描 Lua 脚本中所有中文字符（动态设置的文字、配置表等）
for p in root.rglob('*.lua.txt'):
    try:
        text = p.read_text(encoding='utf-8', errors='ignore')
    except Exception:
        continue
    add_chars_from_text(text)

# 3. 扫描 CSV 配置表（物品名、技能名、敌人名等）
for p in config_root.rglob('*.csv'):
    try:
        with p.open('r', encoding='utf-8', errors='ignore', newline='') as f:
            reader = csv.reader(f)
            for row in reader:
                for cell in row:
                    add_chars_from_text(cell)
    except Exception as e:
        print('读取 CSV 失败 {}: {}'.format(p, e))

# 4. 扫描其他可能的配置文件（json、xml、txt）
for ext in ('*.json', '*.xml', '*.txt'):
    for p in root.rglob(ext):
        # 跳过已经专门处理的字体字符文件
        if p.name == 'AllUI_Chars.txt':
            continue
        try:
            text = p.read_text(encoding='utf-8', errors='ignore')
        except Exception:
            continue
        add_chars_from_text(text)

# 分离中文字符和其他字符
chinese = sorted([c for c in chars if '\u4e00' <= c <= '\u9fff'])
others = sorted([c for c in chars if not ('\u4e00' <= c <= '\u9fff')])

result = ''.join(others) + ''.join(chinese)

out_path = Path('Assets/Fonts/AllUI_Chars.txt')
out_path.write_text(result, encoding='utf-8')

try:
    print('扫描到 {} 个唯一字符'.format(len(chars)))
    print('中文字符数: {}'.format(len(chinese)))
    print('其他字符数: {}'.format(len(others)))
    print('已写入: {}'.format(out_path))
    print('前200字符: {}'.format(result[:200]))
except UnicodeEncodeError:
    print('Scan complete. Total chars: {}, Chinese: {}, Others: {}'.format(len(chars), len(chinese), len(others)))
    print('Output: {}'.format(out_path))
