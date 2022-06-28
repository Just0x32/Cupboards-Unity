using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipOrPlaceController : MonoBehaviour
{
    public static GameboardController GameboardController { get; set; }
    public ChipOrPlaceType Type { get; private set; }
    public int Index { get; private set; }
    private bool isInitialized = false;

    public enum ChipOrPlaceType
    {
        Unknown,
        Place,
        Chip
    }

    public void Initialize(ChipOrPlaceType type, int index)
    {
        if (!isInitialized)
        {
            Type = type;
            Index = index;
            isInitialized = true;
        }
    }

    public void OnMouseDown()
    {
        if (Type == ChipOrPlaceType.Place)
            GameboardController.OnPlaceClick(Index);
        else if (Type == ChipOrPlaceType.Chip)
            GameboardController.OnChipClick(Index);
    }
}
