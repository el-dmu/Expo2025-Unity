using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.IO; // 파일을 다루기 위해 필요
using System;   // DateTime을 사용하기 위해 필요

// JSON의 "sensorData" 배열에 들어갈 각 프레임의 데이터 구조
[System.Serializable]
public class SensorFrame
{
    public int flex01, flex02, flex03, flex04, flex05;
    public int flex06, flex07, flex08, flex09, flex10;
    public float qw1, qx1, qy1, qz1;
    public float qw2, qx2, qy2, qz2;
}

// 최종적으로 보낼 JSON 전체 구조
[System.Serializable]
public class FinalPayload
{
    public string motionName;
    public string empNo;
    public List<SensorFrame> sensorData = new List<SensorFrame>();
}

public class DataManager : MonoBehaviour
{
    [Header("서버 설정")]
    public string serverUrl = "http://127.0.0.1:8000/api/your-endpoint/";

    [Header("데이터 소스")]
    public ReceiveHandDataL leftHand;
    public ReceiveHandDataR rightHand;
    
    [Header("수집 설정")]
    [Tooltip("데이터를 수집하는 시간 간격 (초)")]
    public float collectionInterval = 0.1f;

    [Header("저장 설정")]
    [Tooltip("체크하면 서버로 보내는 대신 로컬 파일로 저장합니다.")]
    public bool saveToFileInsteadOfServer = false;
    [Tooltip("JSON 파일을 저장할 폴더 이름 (Assets 폴더 내에 생성됩니다)")]
    public string saveFolderName = "SensorDataOutput";

    private List<SensorFrame> collectedFrames = new List<SensorFrame>();
    private Coroutine collectionCoroutine;
    private bool isRecording = false;

    // 외부에서 녹화를 시작하기 위해 호출할 함수
    public void StartRecording()
    {
        if (isRecording) return;
        if (leftHand == null || rightHand == null)
        {
            Debug.LogError("양손의 데이터 수신기가 연결되지 않았습니다!");
            return;
        }

        isRecording = true;
        collectedFrames.Clear();
        collectionCoroutine = StartCoroutine(CollectDataRoutine());
        Debug.Log(">>> 데이터 수집 시작!");
    }

    // 외부에서 녹화를 중지하고 데이터를 전송 또는 저장하기 위해 호출할 함수
    public void StopRecordingAndSend(string motionName, string employeeNumber)
    {
        if (!isRecording) return;

        isRecording = false;
        if (collectionCoroutine != null)
        {
            StopCoroutine(collectionCoroutine);
        }
        Debug.Log($">>> 데이터 수집 중지! 총 {collectedFrames.Count} 프레임 수집됨.");
        
        FinalPayload finalPayload = new FinalPayload
        {
            motionName = motionName,
            empNo = employeeNumber,
            sensorData = collectedFrames
        };

        string jsonData = JsonUtility.ToJson(finalPayload, true);
        Debug.Log(jsonData);
        
        // ★★★ 분기 처리: 체크박스 상태에 따라 저장 또는 전송 ★★★
        if (saveToFileInsteadOfServer)
        {
            SaveDataToFile(jsonData, motionName, employeeNumber);
        }
        else
        {
            StartCoroutine(SendDataToServer(jsonData));
        }
    }

    // 일정 간격으로 센서 데이터를 수집하는 코루틴
    private IEnumerator CollectDataRoutine()
    {
        while (isRecording)
        {
            SensorFrame currentFrame = new SensorFrame
            {
                // 왼손 데이터
                flex01 = leftHand.flexVals[0],
                flex02 = leftHand.flexVals[1],
                flex03 = leftHand.flexVals[2],
                flex04 = leftHand.flexVals[3],
                flex05 = leftHand.flexVals[4],
                qw1 = leftHand.QuaterVals[0],
                qx1 = leftHand.QuaterVals[1],
                qy1 = leftHand.QuaterVals[2],
                qz1 = leftHand.QuaterVals[3],

                // 오른손 데이터
                flex06 = rightHand.flexVals[0],
                flex07 = rightHand.flexVals[1],
                flex08 = rightHand.flexVals[2],
                flex09 = rightHand.flexVals[3],
                flex10 = rightHand.flexVals[4],
                qw2 = rightHand.QuaterVals[0],
                qx2 = rightHand.QuaterVals[1],
                qy2 = rightHand.QuaterVals[2],
                qz2 = rightHand.QuaterVals[3],
            };
            
            collectedFrames.Add(currentFrame);
            
            yield return new WaitForSeconds(collectionInterval);
        }
    }

    // JSON 데이터를 서버로 보내는 코루틴
    private IEnumerator SendDataToServer(string jsonData)
    {
        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("서버로 데이터를 전송합니다...");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"서버 전송 실패: {request.error}");
            }
            else
            {
                Debug.Log($"서버 전송 성공: {request.downloadHandler.text}");
            }
        }
    }

    // ★★★ JSON 데이터를 파일로 저장하는 함수 ★★★
    private void SaveDataToFile(string jsonData, string motionName, string empNo)
    {
        try
        {
            // 1. 저장 경로 생성 (Assets/SensorDataOutput)
            string directoryPath = Path.Combine(Application.dataPath, saveFolderName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 2. 고유한 파일 이름 생성 (동작이름_사번_타임스탬프.json)
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{motionName}_{empNo}_{timestamp}.json";
            string fullPath = Path.Combine(directoryPath, fileName);

            // 3. 파일로 저장
            File.WriteAllText(fullPath, jsonData);

            Debug.Log($"<color=green>>>> 데이터 파일 저장 성공! 경로: {fullPath}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 저장 실패: {e.Message}");
        }
    }
}