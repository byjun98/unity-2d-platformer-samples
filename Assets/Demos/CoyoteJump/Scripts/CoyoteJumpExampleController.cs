using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class CoyoteJumpExampleController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float jumpVelocity = 11f;
    [SerializeField] private float jumpCutMultiplier = 0.45f;
    [SerializeField] private float fastFallSpeed = 14f;

    [Header("Assist Windows")]
    [SerializeField] private float coyoteTime = 0.14f;
    [SerializeField] private float jumpBufferTime = 0.14f;

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite runSprite;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite fallSprite;

    [Header("Sprite Sheets")]
    [SerializeField] private Texture2D idleSheet;
    [SerializeField] private Texture2D runSheet;
    [SerializeField] private Texture2D jumpSheet;
    [SerializeField] private Texture2D fallSheet;
    [SerializeField] private int animationFrameWidth = 32;
    [SerializeField] private int animationFrameHeight = 32;
    [SerializeField] private float animationPixelsPerUnit = 16f;
    [SerializeField] private float idleFramesPerSecond = 8f;
    [SerializeField] private float runFramesPerSecond = 12f;

    [Header("Scene Feedback")]
    [SerializeField] private TextMesh stateText;
    [SerializeField] private TextMesh coyoteText;
    [SerializeField] private TextMesh bufferText;
    [SerializeField] private Transform coyoteBarFill;
    [SerializeField] private Transform bufferBarFill;

    [Header("Respawn")]
    [SerializeField] private float fallResetY = -6f;
    [SerializeField] private float respawnY = 4.2f;

    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Sprite[] idleFrames;
    private Sprite[] runFrames;
    private Sprite[] jumpFrames;
    private Sprite[] fallFrames;
    private Vector3 startPosition;
    private float moveInput;
    private bool fastFallInput;
    private float coyoteCounter;
    private float bufferCounter;
    private bool grounded;
    private bool lastFacingRight = true;
    private float flashTimer;
    private string flashMessage = "발판 끝에서 살짝 늦게 점프해 보세요.";
    private string currentAnimationState;
    private float animationTimer;
    private int animationFrameIndex;

    private const float FullBarWidth = 2.45f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        body.freezeRotation = true;
        BuildAnimationFrames();

        if (stateText == null)
        {
            stateText = FindText("DemoStateText");
        }

        if (coyoteText == null)
        {
            coyoteText = FindText("CoyoteReadout");
        }

        if (bufferText == null)
        {
            bufferText = FindText("BufferReadout");
        }

        if (coyoteBarFill == null)
        {
            coyoteBarFill = FindTransform("CoyoteBarFill");
        }

        if (bufferBarFill == null)
        {
            bufferBarFill = FindTransform("BufferBarFill");
        }
    }

    private void Update()
    {
        moveInput = ReadMoveInput();
        fastFallInput = ReadFastFallInput();
        grounded = CheckGrounded();

        if (grounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (WasJumpPressedThisFrame())
        {
            bufferCounter = jumpBufferTime;
            flashTimer = 0.18f;
            flashMessage = grounded ? "지상 점프 입력!" : "착지 전 점프 입력 저장!";
        }
        else
        {
            bufferCounter -= Time.deltaTime;
        }

        if (WasJumpReleasedThisFrame() && body.linearVelocity.y > 0f)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y *= jumpCutMultiplier;
            body.linearVelocity = velocity;
        }

        if (bufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();
        }

        flashTimer -= Time.deltaTime;

        if (transform.position.y < fallResetY)
        {
            Respawn();
        }

        UpdateFacing();
        UpdateSprite();
        UpdateFeedback();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        velocity.x = moveInput * runSpeed;

        if (!grounded && fastFallInput)
        {
            velocity.y = Mathf.Min(velocity.y, -fastFallSpeed);
        }

        body.linearVelocity = velocity;
    }

    private void Jump()
    {
        Vector2 velocity = body.linearVelocity;
        velocity.y = jumpVelocity;
        body.linearVelocity = velocity;

        bufferCounter = 0f;
        coyoteCounter = 0f;
        flashTimer = 0.22f;
        flashMessage = grounded ? "일반 점프!" : "코요테 점프 성공!";
    }

    private void Respawn()
    {
        transform.position = new Vector3(startPosition.x, respawnY, startPosition.z);
        body.linearVelocity = Vector2.zero;
        bufferCounter = 0f;
        coyoteCounter = 0f;
        flashTimer = 0.5f;
        flashMessage = "떨어졌습니다. 처음 위치 위 하늘에서 다시 시작!";
    }

    private bool CheckGrounded()
    {
        Bounds bounds = bodyCollider.bounds;
        Vector2 center = new Vector2(bounds.center.x, bounds.min.y - 0.04f);
        Vector2 size = new Vector2(Mathf.Max(0.1f, bounds.size.x - 0.12f), 0.08f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit != null && hit != bodyCollider && !hit.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateFacing()
    {
        if (moveInput > 0.01f)
        {
            lastFacingRight = true;
        }
        else if (moveInput < -0.01f)
        {
            lastFacingRight = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !lastFacingRight;
        }
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite[] frames = idleFrames;
        Sprite fallback = idleSprite;
        float framesPerSecond = idleFramesPerSecond;
        string animationState = "idle";

        if (!grounded)
        {
            if (body.linearVelocity.y >= 0f)
            {
                frames = jumpFrames;
                fallback = jumpSprite;
                animationState = "jump";
            }
            else
            {
                frames = fallFrames;
                fallback = fallSprite;
                animationState = "fall";
            }

            framesPerSecond = 1f;
        }
        else if (Mathf.Abs(moveInput) > 0.01f)
        {
            frames = runFrames;
            fallback = runSprite;
            framesPerSecond = runFramesPerSecond;
            animationState = "run";
        }

        Sprite nextSprite = GetAnimatedSprite(animationState, frames, framesPerSecond, fallback);
        if (nextSprite != null)
        {
            spriteRenderer.sprite = nextSprite;
        }
    }

    private void BuildAnimationFrames()
    {
        idleFrames = BuildFrames(idleSheet);
        runFrames = BuildFrames(runSheet);
        jumpFrames = BuildFrames(jumpSheet);
        fallFrames = BuildFrames(fallSheet);

        idleSprite = FirstOrFallback(idleFrames, idleSprite);
        runSprite = FirstOrFallback(runFrames, runSprite);
        jumpSprite = FirstOrFallback(jumpFrames, jumpSprite);
        fallSprite = FirstOrFallback(fallFrames, fallSprite);
    }

    private Sprite GetAnimatedSprite(string animationState, Sprite[] frames, float framesPerSecond, Sprite fallback)
    {
        if (frames == null || frames.Length == 0)
        {
            return fallback;
        }

        if (currentAnimationState != animationState)
        {
            currentAnimationState = animationState;
            animationTimer = 0f;
            animationFrameIndex = 0;
        }

        if (frames.Length > 1 && framesPerSecond > 0f)
        {
            animationTimer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;

            while (animationTimer >= frameDuration)
            {
                animationTimer -= frameDuration;
                animationFrameIndex = (animationFrameIndex + 1) % frames.Length;
            }
        }

        return frames[Mathf.Clamp(animationFrameIndex, 0, frames.Length - 1)];
    }

    private Sprite[] BuildFrames(Texture2D sheet)
    {
        if (sheet == null || animationFrameWidth <= 0 || animationFrameHeight <= 0)
        {
            return System.Array.Empty<Sprite>();
        }

        int columns = Mathf.Max(1, sheet.width / animationFrameWidth);
        Sprite[] frames = new Sprite[columns];

        for (int i = 0; i < columns; i++)
        {
            Rect rect = new Rect(i * animationFrameWidth, 0f, animationFrameWidth, animationFrameHeight);
            frames[i] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), animationPixelsPerUnit, 0, SpriteMeshType.FullRect);
            frames[i].name = sheet.name + "_" + i.ToString("00");
        }

        return frames;
    }

    private static Sprite FirstOrFallback(Sprite[] frames, Sprite fallback)
    {
        return frames == null || frames.Length == 0 ? fallback : frames[0];
    }

    private void UpdateFeedback()
    {
        float coyotePercent = Mathf.Clamp01(coyoteCounter / coyoteTime);
        float bufferPercent = Mathf.Clamp01(bufferCounter / jumpBufferTime);

        SetBar(coyoteBarFill, coyotePercent);
        SetBar(bufferBarFill, bufferPercent);

        if (coyoteText != null)
        {
            coyoteText.text = "코요테  " + coyotePercent.ToString("0.00");
        }

        if (bufferText != null)
        {
            bufferText.text = "버퍼  " + bufferPercent.ToString("0.00");
        }

        if (stateText != null)
        {
            if (flashTimer > 0f)
            {
                stateText.text = flashMessage;
            }
            else if (grounded)
            {
                stateText.text = "A/D·←/→ 이동  W/↑/스페이스 점프  S/↓ 빠른 낙하";
            }
            else if (coyoteCounter > 0f)
            {
                stateText.text = "코요테 타임 열림: 지금 눌러도 점프됩니다.";
            }
            else if (bufferCounter > 0f)
            {
                stateText.text = "점프 버퍼 저장됨: 착지하면 바로 점프합니다.";
            }
            else
            {
                stateText.text = "공중입니다. 착지 직전에 점프를 눌러 보세요.";
            }
        }
    }

    private static void SetBar(Transform fill, float normalized)
    {
        if (fill == null)
        {
            return;
        }

        float width = Mathf.Max(0.001f, FullBarWidth * Mathf.Clamp01(normalized));
        Vector3 scale = fill.localScale;
        scale.x = width;
        fill.localScale = scale;

        Vector3 position = fill.localPosition;
        position.x = -FullBarWidth * 0.5f + width * 0.5f;
        fill.localPosition = position;
    }

    private static TextMesh FindText(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found == null ? null : found.GetComponent<TextMesh>();
    }

    private static Transform FindTransform(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found == null ? null : found.transform;
    }

    private static float ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float x = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            return x;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        float x = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            x -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            x += 1f;
        }

        return x;
#else
        return 0f;
#endif
    }

    private static bool WasJumpPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            return keyboard.spaceKey.wasPressedThisFrame ||
                   keyboard.wKey.wasPressedThisFrame ||
                   keyboard.upArrowKey.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetButtonDown("Jump") ||
               Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.UpArrow);
#else
        return false;
#endif
    }

    private static bool ReadFastFallInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            return keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#else
        return false;
#endif
    }

    private static bool WasJumpReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            return keyboard.spaceKey.wasReleasedThisFrame ||
                   keyboard.wKey.wasReleasedThisFrame ||
                   keyboard.upArrowKey.wasReleasedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetButtonUp("Jump") ||
               Input.GetKeyUp(KeyCode.Space) ||
               Input.GetKeyUp(KeyCode.W) ||
               Input.GetKeyUp(KeyCode.UpArrow);
#else
        return false;
#endif
    }
}
