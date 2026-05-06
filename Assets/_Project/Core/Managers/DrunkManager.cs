using System;
using UnityEngine;

public class DrunkManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int maxLevel = 24;
    [SerializeField] private float effectExponent = 1.6f;

    [Header("Debug")]
    [SerializeField] private KeyCode debugAddAlcoholKey = KeyCode.G;
    [SerializeField] private int debugAddAlcoholAmount = 1;

    [Header("Runtime")]
    [SerializeField] private int alcoholLevel = 0;

    public int AlcoholLevel => alcoholLevel;
    public int MaxLevel => maxLevel;

    public float NormalizedLevel => maxLevel > 0 ? (float)alcoholLevel / maxLevel : 0f;
    public float EffectIntensity => Mathf.Pow(NormalizedLevel, effectExponent);

    public event Action<int> OnAlcoholLevelChanged;

    void Update()
    {
        if (Input.GetKeyDown(debugAddAlcoholKey))
        {
            AddAlcohol(debugAddAlcoholAmount);
        }
    }

    public void AddAlcohol(int amount)
    {
        if (amount <= 0) return;

        alcoholLevel = Mathf.Min(alcoholLevel + amount, maxLevel);
        OnAlcoholLevelChanged?.Invoke(alcoholLevel);
    }

    public void AddBeer()
    {
        AddAlcohol(1);
    }

    public void ResetLevel()
    {
        alcoholLevel = 0;
        OnAlcoholLevelChanged?.Invoke(alcoholLevel);
    }
}
