using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// PlayFab 자동 익명 로그인을 처리하는 싱글톤 매니저 클래스입니다.
/// </summary>
public class PlayFabLogin : MonoBehaviour
{
    // 이 클래스의 단일 인스턴스를 저장하여 어디서든 쉽게 접근할 수 있게 합니다.
    public static PlayFabLogin Instance { get; private set; }
    
    // 로그인 성공 후 서버로부터 발급받는 플레이어의 고유 ID입니다.
    public static string PlayerPlayFabID { get; private set; }

    private void Awake()
    {
        // 싱글톤 패턴 구현: PlayFabLogin 매니저가 단 하나만 존재하도록 보장합니다.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject); // 이미 존재하면 새로 생긴 것은 파괴
        }
    }

    private void Start()
    {
        // 게임이 시작되면 자동으로 로그인을 시도합니다.
        Login();
    }

    /// <summary>
    /// 기기의 고유 ID를 사용하여 PlayFab에 익명으로 로그인합니다.
    /// </summary>
    private void Login()
    {
        // 로그인 요청에 필요한 정보를 담는 객체를 생성합니다.
        var request = new LoginWithCustomIDRequest
        {
            // 각 기기마다 고유한 ID를 CustomId로 사용합니다.
            CustomId = SystemInfo.deviceUniqueIdentifier,
            // 이 CustomId로 된 계정이 없으면 자동으로 새로 생성해달라는 중요한 옵션입니다.
            CreateAccount = true 
        };
        
        // PlayFab 서버에 로그인 요청을 보냅니다. 성공 시 OnLoginSuccess, 실패 시 OnLoginFailure가 호출됩니다.
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    /// <summary>
    /// 로그인에 성공했을 때 호출되는 콜백 함수입니다.
    /// </summary>
    private void OnLoginSuccess(LoginResult result)
    {
        // 로그인에 성공하면, 서버가 발급해준 고유 ID (PlayFabId)를 저장합니다.
        PlayerPlayFabID = result.PlayFabId;
        // Debug.Log($"<color=cyan>[PlayFab] 로그인 성공! PlayFab ID: {PlayerPlayFabID}</color>");
        
        // 향후 이 부분에서 유저의 온라인 데이터(PlayerData)를 불러오는 로직을 추가할 수 있습니다.
    }

    /// <summary>
    /// 로그인에 실패했을 때 호출되는 콜백 함수입니다.
    /// </summary>
    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("[PlayFab] 로그인에 실패했습니다.");
        Debug.LogError(error.GenerateErrorReport()); // 실패 원인을 자세히 콘솔에 출력합니다.
    }
}