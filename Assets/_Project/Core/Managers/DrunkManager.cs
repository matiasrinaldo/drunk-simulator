using System;
using UnityEngine;

public class DrunkManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int maxLevel = 8;

    public int AlcoholLevel { get; private set; } = 0;
    public int MaxLevel => maxLevel;

    // 0-1 for scaling effects continuously across levels
    public float NormalizedLevel => maxLevel > 0 ? (float)AlcoholLevel / maxLevel : 0f;

    public event Action<int> OnAlcoholLevelChanged;

    public void AddBeer()
    {
        AlcoholLevel = Mathf.Min(AlcoholLevel + 1, maxLevel);
        OnAlcoholLevelChanged?.Invoke(AlcoholLevel);
    }

    public void ResetLevel()
    {
        AlcoholLevel = 0;
        OnAlcoholLevelChanged?.Invoke(AlcoholLevel);
    }
}
