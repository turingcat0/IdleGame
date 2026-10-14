using UnityEngine;

/// <summary>
/// 挂载到一次性特效 prefab 上，在指定延迟后销毁自身。
/// delay 建议填对应动画 Clip 的时长。
/// </summary>
public class VfxAutoDestroy : MonoBehaviour
{
    [Tooltip("特效播放几秒后销毁，填动画 Clip 的时长")]
    public float delay = 1.0f;

    void Start()
    {
        Destroy(gameObject, delay);
    }
}
