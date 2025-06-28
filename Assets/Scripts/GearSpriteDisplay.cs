using UnityEngine;
using UnityEngine.UI;

public class GearSpriteDisplay : MonoBehaviour
{
    public Sprite[] gearSprites; // Array of gear sprites (one for each gear)
    private Image gearImage;
    
    void Start()
    {
        gearImage = GetComponent<Image>();
        
        if (gearImage == null)
        {
            Debug.LogError("GearSpriteDisplay requires an Image component");
        }
    }

    public void UpdateGearSprite(int gearIndex)
    {
        // Check if we have valid sprites and image component
        if (gearImage == null || gearSprites == null || gearSprites.Length == 0)
            return;
            
        // Clamp index to valid range
        int index = Mathf.Clamp(gearIndex, 0, gearSprites.Length - 1);
        
        // Set the sprite
        gearImage.sprite = gearSprites[index];
    }
}
