using UnityEngine;

namespace Story
{
    // Attach to each gameplay scene root. Marks which stage number this scene represents.
    // stageNumber: 1..3. Used to unlock next stage in SaveManager when victory occurs.
    public class StageMeta : MonoBehaviour
    {
        [Range(1,3)] public int stageNumber = 1;
    }
}
