using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

/// <summary>
/// 게임 중 발생하는 치명적인 예외(Exception)를 감지하여 PlayFab으로 전송하는 클래스.
/// 이 스크립트를 가진 빈 게임 오브젝트를 만들어 첫 씬에 배치하세요.
/// </summary>
public class ExceptionReporter : MonoBehaviour
{
    public static ExceptionReporter Instance { get; private set; }
    
    // 한 세션에서 동일한 오류가 반복적으로 전송되는 것을 막기 위한 Set
    private HashSet<string> _sentExceptionsThisSession = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Unity의 로그 메시지 이벤트를 구독합니다.
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        // 구독을 해제합니다.
        Application.logMessageReceived -= HandleLog;
    }

    /// <summary>
    /// Unity의 모든 로그 메시지가 호출될 때 실행되는 핸들러 메서드입니다.
    /// </summary>
    /// <param name="logString">오류 메시지</param>
    /// <param name="stackTrace">호출 스택</param>
    /// <param name="type">로그 타입 (Log, Warning, Error, Exception 등)</param>
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 로그 타입이 Exception일 경우에만 처리합니다.
        // (Warning이나 Error까지 보내면 너무 많은 데이터가 쌓일 수 있습니다.)
        if (type == LogType.Exception)
        {
            // 이 세션에서 이미 보낸적 있는 오류 메시지라면 건너뜁니다. (서버 과부하 방지)
            if (_sentExceptionsThisSession.Contains(logString))
            {
                return;
            }
            
            // PlayFab 클라이언트가 로그인 상태인지 확인합니다.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                return;
            }
            
            // 전송할 데이터를 구성합니다.
            var request = new WriteClientPlayerEventRequest
            {
                EventName = "client_exception", // 이벤트 이름
                Body = new Dictionary<string, object>
                {
                    { "error_message", logString }, // 실제 오류 메시지
                    { "stack_trace", stackTrace },   // 호출 스택 (가장 중요!)
                    { "game_version", Application.version }, // 게임 버전
                    { "scene_name", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name } // 오류 발생 씬
                    // 필요하다면 다른 유용한 정보 (예: 플레이어 레벨, 현재 웨이브 등)를 추가할 수 있습니다.
                }
            };
            
            // PlayFab으로 이벤트 전송
            PlayFabClientAPI.WritePlayerEvent(request, 
                result => {
                    Debug.Log("<color=orange>[ExceptionReporter] 치명적 오류 정보를 PlayFab으로 전송했습니다.</color>");
                },
                error => {
                    Debug.LogError("[ExceptionReporter] 오류 정보 전송 실패: " + error.GenerateErrorReport());
                }
            );
            
            // 보낸 오류 목록에 추가하여 중복 전송을 방지합니다.
            _sentExceptionsThisSession.Add(logString);
        }
    }
}