using UnityEngine;
using System.Collections.Generic;

public class WaypointMover : MonoBehaviour
{
    public VrWaypointWalker path;
    public float speed = 2.0f;
    public bool loop = false;

    private bool isMoving = false;
    private int currentWaypointIndex = 0;
    private List<Transform> waypoints;

    // [추가] 전체 경로가 시작되었는지 확인하여 이중 입력을 막는 플래그
    private bool hasPathStarted = false;

    void Start()
    {
        if (path != null)
        {
            waypoints = path.waypoints;
        }
    }

    void Update()
    {
        if (!isMoving || waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        // 모든 경로를 마쳤으면 더 이상 진행하지 않음
        if (currentWaypointIndex >= waypoints.Count) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);

        // 목표 지점에 도착했을 때
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            // 도착한 웨이포인트에 WaypointInfo 컴포넌트가 있는지 확인
            WaypointInfo waypointInfo = targetWaypoint.GetComponent<WaypointInfo>();

            // WaypointInfo가 있고, stopHere가 true이면 이동을 멈춤
            if (waypointInfo != null && waypointInfo.stopHere)
            {
                Debug.Log($"정지 지점 '{targetWaypoint.name}'에 도착. 입력을 대기합니다.");
                isMoving = false; // 이동 중지
            }
            else // 멈출 필요가 없다면 자동으로 다음 웨이포인트로 이동
            {
                currentWaypointIndex++;
                if (loop && currentWaypointIndex >= waypoints.Count)
                {
                    currentWaypointIndex = 0;
                }
            }
        }
    }

    // G키 입력으로 멈춘 지점에서 이동을 재개하는 함수
    public void ContinueToNextWaypoint()
    {
        // [수정] 경로가 아직 시작되지 않았거나, 이미 움직이는 중이면 함수를 종료
        if (!hasPathStarted || isMoving) return;

        currentWaypointIndex++;

        if (currentWaypointIndex >= waypoints.Count)
        {
            Debug.Log("모든 웨이포인트 이동을 완료했습니다.");
            isMoving = false; // 이동 완료 후 정지
            return;
        }

        Debug.Log($"입력을 받아 다음 웨이포인트 '{waypoints[currentWaypointIndex].name}'로 이동을 재개합니다.");
        isMoving = true;
    }

    // UI 안내가 모두 끝난 후, GameManager에 의해 딱 한 번 호출되는 함수
    public void StartPath()
    {
        // [수정] 경로가 이미 시작되었다면 함수를 종료 (중복 실행 방지)
        if (hasPathStarted) return;
        
        if (waypoints != null && waypoints.Count > 0)
        {
            Debug.Log("경로 이동을 시작합니다.");
            isMoving = true;
            hasPathStarted = true; // 경로가 시작되었음을 기록
        }
    }
}

