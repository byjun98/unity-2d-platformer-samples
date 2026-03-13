# 코요테 타임 + 점프 버퍼 2D 예제 정리

이 문서는 `CoyoteJumpBufferDemo` 씬에서 사용한 기술, 구현 방식, 핵심 코드, 사용 에셋 출처를 쉽게 정리한 문서입니다.

## 결과물

- 데모 씬: `Assets/Demos/CoyoteJump/Scenes/CoyoteJumpBufferDemo.unity`
- 플레이어 컨트롤러: `Assets/Demos/CoyoteJump/Scripts/CoyoteJumpExampleController.cs`
- 씬 자동 생성기: `Assets/Demos/CoyoteJump/Editor/CoyoteJumpDemoSceneBuilder.cs`
- 사용 에셋 폴더: `Assets/Pixel Adventure 1`

## 사용한 기술

### Unity 2D 물리

플레이어는 `Rigidbody2D`와 `BoxCollider2D`를 사용합니다.

- `Rigidbody2D`: 중력, 점프 속도, 좌우 이동 속도를 처리합니다.
- `BoxCollider2D`: 바닥과 충돌하고, 현재 땅에 닿아 있는지 검사하는 기준이 됩니다.
- `Physics2D.OverlapBoxAll`: 플레이어 발밑에 작은 박스를 만들어 바닥 접촉을 확인합니다.

쉽게 말하면, 캐릭터를 직접 좌표로 움직이는 것이 아니라 Unity 2D 물리 엔진에 속도 값을 넣어서 움직이게 만든 구조입니다.

### 코요테 타임

코요테 타임은 발판에서 떨어진 직후 아주 짧은 시간 동안 점프를 허용하는 기술입니다.

플레이어가 발판 끝에서 조금 늦게 점프 버튼을 눌러도 “아직 점프할 수 있는 것처럼” 느끼게 해 줍니다. 액션 게임이나 플랫포머에서 조작감을 부드럽게 만드는 대표적인 기법입니다.

### 점프 버퍼

점프 버퍼는 착지하기 직전에 점프 버튼을 눌렀을 때, 그 입력을 잠깐 저장했다가 착지 순간 바로 점프시키는 기술입니다.

즉, 플레이어가 조금 일찍 점프를 눌러도 입력이 씹히지 않습니다.

### 스프라이트시트 애니메이션

사용한 `Pixel Adventure 1` 에셋의 `Ninja Frog` 캐릭터는 걷기/대기 모션이 한 장의 긴 이미지에 여러 프레임으로 들어 있습니다.

예시:

- `Idle (32x32).png`: 32x32 프레임 11개
- `Run (32x32).png`: 32x32 프레임 12개

컨트롤러에서 이 이미지를 런타임에 32x32 크기로 잘라서 `Sprite` 배열로 만들고, 시간에 맞춰 프레임을 교체합니다.

### 입력 처리

프로젝트 설정에 따라 두 입력 방식을 모두 지원합니다.

- New Input System: `Keyboard.current`
- Legacy Input Manager: `Input.GetKey`

조작:

- `A/D` 또는 `←/→`: 좌우 이동
- `W`, `↑`, `Space`: 점프
- `S` 또는 `↓`: 공중 빠른 낙하

### 리스폰

플레이어가 일정 높이 아래로 떨어지면 처음 위치의 위쪽 하늘에서 다시 시작합니다.

- 낙하 판정: `y < -6`
- 부활 높이: `y = 4.2`

## 핵심 구현

### 1. 땅에 닿았는지 확인

플레이어 콜라이더의 아래쪽에 작은 박스를 만들고, 그 박스와 겹치는 콜라이더가 있으면 바닥에 있다고 판단합니다.

```csharp
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
```

### 2. 코요테 타임

땅에 있으면 `coyoteCounter`를 계속 충전합니다. 공중으로 나가면 시간이 줄어듭니다.

```csharp
if (grounded)
{
    coyoteCounter = coyoteTime;
}
else
{
    coyoteCounter -= Time.deltaTime;
}
```

이 값이 0보다 크면 “방금 발판을 떠난 상태”로 보고 점프를 허용합니다.

### 3. 점프 버퍼

점프 버튼을 누르면 `bufferCounter`를 충전합니다. 시간이 지나면 줄어듭니다.

```csharp
if (WasJumpPressedThisFrame())
{
    bufferCounter = jumpBufferTime;
}
else
{
    bufferCounter -= Time.deltaTime;
}
```

### 4. 코요테 타임 + 점프 버퍼 결합

두 조건이 모두 살아 있으면 점프합니다.

```csharp
if (bufferCounter > 0f && coyoteCounter > 0f)
{
    Jump();
}
```

이 한 줄이 데모의 핵심입니다.

- `bufferCounter > 0f`: 최근에 점프 입력이 있었다.
- `coyoteCounter > 0f`: 현재 점프 가능한 시간 안이다.

그래서 늦게 누른 점프와 일찍 누른 점프를 둘 다 자연스럽게 처리할 수 있습니다.

### 5. 점프 실행

