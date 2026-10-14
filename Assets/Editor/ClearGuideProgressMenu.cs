using UnityEditor;
using UnityEngine;

namespace xluaDemo.Editor
{
    public static class ClearGuideProgressMenu
    {
        [MenuItem("Tools/清除新手引导进度")]
        public static void ClearGuideProgress()
        {
            xluaDemo.SaveManager.ClearGuideProgress();
            Debug.Log("[ClearGuideProgress] 已清除 save.json 中的 guide 进度，其它存档数据保留。");
        }
    }
}
