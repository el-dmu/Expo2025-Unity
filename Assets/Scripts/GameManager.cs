using UnityEngine;

public class GameManager : MonoBehaviour
{
    public UIManager uiManager;
    public WaypointMover playerMover;

    void Start()
    {
        // UIManager의 "안내가 끝났다"는 이벤트가 발생하면,
        // playerMover의 "이동을 시작하라"는 함수를 실행하도록 연결
        if (uiManager != null && playerMover != null)
        {
            uiManager.OnInstructionFinished.AddListener(playerMover.StartPath);
        }
        else
        {
            Debug.LogError("UIManager 또는 PlayerMover가 할당되지 않았습니다!");
        }
    }
}