#!/usr/bin/env python3
"""Generate 10-chapter config: Chapters, Stages, Waves CSVs."""
import os, csv

EXCEL = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Config", "Excel")

# ---- Chapters ----
chapters = []
themes = [
    ("荒野",       "plains"),
    ("热带平原",   "tropical"),
    ("地牢",       "gold_mine"),
    ("草原",       "plains"),
    ("金色平原",   "tropical"),
    ("深渊",       "gold_mine"),
    ("森林",       "plains"),
    ("热带高原",   "tropical"),
    ("暗影巢穴",   "gold_mine"),
    ("繁花森林",   "plains"),
]
for i, (name, scene) in enumerate(themes):
    cid = f"chapter_{i+1}"
    unlock = f"chapter_{i}" if i > 0 else ""
    chapters.append({"id": cid, "name": f"第{i+1}章:{name}", "unlockCondition": unlock})

with open(os.path.join(EXCEL, "Chapters.csv"), "w", encoding="utf-8-sig", newline="") as f:
    w = csv.DictWriter(f, fieldnames=["id", "name", "unlockCondition"])
    w.writeheader()
    w.writerows(chapters)

# ---- Stages ----
stages = []
# Bosses assigned by scene to match sprite style:
# plains: Frog(boss_3) + Bunny(boss_4); tropical: Vulture(boss_2) + Pig(boss_6); gold_mine: Slimer(boss_1) + Beetle(boss_5)
boss_by_scene = {
    "plains":    ["boss_3", "boss_4"],
    "tropical":  ["boss_2", "boss_6"],
    "gold_mine": ["boss_1", "boss_5"],
}
# exp reward scales: base * chapter^1.4 roughly
exp_base = [100, 300, 800, 1500, 2500, 4000, 6000, 8500, 11500, 15000]
crystal_base = [20, 50, 120, 200, 300, 400, 550, 700, 900, 1100]
coin_base = [20, 50, 120, 200, 300, 400, 550, 700, 900, 1100]

for ci, (name, scene) in enumerate(themes):
    for si in range(2):
        sid = f"stage_{ci+1}_{si+1}"
        stage_name = f"{name}{'入口' if si==0 else '深处'}"
        rec_level = ci * 10 + 5 + si * 5
        boss = boss_by_scene[scene][si]
        exp = exp_base[ci] + si * exp_base[ci] // 2
        crystal = crystal_base[ci] + si * crystal_base[ci] // 2
        coin = coin_base[ci] + si * coin_base[ci] // 2
        stages.append({
            "id": sid, "chapterId": f"chapter_{ci+1}", "name": stage_name,
            "recommendedLevel": rec_level, "bossId": boss,
            "expReward": exp, "crystalReward": crystal, "coinReward": coin,
            "type": "main", "dungeonGroup": "", "unlockLevel": 0, "sceneId": scene,
        })

# Dungeons (keep existing 4, fix sceneId)
dungeon_defs = [
    ("dungeon_exp_1", "chapter_1", "经验洞窟·初", 8,  "boss_1", 300, 10, 10, "exp", 5),
    ("dungeon_exp_2", "chapter_2", "经验洞窟·深", 20, "boss_3", 800, 20, 20, "exp", 15),
    ("dungeon_gold_1","chapter_1", "金币矿洞·初", 12, "boss_2", 100, 10, 300,"gold",10),
    ("dungeon_gold_2","chapter_3", "金币矿洞·深", 30, "boss_5", 200, 30, 800,"gold",25),
]
for did, ch, name, lvl, boss, exp, crystal, coin, grp, unlock in dungeon_defs:
    stages.append({
        "id": did, "chapterId": ch, "name": name,
        "recommendedLevel": lvl, "bossId": boss,
        "expReward": exp, "crystalReward": crystal, "coinReward": coin,
        "type": "dungeon", "dungeonGroup": grp, "unlockLevel": unlock, "sceneId": "gold_mine",
    })

with open(os.path.join(EXCEL, "Stages.csv"), "w", encoding="utf-8-sig", newline="") as f:
    w = csv.DictWriter(f, fieldnames=["id","chapterId","name","recommendedLevel","bossId",
        "expReward","crystalReward","coinReward","type","dungeonGroup","unlockLevel","sceneId"])
    w.writeheader()
    w.writerows(stages)

# ---- Waves ----
waves = []
# count pattern per chapter tier
# ch1-3: 3,3,4,4,5  ch4-6: 4,4,5,5,6  ch7-10: 5,5,6,6,7
def counts_for_chapter(ci):
    if ci < 3:  return [3,3,4,4,5]
    if ci < 6:  return [4,4,5,5,6]
    return [5,5,6,6,7]

pools = ["dino", "dino;dog", "dino;dog", "dino;dog", "dog"]

for ci, (name, scene) in enumerate(themes):
    base_diff = ci * 6 + 2  # ch1=2, ch2=8, ch3=14, ...
    cluster = 2 if ci < 3 else 3
    cnts = counts_for_chapter(ci)
    for si in range(2):
        stage_base = base_diff + si * 3
        sid = f"stage_{ci+1}_{si+1}"
        for wi in range(5):
            wid = f"wave_{ci+1}_{si+1}_{wi+1}"
            diff = stage_base + wi * 2
            waves.append({
                "id": wid, "stageId": sid, "waveIndex": wi+1,
                "enemyPool": pools[wi], "count": cnts[wi],
                "difficulty": diff, "maxCluster": cluster, "spawnInterval": 1.0,
            })

# Dungeon waves (keep existing patterns)
dungeon_waves = [
    # dungeon_exp_1
    ("wave_d_e1_1","dungeon_exp_1",1,"dino;dog",5,5,2,0.8),
    ("wave_d_e1_2","dungeon_exp_1",2,"dino;dog",5,6,2,0.8),
    ("wave_d_e1_3","dungeon_exp_1",3,"dog",6,7,2,0.8),
    # dungeon_exp_2
    ("wave_d_e2_1","dungeon_exp_2",1,"dino;dog",6,18,2,0.8),
    ("wave_d_e2_2","dungeon_exp_2",2,"dino;dog",6,20,2,0.8),
    ("wave_d_e2_3","dungeon_exp_2",3,"dog",7,22,2,0.8),
    # dungeon_gold_1
    ("wave_d_g1_1","dungeon_gold_1",1,"dino;dog",5,10,2,0.8),
    ("wave_d_g1_2","dungeon_gold_1",2,"dino;dog",5,12,2,0.8),
    ("wave_d_g1_3","dungeon_gold_1",3,"dog",6,14,2,0.8),
    # dungeon_gold_2
    ("wave_d_g2_1","dungeon_gold_2",1,"dino;dog",6,28,2,0.8),
    ("wave_d_g2_2","dungeon_gold_2",2,"dino;dog",6,30,2,0.8),
    ("wave_d_g2_3","dungeon_gold_2",3,"dog",7,32,2,0.8),
]
for wid, sid, wi, pool, cnt, diff, mc, interval in dungeon_waves:
    waves.append({"id": wid, "stageId": sid, "waveIndex": wi,
        "enemyPool": pool, "count": cnt,
        "difficulty": diff, "maxCluster": mc, "spawnInterval": interval})

with open(os.path.join(EXCEL, "Waves.csv"), "w", encoding="utf-8-sig", newline="") as f:
    w = csv.DictWriter(f, fieldnames=["id","stageId","waveIndex","enemyPool","count","difficulty","maxCluster","spawnInterval"])
    w.writeheader()
    w.writerows(waves)

print(f"Generated: {len(chapters)} chapters, {len(stages)} stages, {len(waves)} waves")
