using UnityEngine;
using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

// JSON으로 변환할 데이터 구조 (HandData는 동일)
[System.Serializable]
public class HandData
{
    public Quaternion rotation;
    public float[] fingers;
}

// 이 클래스가 이제 JSON의 최상위 구조가 됩니다.
[System.Serializable]
public class HandPosePayload
{
    public string metric = "hand_pose";
    public string ts;
    public int frame;
    public HandData left_hand;
    public HandData right_hand;
}

// JsonWrapper 클래스는 이제 필요 없으므로 삭제했습니다.

public class DataManager : MonoBehaviour
{
    [Header("서버 설정")]
    public string serverUrl = "http://192.168.1.101:8001/api/sensor/ingest/";
    public string apiKey = "051ca48ea0b2cb23b1047bffbfbb390248cc52701492e86aaa968a00a2b4c925";

    [Header("데이터 소스")]
    public HandControllerL leftHandController;
    public HandControllerR rightHandController;

    [Header("전송 설정")]
    public float sendInterval = 0.1f;

    private int frameCount = 0;

    void Start()
    {
        if (leftHandController == null || rightHandController == null)
        {
            Debug.LogError("[DataManager] 양손의 컨트롤러가 모두 연결되지 않았습니다! Inspector 창을 확인해주세요.");
            return;
        }
        StartCoroutine(SendDataRoutine());
    }

    private IEnumerator SendDataRoutine()
    {
        while (true)
        {
            frameCount++;
            string timestamp = DateTime.UtcNow.ToString("o");

            // 1. HandPosePayload 객체를 직접 생성
            HandPosePayload payload = new HandPosePayload
            {
                ts = timestamp,
                frame = frameCount,
                left_hand = new HandData
                {
                    rotation = leftHandController.currentRotation,
                    fingers = leftHandController.fingerValues
                },
                right_hand = new HandData
                {
                    rotation = rightHandController.currentRotation,
                    fingers = rightHandController.fingerValues
                }
            };

            // 2. ★ Wrapper 없이 payload 객체를 바로 JSON으로 변환
            string jsonData = JsonUtility.ToJson(payload);

            // 3. UnityWebRequest 생성 및 헤더 설정 (이하 동일)
            using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-API-Key", apiKey);
                request.SetRequestHeader("Idempotency-Key", frameCount.ToString());

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[DataManager] 서버 전송 실패 (Frame: {frameCount}): {request.error}");
                }
                else
                {
                    Debug.Log($"[DataManager] 서버 전송 성공 (Frame: {frameCount}): {request.downloadHandler.text}");
                }
            }
            
            yield return new WaitForSeconds(sendInterval);
        }
    }
}