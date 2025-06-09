using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Transform target2;
    public Transform target3;
    public float smoothing;
    public Vector3 offset;
    public Vector2 maxPos;
    public Vector2 minPos;

    private void FixedUpdate()
    {
        if (transform.position.x != target2.position.x)
        {
            float targetX = target2.position.x;
            float targetY = transform.position.y; // ใช้ค่า y ของกล้องเอง
            float targetZ = transform.position.z; // ใช้ค่า z ของกล้องเอง

            targetX = Mathf.Clamp(targetX, minPos.x, maxPos.x);

            Vector3 targetPos = new Vector3(targetX, targetY, targetZ);
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothing);
        }
    }
}
