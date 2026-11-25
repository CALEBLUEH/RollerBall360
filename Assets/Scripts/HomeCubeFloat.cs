using UnityEngine;

public class HomeCubeFloat : MonoBehaviour
{
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 1f;
    public float rotationSpeed = 12f;

    private Camera viewCamera;
    private Vector3 restingWorldPosition;
    private Vector3 rotationAxis;
    private float phase;
    private float minimumScale;
    private float maximumScale;
    private bool isInteracting;

    public void Configure(Camera cameraReference, float minScale, float maxScale)
    {
        viewCamera = cameraReference;
        minimumScale = Mathf.Min(minScale, maxScale);
        maximumScale = Mathf.Max(minScale, maxScale);
        restingWorldPosition = transform.position;

        int seed = Mathf.Abs(GetInstanceID());
        phase = (seed % 628) * 0.01f;
        floatFrequency = 0.75f + (seed % 50) * 0.01f;
        rotationSpeed = 8f + (seed % 10);
        rotationAxis = new Vector3(
            0.2f + (seed % 7) * 0.1f,
            0.5f,
            0.2f + (seed % 5) * 0.1f
        ).normalized;
    }

    private void Update()
    {
        if (isInteracting || viewCamera == null)
            return;

        phase += Time.deltaTime * floatFrequency;
        Vector3 floatDirection = viewCamera.transform.up;
        Vector3 targetPosition = restingWorldPosition +
                                 floatDirection * Mathf.Sin(phase) * floatAmplitude;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            4f * Time.deltaTime
        );
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void BeginInteraction()
    {
        isInteracting = true;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        restingWorldPosition = worldPosition;
    }

    public void AdjustScale(float amount)
    {
        float scale = Mathf.Clamp(
            transform.localScale.x + amount,
            minimumScale,
            maximumScale
        );
        transform.localScale = Vector3.one * scale;
    }

    public void EndInteraction()
    {
        restingWorldPosition = transform.position;
        isInteracting = false;
    }
}
