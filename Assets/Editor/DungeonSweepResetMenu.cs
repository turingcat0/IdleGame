using UnityEngine;
using UnityEditor;
using XLua;

/// <summary>
/// 编辑器菜单：重置副本每日扫荡次数（仅运行模式可用）
/// </summary>
public class DungeonSweepResetMenu
{
    [MenuItem("Tools/Reset Dungeon Sweep Count")]
    static void ResetDungeonSweep()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Reset Dungeon Sweep", "只能在运行模式下重置扫荡次数。", "OK");
            return;
        }

        var luaEnv = LuaBehaviour.luaEnv;
        if (luaEnv == null)
        {
            Debug.LogError("[DungeonSweepResetMenu] LuaEnv is null");
            return;
        }

        luaEnv.DoString(
            "local gm = require('GameManager')\n" +
            "if gm and gm.dungeonManager then\n" +
            "    gm.dungeonManager:ResetDailySweepCount()\n" +
            "    print('[DungeonSweepResetMenu] 扫荡次数已重置')\n" +
            "else\n" +
            "    print('[DungeonSweepResetMenu] dungeonManager 未初始化')\n" +
            "end",
            "DungeonSweepResetMenu");

        Debug.Log("[DungeonSweepResetMenu] 扫荡次数重置完成");
    }
}
