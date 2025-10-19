using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요
using TMPro; // TextMeshPro를 사용하기 위해 필요

public class TitleUI : MonoBehaviour
{
    public TMP_InputField employeeIdField; // 사번 입력창을 연결할 변수

    // 버튼을 누를 때 호출될 함수
    public void StartTraining()
    {
        // 1. 입력창의 텍스트를 가져옴
        string id = employeeIdField.text;

        // 2. GameManager에 사번 저장
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.employeeID = id;
        }

        // 3. 메인 훈련 씬(FE_Scene_BG_added)을 로드
        SceneManager.LoadScene("LeapMotionScene_edit");
    }
}