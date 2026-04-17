using UnityEngine;
using Pixelplacement;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 startPos;

    void Awake()
    {
        Instance = this;
    }

    public void Shake(float duration, float strength)
    {
        startPos = transform.localPosition;

        int shakes = 8;
        float step = duration / shakes;

        for (int i = 0; i < shakes; i++)
        {
            float delay = step * i;

            Vector3 offset = new Vector3(
                Random.Range(-strength, strength),
                Random.Range(-strength, strength),
                0f
            );

            Tween.LocalPosition(
                transform,
                startPos + offset,
                step,
                delay
            );
        }

        Tween.LocalPosition(
            transform,
            startPos,
            step,
            duration
        );
    }
}
