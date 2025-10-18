using UnityEngine;
using System.Collections.Generic;

public class VrWaypointWalker : MonoBehaviour
{
    [Header("경로 설정")]
    [Tooltip("이동할 웨이포인트들의 리스트입니다.")]
    public List<Transform> waypoints;

    [Header("이동 설정")]
    public float speed = 2.0f;
    public bool loop = false;

    private bool isMoving = false;
    private int currentWaypointIndex = 0;
    private bool hasPathStarted = false;

    public bool IsMoving => isMoving;
    public WaypointInfo CurrentWaypoint { get; private set; }

    // ★★★ [핵심] 디버그 모드 시작을 위한 새로운 함수 ★★★
    public void SetupDebugStart(int startIndex)
    {
        if (startIndex >= 0 && startIndex < waypoints.Count)
        {
            // 1. 현재 웨이포인트 인덱스를 설정합니다.
            currentWaypointIndex = startIndex;
            CurrentWaypoint = waypoints[startIndex].GetComponent<WaypointInfo>();
            
            // 2. 경로가 이미 시작된 상태라고 알려줍니다. (가장 중요!)
            hasPathStarted = true;
            
            // 3. 시작할 때는 움직이지 않는 상태여야 합니다.
            isMoving = false; 

            Debug.Log($"[디버그] 시작 지점이 {startIndex}번({waypoints[startIndex].name})으로 설정되었고, 경로가 초기화되었습니다.");
        }
        else
        {
            Debug.LogError($"잘못된 웨이포인트 인덱스({startIndex})입니다. 디버그 모드를 설정할 수 없습니다.");
        }
    }

    void Update()
    {
        if (!isMoving || waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count) return;
        
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            HandleArrivalAtWaypoint(targetWaypoint);
        }
    }
    
    private void HandleArrivalAtWaypoint(Transform arrivedWaypoint)
    {
        CurrentWaypoint = arrivedWaypoint.GetComponent<WaypointInfo>();

        if (CurrentWaypoint != null)
        {
            isMoving = false;
            Debug.Log($"'{arrivedWaypoint.name}'에 도착하여 정지합니다. 사용자 입력을 기다립니다.");

            if (CurrentWaypoint.onArrive != null)
            {
                CurrentWaypoint.onArrive.Invoke(CurrentWaypoint.message);
            }
        }
        else
        {
            Debug.Log($"'{arrivedWaypoint.name}' 지점은 WaypointInfo가 없어 통과합니다.");
            MoveToNext();
        }
    }

    private void MoveToNext()
    {
        currentWaypointIndex++;
        if (loop && currentWaypointIndex >= waypoints.Count)
        {
            currentWaypointIndex = 0;
        }
        
        if(currentWaypointIndex < waypoints.Count)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
            Debug.Log("모든 웨이포인트 이동을 완료했습니다.");
        }
    }

    public void ContinueToNextWaypoint()
    {
        // 이제 hasPathStarted가 true이므로 이 조건문이 정상적으로 통과됩니다.
        if (!hasPathStarted || isMoving) return;
        MoveToNext();
    }

    public void StartPath()
    {
        if (hasPathStarted) return;
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogError("경로 이동을 시작할 수 없습니다! Waypoints가 할당되지 않았습니다.", this.gameObject);
            return;
        }
        Debug.Log("경로 이동을 시작합니다.");
        hasPathStarted = true;
        isMoving = true;
    }
}

