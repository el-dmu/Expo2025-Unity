using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VrWaypointWalker))]
public class VrWaypointWalkerEditor : Editor
{
    private VrWaypointWalker walker;

    private void OnEnable()
    {
        // 스크립트가 활성화될 때 타겟 컴포넌트를 저장해둡니다.
        walker = (VrWaypointWalker)target;
    }

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 UI를 그립니다.
        base.OnInspectorGUI();

        // 인스펙터에 "Add Waypoint" 버튼을 추가합니다. (기존 기능 유지)
        if (GUILayout.Button("Add Waypoint"))
        {
            // Undo 기능을 지원하도록 변경하여, 실수로 추가해도 Ctrl+Z로 되돌릴 수 있습니다.
            Undo.RegisterCompleteObjectUndo(walker, "Add Waypoint");
            walker.AddWaypoint();
        }

        // 사용법 안내를 위한 도움말 상자를 추가합니다.
        EditorGUILayout.HelpBox("씬(Scene) 뷰에서 Shift + 마우스 왼쪽 클릭으로 웨이포인트를 생성할 수 있습니다.", MessageType.Info);
    }

    /// <summary>
    /// 씬 뷰에 커스텀 GUI를 렌더링하는 함수입니다.
    /// </summary>
    // VrWaypointWalkerEditor.cs 파일의 OnSceneGUI 함수를 아래 코드로 수정하세요.

private void OnSceneGUI()
{
    // 현재 이벤트 정보를 가져옵니다.
    Event currentEvent = Event.current;

    // 이벤트가 마우스 클릭이고, Shift 키가 함께 눌렸는지 확인합니다.
    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && currentEvent.shift)
    {
        // ======================= [추가된 코드 1] =======================
        // 현재 GUI 이벤트에 대한 제어 ID를 얻어옵니다.
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        // 마우스 제어권을 현재 컨트롤로 고정시켜 다른 기능(오브젝트 선택 등)이 동작하지 않게 합니다.
        GUIUtility.hotControl = controlID;
        // ===============================================================

        // 기본 동작(예: 오브젝트 선택)을 막습니다. (이 코드도 그대로 둡니다)
        currentEvent.Use();

        // 마우스 위치에 Ray를 쏴서 3D 월드 좌표를 얻습니다.
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        
        // Raycast가 바닥이나 다른 오브젝트에 부딪혔는지 확인합니다.
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            // Undo 기능을 등록합니다.
            Undo.RegisterCompleteObjectUndo(walker, "Add Waypoint with Click");
            
            // 새로운 웨이포인트를 생성하고 위치를 설정합니다.
            walker.AddWaypointAt(hitInfo.point);
        }
        else
        {
            Debug.LogWarning("웨이포인트를 생성할 바닥이나 오브젝트를 찾지 못했습니다. Ray가 허공에 있습니다.");
        }
    }
}
}