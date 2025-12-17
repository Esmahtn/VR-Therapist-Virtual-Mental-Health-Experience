using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StressManager : MonoBehaviour
{
    public static StressManager Instance;

    [Header("UI Elemanları")]
    public GameObject stressUIPaneli;    // Stress Bar'ın tamamını kapsayan ana obje
    public Slider stressSlider;
    public TMP_Text stressPercentText;   
    public TMP_Text feedbackText;        

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

        // Başlangıçta her şeyi kapatıyoruz (1. odada gözükmemesi için)
        if (stressUIPaneli != null)
            stressUIPaneli.SetActive(false); 
            
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    // 2. Odaya geçince bu fonksiyonu çağıracağız
    public void BariAc()
    {
        if (stressUIPaneli != null)
        {
            stressUIPaneli.SetActive(true);
            Debug.Log("Stress Bar aktif edildi.");
        }
    }

    // Objeler kırılınca bu fonksiyon çağırılacak
    public void ReduceStress(float amount)
    {
        if (stressFinished) return;

        currentStress -= amount; // Artık dışarıdan ne kadar düşeceği söylenebilir
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);

        if (currentStress <= 0f)
        {
            stressFinished = true;
            OnStressFinished();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (stressSlider != null)
            stressSlider.value = currentStress / maxStress;

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
    }
}