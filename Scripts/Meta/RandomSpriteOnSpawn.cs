using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RandomSpriteOnSpawn : MonoBehaviour
{
    [Header("Sprites Pool")]
    [SerializeField] private Sprite[] sprites;

    [Header("Options")]
    [SerializeField] private bool randomOnAwake = true;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (randomOnAwake)
            ApplyRandomSprite();
    }

    public void ApplyRandomSprite()
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"{name}: Sprite list is empty!");
            return;
        }

        int index = Random.Range(0, sprites.Length);
        spriteRenderer.sprite = sprites[index];
    }
}
