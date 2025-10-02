using UnityEngine;
using System.IO;
using System.Text; // 암호화를 위해 System.Text
using System;     // Action 이벤트를 위해 필요 (PlayerData 로드 이벤트용)

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    public static bool IsReady { get; private set; }

    private string savePath; // 저장 경로
    
    // 암호화에 사용할 비밀 키
    private readonly string encryptionKey = "???";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            IsReady = false; // 준비 안됨으로 초기화

            // Application.persistentDataPath의 상위 디렉토리 (LocalLow)를 가져옴
            string baseAppDataPath = Path.GetDirectoryName(Path.GetDirectoryName(Application.persistentDataPath));
            // 원하는 게임 저장 디렉토리 경로를 구성
            string gameSaveDirectory = Path.Combine(baseAppDataPath, "RogueTower");

            // 저장 디렉토리가 없으면 생성
            if (!Directory.Exists(gameSaveDirectory))
            {
                Directory.CreateDirectory(gameSaveDirectory);
            }

            savePath = Path.Combine(gameSaveDirectory, "playerData.json");
            // Debug.Log($"저장 경로: {savePath}");

            IsReady = true; // 모든 준비 완료
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 플레이어 데이터를 파일에 저장합니다.
    /// </summary>
    /// <param name="playerData">저장할 PlayerData 객체</param>
    public void SavePlayerData(PlayerData playerData)
    {
        string json = JsonUtility.ToJson(playerData, true);
        //File.WriteAllText(savePath, json);
        
        // JSON 데이터를 파일에 저장하기 전에 암호화합니다.
        string encryptedJson = EncryptDecrypt(json);
        
        // 암호화된 데이터를 파일에 씁니다.
        File.WriteAllText(savePath, encryptedJson);
    }

    /// <summary>
    /// [수정] 파일에서 플레이어 데이터를 불러옵니다. 구버전(암호화 안됨) 데이터의 마이그레이션을 지원합니다.
    /// </summary>
    /// <returns>불러온 PlayerData 객체, 파일이 없으면 새로운 PlayerData 객체 반환</returns>
    public PlayerData LoadPlayerData()
    {
        // PlayerData를 담을 변수를 미리 선언합니다.
        PlayerData loadedData;

        if (File.Exists(savePath))
        {
            // 일단 파일의 모든 내용을 문자열로 읽어옵니다.
            string fileContents = File.ReadAllText(savePath);

            try
            {
                // 1. 먼저, 데이터가 '암호화된 최신 버전'이라고 가정하고 복호화를 시도합니다.
                string decryptedJson = EncryptDecrypt(fileContents);
                loadedData = JsonUtility.FromJson<PlayerData>(decryptedJson);
                
                // JsonUtility가 null을 반환하는 경우(파일 내용이 있지만 유효한 JSON이 아님)를 대비하여 예외를 발생시킵니다.
                if (loadedData == null)
                {
                    throw new System.Exception("Decrypted data is not valid JSON.");
                }

                // Debug.Log("암호화된 최신 세이브 파일을 성공적으로 불러왔습니다.");
            }
            catch (System.Exception)
            {
                // 2. 복호화 또는 JSON 파싱에 실패하면, '암호화되지 않은 구버전' 데이터로 간주합니다.
                Debug.LogWarning("데이터 복호화 실패, 구버전 세이브 파일로 간주하고 마이그레이션을 시도합니다.");
                
                try
                {
                    // 파일 내용을 일반 JSON 텍스트로 다시 파싱합니다.
                    loadedData = JsonUtility.FromJson<PlayerData>(fileContents);

                    if (loadedData != null)
                    {
                        // [핵심] 즉시 새로운 암호화된 파일로 다시 저장하여 데이터를 최신 버전으로 마이그레이션합니다.
                        SavePlayerData(loadedData);
                        Debug.Log("구버전 데이터를 최신 암호화 버전으로 마이그레이션하고 저장했습니다.");
                    }
                    else
                    {
                        // 구버전 데이터조차 파싱할 수 없다면, 파일이 손상된 것입니다.
                        Debug.LogError("세이브 파일이 손상되어 데이터를 읽을 수 없습니다. 새로운 데이터를 생성합니다.");
                        loadedData = new PlayerData();
                    }
                }
                catch (System.Exception innerEx)
                {
                    // 구버전 데이터 파싱도 실패하는 최악의 경우
                    Debug.LogError($"구버전 데이터 파싱에도 실패했습니다. 파일이 심각하게 손상되었습니다. 오류: {innerEx.Message}");
                    loadedData = new PlayerData();
                }
            }
        }
        else
        {
            // 세이브 파일이 없으면 새로 생성
            Debug.LogWarning("저장된 플레이어 데이터가 없습니다. 새로운 데이터 생성.");
            loadedData = new PlayerData();
        }
        
        // --- 불러온 데이터 안전성 검사 (기존 코드 유지) ---
        if (loadedData.ownedEquipmentInstances == null)
        {
            loadedData.ownedEquipmentInstances = new System.Collections.Generic.List<PlayerEquipmentInstance>();
        }
        if (loadedData.equippedSlots == null || loadedData.equippedSlots.Count == 0)
        {
            loadedData.equippedSlots = new System.Collections.Generic.List<EquipmentSlotEntry>();
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                loadedData.equippedSlots.Add(new EquipmentSlotEntry(type, null));
            }
        }

        // 최종적으로 처리된 PlayerData 객체를 반환합니다.
        return loadedData;
    }
    
    /// <summary>
    /// XOR 연산을 사용하여 문자열을 암호화하거나 복호화합니다.
    /// </summary>
    private string EncryptDecrypt(string data)
    {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            // 데이터의 각 문자를 키의 문자와 XOR 연산합니다.
            result.Append((char)(data[i] ^ encryptionKey[i % encryptionKey.Length]));
        }
        return result.ToString();
    }
}
