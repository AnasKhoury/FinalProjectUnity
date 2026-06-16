using UnityEngine;
using UnityEngine.EventSystems;

public sealed class TouchObjectManipulator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 0.25f;
    [SerializeField] private float pinchScaleSpeed = 0.005f;
    [SerializeField] private float minimumScale = 0.15f;
    [SerializeField] private float maximumScale = 2.5f;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void ClearTarget(Transform oldTarget)
    {
        if (target == oldTarget)
        {
            target = null;
        }
    }

    public void RotateTarget(float degrees)
    {
        if (target == null)
        {
            return;
        }

        target.Rotate(Vector3.up, degrees, Space.World);
    }

    public void ScaleTarget(float multiplier)
    {
        if (target == null)
        {
            return;
        }

        float nextScale = Mathf.Clamp(target.localScale.x * multiplier, minimumScale, maximumScale);
        target.localScale = Vector3.one * nextScale;
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                target.Rotate(Vector3.up, -touch.deltaPosition.x * rotationSpeed, Space.World);
                target.Rotate(Vector3.right, touch.deltaPosition.y * rotationSpeed, Space.World);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            if (EventSystem.current != null &&
                (EventSystem.current.IsPointerOverGameObject(first.fingerId) ||
                 EventSystem.current.IsPointerOverGameObject(second.fingerId)))
            {
                return;
            }

            float previousDistance = ((first.position - first.deltaPosition) - (second.position - second.deltaPosition)).magnitude;
            float currentDistance = (first.position - second.position).magnitude;
            float delta = (currentDistance - previousDistance) * pinchScaleSpeed;
            float nextScale = Mathf.Clamp(target.localScale.x + delta, minimumScale, maximumScale);
            target.localScale = Vector3.one * nextScale;
        }
    }
}
