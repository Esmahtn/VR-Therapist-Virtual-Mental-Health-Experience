using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class StressManager : MonoBehaviour
{
    public static StressManager Instance;

    [Header("UI")]
    public Slider stressSlider;
    public TMP_Text stressText;
   // %100 yazısı için (opsiyonel)

    [Header("Stress Ayarları")]
    public float maxStress = 100f;
    private float currentStress;

    void Awake()
    {
        // Singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        currentStress = maxStress;
        UpdateUI();
    }

    public void ReduceStress(float amount)
    {
        currentStress -= amount;
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);
        UpdateUI();
    }

    void UpdateUI()
    {
        // Slider 0–1 arası çalışıyor
        stressSlider.value = currentStress / maxStress;

        // % yazısı
        if (stressText != null)
        {
            int percent = Mathf.RoundToInt((currentStress / maxStress) * 100f);
            stressText.text = "%" + percent;
        }
    }
}
