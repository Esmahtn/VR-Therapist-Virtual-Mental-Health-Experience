using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StressManager : MonoBehaviour
{
    public static StressManager Instance;

    [Header("UI")]
    public Slider stressSlider;
    public TMP_Text stressPercentText;   // %100 → %0
    public TMP_Text feedbackText;        // Stress 0 olunca mesaj

    [Header("Stress Ayarları")]
    public float maxStress = 100f;

    private float currentStress;
    private bool stressFinished = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        currentStress = maxStress;
        UpdateUI();

        // Başlangıçta feedback kapalı
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    // 🔻 UniversalSmash burayı çağırıyor
    public void ReduceStress(float amount)
    {
        if (stressFinished) return;

        // 🔽 HER SEFERİNDE 5 AZALIR
        currentStress -= 5f;
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);

        // 🔔 Stress tamamen bitti mi?
        if (currentStress <= 0f)
        {
            stressFinished = true;
            OnStressFinished();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        // Slider (0–1)
        if (stressSlider != null)
            stressSlider.value = currentStress / maxStress;

        // % Yazısı
        if (stressPercentText != null)
        {
            int percent = Mathf.RoundToInt((currentStress / maxStress) * 100f);
            stressPercentText.text = "%" + percent;
        }
    }

    void OnStressFinished()
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = "STRESS SEVİYENİZ NORMAL DÜZEYE ERİŞTİ";
        }

        Debug.Log("STRESS TAMAMEN AZALDI");
    }
}
