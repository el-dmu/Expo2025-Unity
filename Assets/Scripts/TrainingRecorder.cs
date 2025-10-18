using UnityEngine;
using UnityEngine.InputSystem;

public class TrainingRecorder : MonoBehaviour
{
    public DataManager dataManager;

    void Update()
    {
        // 스페이스 바를 누르면 녹화 시작
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            dataManager.StartRecording();
        }

        // 엔터 키를 누르면 녹화 중지 및 전송
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            string motionName = "fire_extinguisher_lift";

            // SessionManager에서 사번을 가져옵니다.
            string empNo = "UNKNOWN"; // 기본값 설정
            if (SessionManager.Instance != null)
            {
                empNo = SessionManager.Instance.employeeID;
            }
            else
            {
                Debug.LogWarning("SessionManager를 찾을 수 없습니다. 기본 사번 'UNKNOWN'을 사용합니다.");
            }
            
            dataManager.StopRecordingAndSend(motionName, empNo);
        }
    }
}