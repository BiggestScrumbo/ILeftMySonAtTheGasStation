using UnityEngine;

public class ObstacleAnimation : MonoBehaviour
{
    public Sprite[] frames = new Sprite[2]; // The two animation frames
    private int currentFrame = 0;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Store the initial sprite as frame 0 if not already set
        if (spriteRenderer != null && spriteRenderer.sprite != null && frames[0] == null)
        {
            frames[0] = spriteRenderer.sprite;
        }
    }

    public void SwapFrame()
    {
        // Don't try to animate if we don't have enough frames
        if (frames == null || frames.Length < 2 || spriteRenderer == null)
            return;

        // Only swap if both frames are assigned
        if (frames[0] != null && frames[1] != null)
        {
            // Swap to the next frame
            currentFrame = (currentFrame + 1) % frames.Length;
            spriteRenderer.sprite = frames[currentFrame];
        }
    }
}
