using UnityEngine;

/// <summary>
/// 개별 웨이포인트에 대한 추가 정보를 담는 컴포넌트입니다.
/// </summary>
public class WaypointInfo : MonoBehaviour
{
    [Tooltip("플레이어가 이 웨이포인트에 도착했을 때 멈추게 하려면 체크하세요.")]
    public bool stopHere = false;
}
