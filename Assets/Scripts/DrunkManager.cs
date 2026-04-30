using UnityEngine;

public class DrunkManager : MonoBehaviour
{
    [Header("Alcohol")]
    [SerializeField] float alcoholLevel = 0f;
    [SerializeField] float maxAlcohol = 100f;
    [SerializeField] float drinkAmount = 15f;
    [SerializeField] float soberRate = 4f;
    [SerializeField] float smoothingSpeed = 3f;
    [SerializeField] float intensityExponent = 2.5f;

    float smoothedAlcoholLevel = 0f;

    public float AlcoholLevel => alcoholLevel;
    public float NormalizedLevel => maxAlcohol <= 0f ? 0f : alcoholLevel / maxAlcohol;
    public float SmoothedNormalizedLevel => maxAlcohol <= 0f ? 0f : smoothedAlcoholLevel / maxAlcohol;
    public float EffectIntensity => Mathf.Pow(SmoothedNormalizedLevel, intensityExponent);

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            AddAlcohol(drinkAmount);
        }

        alcoholLevel = Mathf.MoveTowards(alcoholLevel, 0f, soberRate * Time.deltaTime);
        smoothedAlcoholLevel = Mathf.Lerp(smoothedAlcoholLevel, alcoholLevel, smoothingSpeed * Time.deltaTime);
    }

    public void AddAlcohol(float amount)
    {
        alcoholLevel = Mathf.Clamp(alcoholLevel + amount, 0f, maxAlcohol);
    }
}
