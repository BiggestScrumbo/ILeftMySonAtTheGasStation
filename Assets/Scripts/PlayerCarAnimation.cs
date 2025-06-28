using UnityEngine;

public class PlayerCarAnimation : MonoBehaviour
{
    [System.Serializable]
    public class GearSpriteSet
    {
        public Sprite[] frames = new Sprite[2];
    }

    [Header("Gear-Based Sprites")]
    public GearSpriteSet[] gearSpriteSets = new GearSpriteSet[5]; // One set per gear
    
    private int currentFrame = 0;
    private SpriteRenderer spriteRenderer;
    private GameController gameController;
    private int lastGear = -1; // Track gear changes
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameController = FindObjectOfType<GameController>();
        
        // Initialize sprite sets if empty
        for (int i = 0; i < gearSpriteSets.Length; i++)
        {
            if (gearSpriteSets[i] == null)
                gearSpriteSets[i] = new GearSpriteSet();
        }
        
        // Set initial sprite from current gear
        UpdateSpriteSetForCurrentGear();
    }
    
    void Update()
    {
        // Check if gear changed
        if (gameController != null)
        {
            int currentGear = gameController.GetCurrentGear();
            if (currentGear != lastGear)
            {
                lastGear = currentGear;
                UpdateSpriteSetForCurrentGear();
            }
        }
    }
    
    void UpdateSpriteSetForCurrentGear()
    {
        int gear = gameController.GetCurrentGear();
        
        // Ensure gear is within valid range
        if (gear >= 0 && gear < gearSpriteSets.Length)
        {
            // Reset animation frame when switching gears
            currentFrame = 0;
            
            // Apply current frame of the new gear set
            if (spriteRenderer != null && 
                gearSpriteSets[gear].frames.Length > 0 && 
                gearSpriteSets[gear].frames[0] != null)
            {
                spriteRenderer.sprite = gearSpriteSets[gear].frames[0];
            }
        }
    }
    
    public void SwapFrame()
    {
        if (gameController == null || spriteRenderer == null)
            return;
        
        int gear = gameController.GetCurrentGear();
        
        // Validate gear and sprite set
        if (gear < 0 || gear >= gearSpriteSets.Length)
            return;
            
        GearSpriteSet currentSet = gearSpriteSets[gear];
        
        // Only swap if we have a valid set with at least 2 frames
        if (currentSet != null && currentSet.frames.Length >= 2 && 
            currentSet.frames[0] != null && currentSet.frames[1] != null)
        {
            // Swap to the next frame
            currentFrame = (currentFrame + 1) % 2;
            spriteRenderer.sprite = currentSet.frames[currentFrame];
        }
    }
}
