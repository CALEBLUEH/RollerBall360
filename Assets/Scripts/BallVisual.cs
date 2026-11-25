using UnityEngine;

public class BallVisual : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;

    [Header("Rolling")]
    public float forwardRollSpeed = 360f;

    [Header("Turning")]
    public float maxTurnAngle = 25f;

    public float turnSmooth = 10f;

    private float rollAngle;
    private float currentTurn;

    private void Update()
    {
        // Constant forward rolling
        rollAngle += forwardRollSpeed * Time.deltaTime;

        // Lean according to player input
        float targetTurn =
            player.currentInput * maxTurnAngle;

        currentTurn = Mathf.Lerp(
            currentTurn,
            targetTurn,
            turnSmooth * Time.deltaTime
        );

        transform.localRotation =
            Quaternion.Euler(
                rollAngle,
                currentTurn,
                0f
            );
    }
}