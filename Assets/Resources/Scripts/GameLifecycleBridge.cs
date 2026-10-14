using UnityEngine;

namespace xluaDemo
{
    /// <summary>
    /// C# bridge that invokes a Lua callback when the application is quitting.
    /// Used to trigger a save before the Lua VM is torn down.
    /// Accessed from Lua via CS.xluaDemo.GameLifecycleBridge.
    /// </summary>
    public class GameLifecycleBridge : MonoBehaviour
    {
        private XLua.LuaFunction onQuitCallback;

        /// <summary>
        /// Set the Lua function to be called on application quit.
        /// </summary>
        /// <param name="callback">Lua callback.</param>
        public void SetOnQuitCallback(XLua.LuaFunction callback)
        {
            onQuitCallback = callback;
        }

        private void OnApplicationQuit()
        {
            onQuitCallback?.Call();
        }
    }
}
