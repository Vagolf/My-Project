using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothing;
    public Vector3 offset;
    public Vector2 maxPos;
    public Vector2 minPos;

    private void FixedUpdate()
    {
        if (transform.position.x != target.position.x)
        {
            float targetX = Mathf.Clamp(target.position.x, minPos.x, maxPos.x);
            Vector3 targetPos = transform.position;
            targetPos.x = targetX;
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothing);
        }
    }
}
