using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Home Cube Interaction")]
    [Min(0.01f)]
    public float minimumCubeScale = 0.5f;

    [Min(0.01f)]
    public float maximumCubeScale = 3f;

    [Min(0.0001f)]
    public float pinchSensitivity = 0.005f;

    private Camera menuCamera;
    private Transform cubeContainer;
    private HomeCubeFloat draggedCube;
    private HomeCubeFloat pinchedCube;
    private Plane dragPlane;
    private Vector3 dragOffset;

    private void Start()
    {
        menuCamera = Camera.main;
        GameObject background = GameObject.Find("Background");
        cubeContainer = background != null ? background.transform : null;

        if (cubeContainer == null)
            return;

        foreach (Transform cube in cubeContainer)
        {
            HomeCubeFloat floatingCube = cube.GetComponent<HomeCubeFloat>();
            if (floatingCube == null)
                floatingCube = cube.gameObject.AddComponent<HomeCubeFloat>();

            floatingCube.Configure(menuCamera, minimumCubeScale, maximumCubeScale);
        }
    }

    private void Update()
    {
        if (menuCamera == null || cubeContainer == null)
            return;

        if (Input.touchCount >= 2)
        {
            HandlePinch();
            return;
        }

        EndPinchIfNeeded();

        if (Input.touchCount == 1)
            HandleTouchDrag(Input.GetTouch(0));
        else
            HandleMouseInput();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Endless");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleTouchDrag(Touch touch)
    {
        if (touch.phase == TouchPhase.Began && !IsOverButton(touch.position))
            BeginDrag(touch.position);
        else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) &&
                 draggedCube != null)
            MoveDraggedCube(touch.position);
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            EndDrag();
    }

    private void HandleMouseInput()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0) && !IsOverButton(mousePosition))
            BeginDrag(mousePosition);

        if (Input.GetMouseButton(0) && draggedCube != null)
            MoveDraggedCube(mousePosition);

        if (Input.GetMouseButtonUp(0))
            EndDrag();

        float scroll = Input.mouseScrollDelta.y;
        if (!Mathf.Approximately(scroll, 0f) && !IsOverButton(mousePosition))
        {
            HomeCubeFloat cube = PickCube(mousePosition);
            cube?.AdjustScale(scroll * 0.1f);
        }
    }

    private void HandlePinch()
    {
        Touch first = Input.GetTouch(0);
        Touch second = Input.GetTouch(1);

        if (IsOverButton(first.position) || IsOverButton(second.position))
        {
            EndPinchIfNeeded();
            return;
        }

        Vector2 midpoint = (first.position + second.position) * 0.5f;

        if (pinchedCube == null)
        {
            pinchedCube = draggedCube != null ? draggedCube : PickCube(midpoint);
            if (pinchedCube == null)
                return;

            EndDrag();
            pinchedCube.BeginInteraction();
        }

        Vector2 previousFirst = first.position - first.deltaPosition;
        Vector2 previousSecond = second.position - second.deltaPosition;
        float previousDistance = Vector2.Distance(previousFirst, previousSecond);
        float currentDistance = Vector2.Distance(first.position, second.position);
        pinchedCube.AdjustScale((currentDistance - previousDistance) * pinchSensitivity);
    }

    private void EndPinchIfNeeded()
    {
        if (pinchedCube == null)
            return;

        pinchedCube.EndInteraction();
        pinchedCube = null;
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        draggedCube = PickCube(screenPosition);
        if (draggedCube == null)
            return;

        draggedCube.BeginInteraction();
        dragPlane = new Plane(-menuCamera.transform.forward, draggedCube.transform.position);

        Ray ray = menuCamera.ScreenPointToRay(screenPosition);
        if (dragPlane.Raycast(ray, out float distance))
            dragOffset = draggedCube.transform.position - ray.GetPoint(distance);
        else
            dragOffset = Vector3.zero;
    }

    private void MoveDraggedCube(Vector2 screenPosition)
    {
        Ray ray = menuCamera.ScreenPointToRay(screenPosition);
        if (dragPlane.Raycast(ray, out float distance))
            draggedCube.SetWorldPosition(ray.GetPoint(distance) + dragOffset);
    }

    private void EndDrag()
    {
        if (draggedCube == null)
            return;

        draggedCube.EndInteraction();
        draggedCube = null;
    }

    private HomeCubeFloat PickCube(Vector2 screenPosition)
    {
        Ray ray = menuCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (!hit.transform.IsChildOf(cubeContainer))
                continue;

            return hit.transform.GetComponent<HomeCubeFloat>();
        }

        return null;
    }

    private bool IsOverButton(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponentInParent<Button>() != null)
                return true;
        }

        return false;
    }
}
