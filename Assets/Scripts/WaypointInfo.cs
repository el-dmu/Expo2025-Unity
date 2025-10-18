using UnityEngine;
using UnityEngine.Events;

public class WaypointInfo : MonoBehaviour
{
    [Header("구역 유형 설정")]
    [Tooltip("이 웨이포인트가 화재 진압 미션을 시작하는 곳이라면 체크하세요.")]
    public bool isFirefightingZone = false;

    [Header("도착 시 표시할 내용")]
    [TextArea(3, 5)]
    public string message = "메시지를 입력하세요.";
    public AudioClip narrationClip;

    [Header("이벤트")]
    public UnityEvent<string> onArrive;
}

