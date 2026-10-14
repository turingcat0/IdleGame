#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using xluaDemo;

namespace xluaDemo.Editor
{
    public static class DeleteSaveMenu
    {
        [MenuItem("Tools/Delete Save")]
        public static void DeleteSave()
        {
            if (SaveManager.HasSave())
            {
                SaveManager.Delete();
                Debug.Log("[DeleteSaveMenu] Save deleted from: " + Application.persistentDataPath);
            }
            else
            {
                Debug.Log("[DeleteSaveMenu] No save file to delete at: " + Application.persistentDataPath);
            }
        }
    }
}
#endif
