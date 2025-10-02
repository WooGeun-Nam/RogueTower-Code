using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GachaType
{
    Normal,
    Advanced,
    Rare
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Gacha Settings")]
    [Tooltip("일반 뽑기 비용")]
    [SerializeField] private int normalGachaCost = 50000;
    [Tooltip("고급 뽑기 비용")]
    [SerializeField] private int advancedGachaCost = 100000;
    [Tooltip("희귀 뽑기 비용")]
    [SerializeField] private int rareGachaCost = 200000;
    [SerializeField] private float gachaAnimationDuration = 2f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rCoinText;
    [SerializeField] private Button normalDrawButton;
    [SerializeField] private Button advancedDrawButton;
    [SerializeField] private Button rareDrawButton;
    
    [Header("Animation Panel")]
    [SerializeField] private GameObject gachaAnimationPanel;
    [SerializeField] private Image rewardImage;
    [SerializeField] private TextMeshProUGUI rewardNameText;
    [SerializeField] private TextMeshProUGUI rewardDescriptionText;
    [SerializeField] private Slider gachaSlider;
    [SerializeField] private GameObject gachaCloseButton; // [추가] 닫기 버튼의 GameObject를 연결할 변수

    private bool isGachaInProgress = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        UpdateRCoinDisplay();
        if (gachaAnimationPanel != null)
        {
            gachaAnimationPanel.SetActive(false);
        }
    }

    public void UpdateRCoinDisplay()
    {
        if (EquipmentManager.Instance != null && EquipmentManager.Instance.currentPlayerData != null)
        {
            int currentRCoin = EquipmentManager.Instance.currentPlayerData.rCoin;
            rCoinText.text = currentRCoin.ToString("N0");
            
            if (!isGachaInProgress)
            {
                normalDrawButton.interactable = currentRCoin >= normalGachaCost;
                advancedDrawButton.interactable = currentRCoin >= advancedGachaCost;
                rareDrawButton.interactable = currentRCoin >= rareGachaCost;
            }
        }
    }

    public void OnClickDraw(int gachaTypeIndex)
    {
        if (isGachaInProgress) return;

        // 숫자로 받은 타입을 Enum으로 변환
        GachaType type = (GachaType)gachaTypeIndex;
        int cost = 0;

        // 타입에 맞는 비용 설정
        switch (type)
        {
            case GachaType.Normal:
                cost = normalGachaCost;
                break;
            case GachaType.Advanced:
                cost = advancedGachaCost;
                break;
            case GachaType.Rare:
                cost = rareGachaCost;
                break;
        }

        // 재화 확인 및 차감
        PlayerData playerData = EquipmentManager.Instance.currentPlayerData;
        if (playerData.rCoin < cost)
        {
            // Debug.Log("RCoin이 부족합니다.");
            return;
        }
        
        SoundManager.Instance.PlaySFX("SFX_UI_UseCoin");
        
        playerData.rCoin -= cost;
        GameDataManager.Instance.SavePlayerData(playerData);
        UpdateRCoinDisplay();
        
        // 애니메이션 코루틴 시작
        StartCoroutine(StartGachaAnimation(type));
    }

    private IEnumerator StartGachaAnimation(GachaType type)
    {
        isGachaInProgress = true;
        normalDrawButton.interactable = false;
        advancedDrawButton.interactable = false;
        rareDrawButton.interactable = false;

        try
        {
            gachaAnimationPanel.SetActive(true);
            SoundManager.Instance.PlaySFX("SFX_UI_Gacha");
            
            gachaSlider.value = 0;
            if (rewardDescriptionText != null) rewardDescriptionText.text = "";
            if (gachaCloseButton != null) gachaCloseButton.SetActive(false);
            
            EquipmentDataModel finalRewardData = LootManager.Instance.GetRandomEquipmentFromGacha(type);
            if (finalRewardData == null)
            {
                Debug.LogError("가챠에서 장비를 뽑지 못했습니다. LootManager를 확인하세요.");
                yield break;
            }

            // 아이콘 프리로딩 시작
            List<Sprite> droppableSprites = new List<Sprite>();
            List<EquipmentDataModel> allEquipment = LootManager.Instance.AllDroppableEquipment;
            
            if (allEquipment != null)
            {
                foreach (var item in allEquipment)
                {
                    Sprite loadedSprite = Resources.Load<Sprite>(item.iconPath);
                    if (loadedSprite != null) droppableSprites.Add(loadedSprite);
                }
            }
            
            float elapsedTime = 0f;
            float slotChangeTimer = 0f;
            while (elapsedTime < gachaAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                gachaSlider.value = elapsedTime / gachaAnimationDuration;
                slotChangeTimer -= Time.deltaTime;
                if (slotChangeTimer <= 0f)
                {
                    slotChangeTimer = Random.Range(0.02f, 0.1f);
                    if (droppableSprites.Count > 0)
                    {
                        rewardImage.sprite = droppableSprites[Random.Range(0, droppableSprites.Count)];
                        EquipmentDataModel randomItem = allEquipment[Random.Range(0, allEquipment.Count)];
                        string rarityColor = GetColorForRarity(randomItem.rarity);
                        rewardNameText.text = $"<color={rarityColor}>{randomItem.equipmentName}</color>";
                    }
                }
                yield return null;
            }

            // 최종 결과 표시
            gachaSlider.value = 1;
            rewardImage.sprite = Resources.Load<Sprite>(finalRewardData.iconPath);
            string finalRarityColor = GetColorForRarity(finalRewardData.rarity);
            rewardNameText.text = $"<color={finalRarityColor}>{finalRewardData.equipmentName}</color>";
            if (rewardDescriptionText != null) rewardDescriptionText.text = finalRewardData.description;
            SoundManager.Instance.PlaySFX("SFX_System_WaveClear");

            PlayerEquipmentInstance newInstance = EquipmentManager.Instance.CreateRandomizedEquipmentInstance(finalRewardData);
            EquipmentManager.Instance.AddEquipmentToPlayer(newInstance, false);
        }
        finally
        {
            if (gachaCloseButton != null) gachaCloseButton.SetActive(true);
            isGachaInProgress = false;
            UpdateRCoinDisplay();
        }
    }
    
    /// <summary>
    /// 뽑기 '결과' 패널만 닫는 메소드입니다.
    /// </summary>
    public void CloseGachaPanel()
    {
        // 뽑기 애니메이션 중에는 닫히지 않도록 방지
        if (gachaAnimationPanel != null && !isGachaInProgress)
        {
            gachaAnimationPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 상점 패널 전체를 닫는 메소드입니다.
    /// </summary>
    // 상점 패널을 닫는 기능을 하는 메소드
    public void CloseShopPanel()
    {
        // 뽑기 애니메이션 중에는 닫히지 않도록 방지
        if (isGachaInProgress) return;

        // this.gameObject는 ShopManager.cs 스크립트가 붙어있는 'ShopPanel' 프리팹 전체를 의미합니다.
        this.gameObject.SetActive(false);
        SoundManager.Instance.PlaySFX("SFX_UI_ButtonClick");
    }
    
    private string GetColorForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return "#808080";
            case Rarity.Uncommon: return "green";
            case Rarity.Rare: return "blue";
            case Rarity.Epic: return "purple";
            case Rarity.Legendary: return "orange";
            default: return "white";
        }
    }
}