점프할 때는 y축 속도를 점프 속도로 바꾸고, 입력 버퍼와 코요테 타이머를 비웁니다.

```csharp
private void Jump()
{
    Vector2 velocity = body.linearVelocity;
    velocity.y = jumpVelocity;
    body.linearVelocity = velocity;

    bufferCounter = 0f;
    coyoteCounter = 0f;
}
```

### 6. WASD + 방향키 입력

New Input System에서는 `Keyboard.current`를 사용합니다.

```csharp
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
```

Legacy Input Manager에서는 `Input.GetKey`를 사용합니다.

```csharp
float x = 0f;
if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
{
    x -= 1f;
}

if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
{
    x += 1f;
}
```

### 7. 스프라이트시트 프레임 자르기

32x32 스프라이트시트를 가로 방향으로 잘라서 여러 장의 `Sprite`를 만듭니다.

```csharp
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
        frames[i] = Sprite.Create(
            sheet,
            rect,
            new Vector2(0.5f, 0.5f),
            animationPixelsPerUnit,
            0,
            SpriteMeshType.FullRect
        );
    }

    return frames;
}
```

### 8. 상태에 맞게 애니메이션 선택

플레이어 상태에 따라 재생할 프레임 배열을 고릅니다.

```csharp
if (!grounded)
{
    if (body.linearVelocity.y >= 0f)
    {
        frames = jumpFrames;
        animationState = "jump";
    }
    else
    {
        frames = fallFrames;
        animationState = "fall";
    }
}
else if (Mathf.Abs(moveInput) > 0.01f)
{
    frames = runFrames;
    animationState = "run";
}
else
{
    frames = idleFrames;
    animationState = "idle";
}
```

### 9. 떨어졌을 때 부활

플레이어가 화면 아래로 떨어지면 시작 x 위치의 하늘 높이로 이동시킵니다.

```csharp
private void Respawn()
{
    transform.position = new Vector3(startPosition.x, respawnY, startPosition.z);
    body.linearVelocity = Vector2.zero;
    bufferCounter = 0f;
    coyoteCounter = 0f;
}
```

## 씬 자동 생성 방식

씬은 `CoyoteJumpDemoSceneBuilder.cs`에서 자동으로 만듭니다.

사용한 Unity Editor API:

- `EditorSceneManager.NewScene`: 새 씬 생성
- `EditorSceneManager.SaveScene`: 씬 저장
- `AssetDatabase.LoadAssetAtPath`: 에셋 로드
- `TextureImporter`: Pixel Adventure 에셋을 2D 스프라이트로 설정
- `SerializedObject`: 생성한 플레이어 컨트롤러에 에셋 참조 연결

씬 생성 메뉴:

```text
Tools/Coyote Jump/Create Demo Scene
```

이 메뉴를 실행하면 데모 씬이 다시 만들어집니다.

## 사용된 에셋 출처

### Pixel Adventure 1

- 에셋명: `Pixel Adventure 1`
- 제작자: `Pixel Frog`
- 출처: Unity Asset Store
- 사용 방식: 이 PC에 다운로드되어 있던 Unity Asset Store 캐시의 `.unitypackage`를 프로젝트에 임포트해서 사용
- 프로젝트 내 위치: `Assets/Pixel Adventure 1`

로컬 캐시 원본 경로:

```text
C:\Users\SSAFY\AppData\Roaming\Unity\Asset Store-5.x\Pixel Frog\Textures Materials2D Characters\Pixel Adventure 1.unitypackage
```

사용한 주요 파일:

```text
Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Idle (32x32).png
Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Run (32x32).png
Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Jump (32x32).png
Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Fall (32x32).png
Assets/Pixel Adventure 1/Assets/Terrain/Terrain Sliced (16x16).png
Assets/Pixel Adventure 1/Assets/Items/Checkpoints/Start/Start (Idle).png
Assets/Pixel Adventure 1/Assets/Items/Checkpoints/End/End (Idle).png
Assets/Pixel Adventure 1/Assets/Items/Fruits/Apple.png
Assets/Pixel Adventure 1/Assets/Background/Blue.png
```

## 쉽게 요약

이 예제는 “플랫포머 점프가 왜 부드럽게 느껴지는지”를 보여주는 샘플입니다.

일반 점프만 있으면 버튼을 아주 정확한 순간에 눌러야 해서 답답합니다. 그래서 실전 게임에서는 보통 두 가지 보정이 들어갑니다.

1. 코요테 타임: 발판에서 막 떨어져도 잠깐 점프 가능
2. 점프 버퍼: 착지 직전에 누른 점프를 잠깐 저장

이 두 기능을 합치면 플레이어가 조금 늦거나 조금 빨리 눌러도 게임이 자연스럽게 받아 줍니다. 즉, 입력 판정을 엄격하게 만들기보다 사람이 조작하는 감각에 맞춰 약간 여유를 주는 기술입니다.

이번 씬은 그 차이를 눈으로 볼 수 있게 코요테 게이지와 버퍼 게이지를 화면에 표시하고, 발판 사이의 틈에서 직접 테스트할 수 있게 만든 예제입니다.
