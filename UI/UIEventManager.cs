using System;
using UnityEngine;

/// <summary>
/// UI 관련 이벤트를 중앙에서 관리하고 다른 스크립트들이 구독할 수 있도록 하는 싱글톤 클래스
/// </summary>
public class UIEventManager : MonoBehaviour
{
    private static UIEventManager instance; // 싱글톤 인스턴스
    private static bool isQuitting = false; // 애플리케이션 종료 중인지 확인하는 플래그
    
    // UI 이벤트 처리를 위한 Action 델리게이트 선언
    /// <summary>
    /// 플레이어 HP가 변경될 때 발생하는 이벤트 (현재 HP, 최대 HP)
    /// </summary>
    public Action<float, float> OnPlayerHPChanged;
    /// <summary>
    /// 플레이어 SP가 변경될 때 발생하는 이벤트 (현재 SP, 최대 SP)
    /// </summary>
    public Action<float, float> OnPlayerSPChanged;
    /// <summary>
    /// 플레이어 골드가 변경될 때 발생하는 이벤트 (현재 골드)
    /// </summary>
    public Action<int> OnPlayerGoldChanged;
    /// <summary>
    /// 플레이어 타워 코스트가 변경될 때 발생하는 이벤트 (현재 타워 코스트, 최대 타워 코스트)
    /// </summary>
    public Action<int> OnTowerCostChanged; // 이 이벤트는 더 이상 사용되지 않음
    /// <summary>
    /// 시스템 메시지가 발생할 때 발생하는 이벤트 (메시지 타입)
    /// </summary>
    public Action<SystemType> OnSystemMessage;
    
    public static event Action<int> OnRCoinAcquired; // RCoin 획득량 알림 이벤트

    /// <summary>
    /// UIEventManager의 싱글톤 인스턴스를 반환합니다.
    /// 애플리케이션 종료 중에는 null을 반환하여 MissingReferenceException을 방지합니다.
    /// </summary>
    public static UIEventManager Instance
    {
        get
        {
            if (isQuitting) 
            {
                return null;
            }
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIEventManager>();
                if (instance == null)
                {
                    Debug.LogError("UIEventManager 인스턴스를 찾을 수 없습니다. 씬에 배치되었는지 확인하세요.");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        isQuitting = false; // 새로운 플레이 세션 시작 시 플래그 초기화
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 오브젝트 유지
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

    /// <summary>
    /// 애플리케이션이 종료될 때 호출됩니다.
    /// 싱글톤 인스턴스 접근 시 MissingReferenceException을 방지하기 위해 플래그를 설정합니다.
    /// </summary>
    private void OnApplicationQuit()
    {
        isQuitting = true; 
    }

    /// <summary>
    /// 플레이어 HP 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="currentHP">현재 HP</param>
    /// <param name="maxHP">최대 HP</param>
    public void UpdatePlayerHP(float currentHP, float maxHP)
    {
        OnPlayerHPChanged?.Invoke(currentHP, maxHP);
    }

    /// <summary>
    /// 플레이어 SP 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="currentSP">현재 SP</param>
    /// <param name="maxSP">최대 SP</param>
    public void UpdatePlayerSP(float currentSP, float maxSP)
    {
        OnPlayerSPChanged?.Invoke(currentSP, maxSP);
    }

    /// <summary>
    /// 플레이어 골드 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="gold">현재 골드</param>
    public void UpdatePlayerGold(int gold)
    {
        OnPlayerGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// 시스템 메시지 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="type">시스템 메시지 타입</param>
    public void ShowSystemMessage(SystemType type)
    {
        OnSystemMessage?.Invoke(type);
    }

    /// <summary>
    /// 플레이어 타워 코스트 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="currentTowerCost">현재 타워 코스트</param>
    /// <param name="maxTowerCost">최대 타워 코스트</param>
    public void UpdateTowerCost(int currentTowerCost)
    {
        OnTowerCostChanged?.Invoke(currentTowerCost);
    }
    
    // RCoin 획득 이벤트를 발생시키는 메소드
    public static void NotifyRCoinAcquired(int amount)
    {
        OnRCoinAcquired?.Invoke(amount);
    }
}