using UnityEngine;
using System.Collections.Generic;

public class VrWaypointWalker : MonoBehaviour
{
    public VrWaypointWalker path;
    public float speed = 2.0f;
    public bool loop = false;

    // [수정] 이동 상태를 제어하는 변수. canStartMoving -> isMoving 으로 이름 변경
    private bool isMoving = false;
    private int currentWaypointIndex = 0;
    public List<Transform> waypoints;
    /// <summary>
    /// 인스PECTOR 버튼을 통해 새로운 웨이포인트를 추가합니다.
    /// </summary>
    public void AddWaypoint()
    {
        GameObject newWaypointObj = new GameObject("Waypoint " + waypoints.Count);
        newWaypointObj.transform.SetParent(this.transform);

        // 마지막 웨이포인트가 있다면 그 근처에, 없다면 이 오브젝트 위치에 생성합니다.
        if (waypoints.Count > 0)
        {
            newWaypointObj.transform.position = waypoints[waypoints.Count - 1].position + Vector3.forward * 2.0f;
        }
        else
        {
            newWaypointObj.transform.position = transform.position + transform.forward * 2.0f;
        }

        // 생성된 웨이포인트를 리스트에 추가합니다.
        waypoints.Add(newWaypointObj.transform);
    }

    /// <summary>
    /// 지정된 월드 좌표에 새로운 웨이포인트를 생성하고 추가합니다.
    /// </summary>
    /// <param name="position">새로운 웨이포인트가 생성될 위치입니다.</param>
    public void AddWaypointAt(Vector3 position)
    {
        GameObject newWaypoint = new GameObject("Waypoint " + waypoints.Count);
        newWaypoint.transform.position = position;
        newWaypoint.transform.SetParent(this.transform);

        // 생성된 웨이포인트를 리스트에 추가합니다.
        waypoints.Add(newWaypoint.transform);

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    void Start()
    {
        if (path != null)
        {
            waypoints = path.waypoints;
        }
    }

    void Update()
    {
        // [수정] isMoving이 false이면 이동 로직을 실행하지 않음
        if (!isMoving || waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
        {
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);

        // 목표 지점에 도착했을 때
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            // [추가] 도착한 웨이포인트에 WaypointInfo 컴포넌트가 있는지 확인
            WaypointInfo waypointInfo = targetWaypoint.GetComponent<WaypointInfo>();

            // [추가] WaypointInfo가 있고, stopHere가 true이면 이동을 멈춤
            if (waypointInfo != null && waypointInfo.stopHere)
            {
                Debug.Log($"정지 지점 '{targetWaypoint.name}'에 도착. 입력을 대기합니다.");
                isMoving = false; // 이동 중지
            }
            else // [추가] 멈출 필요가 없다면 자동으로 다음 웨이포인트로 이동
            {
                currentWaypointIndex++;
                if (loop && currentWaypointIndex >= waypoints.Count)
                {
                    currentWaypointIndex = 0;
                }
            }
        }
    }

    // [수정] StartMovement -> ContinueToNextWaypoint 함수로 역할 변경 및 확장
    public void ContinueToNextWaypoint()
    {
        // [추가] 멈춰있는 상태일 때만 다음 로직을 실행
        if (isMoving) return;

        // [추가] 다음 웨이포인트로 인덱스를 넘김
        currentWaypointIndex++;
        if (loop && currentWaypointIndex >= waypoints.Count)
        {
            currentWaypointIndex = 0;
        }

        // [추가] 모든 경로가 끝났는지 확인
        if (currentWaypointIndex >= waypoints.Count)
        {
            Debug.Log("모든 웨이포인트 이동을 완료했습니다.");
            // 여기서 훈련 종료 등 다른 로직을 호출할 수 있습니다.
            return;
        }

        Debug.Log($"입력을 받아 다음 웨이포인트 '{waypoints[currentWaypointIndex].name}'로 이동을 재개합니다.");
        isMoving = true; // 이동 재개
    }

    // [추가] 외부에서 전체 경로 이동을 시작시키는 함수
    public void StartPath()
    {
        if (waypoints != null && waypoints.Count > 0)
        {
            Debug.Log("경로 이동을 시작합니다.");
            isMoving = true;
        }
    }
}
