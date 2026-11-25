using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class PlayerController : MonoBehaviour
{
    public enum ControlMode
    {
        Tilt,
        Pad
    }

    [Header("Movement")]
    public float rotationSpeed = 360f;

    [Tooltip("Time taken to build up steering input. Smaller values feel more responsive.")]
    [Min(0.01f)]
    public float accelerationTime = 0.12f;

    [Tooltip("Time taken to release steering input. This creates a small amount of inertia.")]
    [Min(0.01f)]
    public float decelerationTime = 0.08f;

    [Header("Controls")]
    public ControlMode controlMode = ControlMode.Tilt;

    [Tooltip("Phone tilt below this amount is ignored to prevent drift.")]
    [Range(0f, 0.5f)]
    public float tiltDeadZone = 0.05f;

    [Tooltip("Acceleration X value that produces full steering (about 30 degrees at 0.5).")]
    [Range(0.1f, 1f)]
    public float tiltFullStrength = 0.5f;

    [HideInInspector]
    public float currentInput;

    private float currentAngle;
    private float inputVelocity;
    private bool gameplayPaused;

    private JoystickController joystick;
    private GameObject joystickRoot;
    private Camera gameplayCamera;
#if UNITY_WEBGL && !UNITY_EDITOR
    // Browser motion sensors are not assumed available; Pad also supports mouse input.
    private static ControlMode savedControlMode = ControlMode.Pad;
#else
    private static ControlMode savedControlMode = ControlMode.Tilt;
#endif

    [Header("Health")]
    [Min(1)]
    public int maximumHealth = 100;

    [Tooltip("Prevents one physical impact from dealing damage multiple times.")]
    [Min(0f)]
    public float damageCooldown = 0.35f;

    public Slider healthBar;

    [Tooltip("Seconds the health bar stays fully visible after taking damage.")]
    [Min(0f)]
    public float healthBarVisibleTime = 1.5f;

    [Tooltip("Seconds used to fade the health bar out.")]
    [Min(0.05f)]
    public float healthBarFadeTime = 0.5f;

    public int CurrentHealth { get; private set; }

    private float nextDamageTime;
    private float healthBarVisibleUntil;
    private CanvasGroup healthBarCanvasGroup;

    private void Awake()
    {
        controlMode = savedControlMode;
        CreateJoystick();
        gameplayCamera = GetComponentInChildren<Camera>();
        CurrentHealth = maximumHealth;
        UpdateHealthBar();
        PrepareHealthBarFade();
        RefreshJoystickVisibility();
    }

    private void Update()
    {
        UpdateHealthBarFade();

        if (gameplayPaused)
            return;

        DetectObstacleTap();

        float targetInput = ReadSteeringInput();
        float smoothTime = Mathf.Abs(targetInput) > Mathf.Abs(currentInput)
            ? accelerationTime
            : decelerationTime;

        currentInput = Mathf.SmoothDamp(
            currentInput,
            targetInput,
            ref inputVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        currentAngle += currentInput * rotationSpeed * Time.deltaTime;

        transform.rotation =
            Quaternion.Euler(0f, 0f, currentAngle);
    }

    public string ToggleControlMode()
    {
        controlMode = controlMode == ControlMode.Tilt
            ? ControlMode.Pad
            : ControlMode.Tilt;

        savedControlMode = controlMode;
        currentInput = 0f;
        inputVelocity = 0f;
        RefreshJoystickVisibility();
        return ControlModeLabel;
    }

    public string ControlModeLabel => controlMode == ControlMode.Tilt ? "Tilt" : "Pad";

    public void SetGameplayPaused(bool paused)
    {
        gameplayPaused = paused;

        if (paused)
        {
            currentInput = 0f;
            inputVelocity = 0f;
            joystick?.ResetInput();
        }

        RefreshJoystickVisibility();
    }

    private float ReadSteeringInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        float keyboardInput = 0f;

        if (Input.GetKey(KeyCode.A))
            keyboardInput -= 1f;

        if (Input.GetKey(KeyCode.D))
            keyboardInput += 1f;

        // Keyboard takes priority, while Unity Remote can still provide acceleration.
        if (!Mathf.Approximately(keyboardInput, 0f))
            return Mathf.Clamp(keyboardInput, -1f, 1f);
#endif

        if (controlMode == ControlMode.Pad)
            return joystick != null ? joystick.Horizontal : 0f;

        return NormalizeTilt(Input.acceleration.x);
    }

    private float NormalizeTilt(float tilt)
    {
        float magnitude = Mathf.Abs(tilt);

        if (magnitude <= tiltDeadZone)
            return 0f;

        float range = Mathf.Max(0.01f, tiltFullStrength - tiltDeadZone);
        float normalized = Mathf.Clamp01((magnitude - tiltDeadZone) / range);
        return Mathf.Sign(tilt) * normalized;
    }

    private void DetectObstacleTap()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                TryDestroyObstacleAt(touch.position);
            }

            return;
        }

        if (Input.GetMouseButtonDown(0) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            TryDestroyObstacleAt(Input.mousePosition);
        }
    }

    private void TryDestroyObstacleAt(Vector2 screenPosition)
    {
        if (gameplayCamera == null)
            return;

        Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            ObstacleBehaviour obstacle = hit.collider.GetComponentInParent<ObstacleBehaviour>();
            if (obstacle == null)
                continue;

            obstacle.DestroyObstacle();
            return;
        }
    }

    private void CreateJoystick()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        joystickRoot = new GameObject(
            "ControllerPad",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(JoystickController)
        );
        joystickRoot.layer = 5;
        joystickRoot.transform.SetParent(canvas.transform, false);

        RectTransform background = joystickRoot.GetComponent<RectTransform>();
        background.anchorMin = new Vector2(0.5f, 0f);
        background.anchorMax = new Vector2(0.5f, 0f);
        background.pivot = new Vector2(0.5f, 0.5f);
        background.anchoredPosition = new Vector2(0f, 200f);
        background.sizeDelta = new Vector2(500f, 500f);

        Image backgroundImage = joystickRoot.GetComponent<Image>();
        backgroundImage.sprite = Resources.Load<Sprite>("UI/analogStickBackground");
        backgroundImage.color = new Color(1f, 1f, 1f, 0.85f);
        backgroundImage.preserveAspect = true;

        GameObject handleObject = new GameObject(
            "Handle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        handleObject.layer = 5;
        handleObject.transform.SetParent(joystickRoot.transform, false);

        RectTransform handle = handleObject.GetComponent<RectTransform>();
        handle.anchorMin = new Vector2(0.5f, 0.5f);
        handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(200f, 200f);

        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.sprite = Resources.Load<Sprite>("UI/analogStick");
        handleImage.preserveAspect = true;
        handleImage.raycastTarget = false;

        joystick = joystickRoot.GetComponent<JoystickController>();
        joystick.Configure(background, handle);
    }

    private void RefreshJoystickVisibility()
    {
        if (joystickRoot != null)
            joystickRoot.SetActive(controlMode == ControlMode.Pad && !gameplayPaused && enabled);
    }

    private void OnDisable()
    {
        if (joystickRoot != null)
            joystickRoot.SetActive(false);
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0 || Time.time < nextDamageTime)
            return false;

        nextDamageTime = Time.time + damageCooldown;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        UpdateHealthBar();
        ShowHealthBar();

        if (CurrentHealth == 0)
            GameManager.Instance?.GameOver();

        return true;
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null)
            return;

        healthBar.minValue = 0f;
        healthBar.maxValue = maximumHealth;
        healthBar.wholeNumbers = true;
        healthBar.value = CurrentHealth;
    }

    private void PrepareHealthBarFade()
    {
        if (healthBar == null)
            return;

        healthBarCanvasGroup = healthBar.GetComponent<CanvasGroup>();
        if (healthBarCanvasGroup == null)
            healthBarCanvasGroup = healthBar.gameObject.AddComponent<CanvasGroup>();

        healthBarCanvasGroup.alpha = 0f;
        healthBarCanvasGroup.interactable = false;
        healthBarCanvasGroup.blocksRaycasts = false;
    }

    private void ShowHealthBar()
    {
        if (healthBarCanvasGroup == null)
            return;

        healthBarCanvasGroup.alpha = 1f;
        healthBarVisibleUntil = Time.unscaledTime + healthBarVisibleTime;
    }

    private void UpdateHealthBarFade()
    {
        if (healthBarCanvasGroup == null || Time.unscaledTime < healthBarVisibleUntil)
            return;

        float fadeSpeed = 1f / Mathf.Max(0.05f, healthBarFadeTime);
        healthBarCanvasGroup.alpha = Mathf.MoveTowards(
            healthBarCanvasGroup.alpha,
            0f,
            fadeSpeed * Time.unscaledDeltaTime
        );
    }
}
