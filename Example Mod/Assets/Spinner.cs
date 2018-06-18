using UnityEngine;
using ModTool.Interface;

public class Spinner : ModBehaviour
{
    public Vector3 speed;

    void Update()
    {
        transform.eulerAngles += speed * Time.deltaTime;
    }
}
