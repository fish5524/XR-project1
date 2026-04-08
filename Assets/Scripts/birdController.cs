/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && UNITY_NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls the player's movement in virtual reality.
/// </summary>
/// <remarks>
/// Specifically, this class builds on the functionality of Unity's built-in CharacterController type to
/// permit movement paradigms suitable for virtual reality, such as teleportation. These capabilities are
/// leveraged by <see cref="OVRSceneSampleController"/> to control functionality in the sample.
/// </remarks>
[RequireComponent(typeof(CharacterController))]
[HelpURL("https://developer.oculus.com/documentation/unity/unity-sf-distancegrab/")]
public class birdController : MonoBehaviour
{
    /// <summary>
    /// Controls the rate of acceleration during continuous movement (as opposed to teleportation).
    /// </summary>
    /// <remarks>
    /// Well-tuned movement settings in VR play a significant role in user comfort and should be adjusted
    /// carefully.  For more information, see
    /// [Locomotion Best Practices](https://developer.oculus.com/resources/locomotion-design-techniques-best-practices/).
    /// </remarks>
    public float Acceleration = 0.1f;

    /// <summary>
    /// Controls the rate of damping during continuous movement (as opposed to teleportation).
    /// </summary>
    /// <remarks>
    /// Well-tuned movement settings in VR play a significant role in user comfort and should be adjusted
    /// carefully.
    /// </remarks>
    public float Damping = 0.3f;

    /// <summary>
    /// Controls the rate of additional damping when moving sideways or backwards during continuous
    /// movement (as opposed to teleportation).
    /// </summary>
    /// <remarks>
    /// Well-tuned movement settings in VR play a significant role in user comfort and should be adjusted
    /// carefully.
    /// </remarks>
    public float BackAndSideDampen = 0.5f;

    /// <summary>
    /// Controls the force applied to the character when jumping.
    /// </summary>
    /// <remarks>
    /// Well-tuned movement settings in VR play a significant role in user comfort and should be adjusted
    /// carefully.
    /// </remarks>
    public float JumpForce = 0.3f;

    /// <summary>
    /// Controls the rate of rotation when continuous-rotating (as opposed to snap-rotating) using a gamepad.
    /// If <see cref="SnapRotation"/> is enabled, <see cref="RotationRatchet"/> is used instead.
    /// </summary>
    /// <remarks>
    /// Well-tuned movement settings in VR play a significant role in user comfort and should be adjusted
    /// carefully.
    /// </remarks>
    public float RotationAmount = 1.5f;

    /// <summary>
    /// Controls the rate of rotation when turning using the keyboard.
    /// </summary>
    /// <remarks>
    /// Well-tuned movement settings in VR play a significant role in user comfort and should be adjusted
    /// carefully.
    /// </remarks>
    public float RotationRatchet = 45.0f;

    /// <summary>
    /// Determines whether rotation will be applied in a single large "snap" (snap-rotating) as opposed
    /// to more slowly over time (continuous-rotating).
    /// </summary>
    [Tooltip("The player will rotate in fixed steps if Snap Rotation is enabled.")]
    public bool SnapRotation = true;

    /// <summary>
    /// [Deprecated] When enabled, snap rotation will happen about the guardian rather than the
    /// player/camera viewpoint. This (legacy) option should be left off except for edge cases that
    /// require extreme behavioral backwards compatibility.
    /// </summary>
    [Obsolete]
    [Tooltip("[Deprecated] When enabled, snap rotation will happen about the center of the " +
             "guardian rather than the center of the player/camera viewpoint. This (legacy) " +
             "option should be left off except for edge cases that require extreme behavioral " +
             "backwards compatibility.")]
    public bool RotateAroundGuardianCenter = false;

    /// <summary>
    /// Sets the number of discrete speeds that will be used in continuous motion. If 0, motion speed is
    /// not discretized.
    /// </summary>
    /// <remarks>
    /// Well-tuned movement settings in VR play a significant role in user comfort and should be adjusted
    /// carefully.
    /// </remarks>
    [Tooltip("Sets the number of discrete speeds that will be used in continuous motion. If 0, motion " +
        "speed is not discretized.")]
    public int FixedSpeedSteps;

    /// <summary>
    /// If true, reset the initial yaw of the player controller when the Hmd pose is recentered.
    /// </summary>
    /// <remarks>
    /// Enabling this feature allows users to change the real-to-virtual spatial correspondence with
    /// respect to orientation --- changing how "north" in the virtual world corresponds with "north"
    /// in the real world, for example.
    /// </remarks>
    public bool HmdResetsY = true;

    /// <summary>
    /// If true, tracking data from a child OVRCameraRig will update the direction of movement. This
    /// capability is sometimes referred to as "head-oriented movement."
    /// </summary>
    public bool HmdRotatesY = true;

    /// <summary>
    /// Modifies the strength of gravity.
    /// </summary>
    /// <remarks>
    /// This value does not _set_ gravity, but merely modifies it. The underlying value used for gravity
    /// is retrieved from Unity's Physics system.
    /// </remarks>
    public float GravityModifier = 0.379f;

    /// <summary>
    /// If true, each OVRPlayerController will use the player's physical height.
    /// </summary>
    public bool useProfileData = true;

    /// <summary>
    /// The CameraHeight is the actual height of the HMD and can be used to adjust the height of the
    /// character controller, which will affect the ability of the character to move into areas with a low
    /// ceiling.
    /// </summary>
    [NonSerialized]
    public float CameraHeight;

    /// <summary>
    /// This event is raised after the character controller is moved. This is used by the OVRAvatarLocomotion
    /// script to keep the avatar transform synchronized with the OVRPlayerController.
    /// </summary>
    public event Action<Transform> TransformUpdated;

    /// <summary>
    /// This bool is set to true whenever the player controller has been teleported. It is reset after every
    /// frame. Some systems, such as CharacterCameraConstraint, test this boolean in order to disable logic
    /// that moves the character controller immediately following the teleport.
    /// </summary>
    [NonSerialized] // This doesn't need to be visible in the inspector.
    public bool Teleported;

    /// <summary>
    /// This event is raised immediately after the camera transform has been updated, but before movement is
    /// updated.
    /// </summary>
    public event Action CameraUpdated;

    /// <summary>
    /// This event is raised right before the character controller is actually moved in order to provide
    /// other systems the opportunity to move the character controller in response to things other than user
    /// input, such as movement of the HMD. See CharacterCameraConstraint.cs for an example of this.
    /// </summary>
    public event Action PreCharacterMove;

    /// <summary>
    /// When true, user input will be applied to linear movement. Set this to false whenever the player
    /// controller needs to ignore input for linear movement.
    /// </summary>
    public bool EnableLinearMovement = true;

    /// <summary>
    /// When true, user input will be applied to rotation. Set this to false whenever the player controller
    /// needs to ignore input for rotation.
    /// </summary>
    public bool EnableRotation = true;

    /// <summary>
    /// Rotation defaults to secondary thumbstick. You can allow either here. Note that this won't behave
    /// well if EnableLinearMovement is true.
    /// </summary>
    public bool RotationEitherThumbstick = false;

    public bool isJumpGame = false;
    public bool isFlyGame = false;

    protected CharacterController Controller = null;
    protected OVRCameraRig CameraRig = null;

    private float MoveScale = 1.0f;
    private Vector3 MoveThrottle = Vector3.zero;
    private float FallSpeed = 0.0f;
    private OVRPose? InitialPose;
    public float InitialYRotation { get; private set; }
    private float MoveScaleMultiplier = 1.0f;
    private float RotationScaleMultiplier = 1.0f;

    // It is rare to want to use mouse movement in VR, so ignore the mouse by default.
    private bool SkipMouseRotation = true;

    private bool HaltUpdateMovement = false;
    private bool prevHatLeft = false;
    private bool prevHatRight = false;
    private float SimulationRate = 60f;
    private float buttonRotation = 0f;

    // Set to true when a snap turn has occurred, code requires one frame of centered thumbstick to enable another snap turn.
    private bool ReadyToSnapTurn;

    private bool playerControllerEnabled = false;

    public float ForwardForce = 2.0f;
    public float JumpCooldown = 1.0f;
    private float lastJumpTime = -999f;
    private float currentModifier;

    [Header("身高觸發設定")]
    public float jumpThreshold = 0.01f;    // 瞬間站起超過 30 公分就觸發
    public float minHeightToTrigger = 0.5f; // 確保不是在地上爬的時候觸發
    private float lastHeaderHeight;        // 記錄上一幀的高度

    [Header("飛行設定")]
    public float flapForce = 0.01f;    // 每次按空白鍵給予的向上推力
    public float maxFlySpeed = 0.02f;  // 限制向上飛行的最高速度，防止衝太快

    [Header("動畫設定")]
    public Animator birdAnimator; 
    public float minAnimSpeed = 0.5f; // 最慢拍動速度
    public float maxAnimSpeed = 2.5f; // 最快拍動速度
    [Header("動畫狀態名稱")]
    public string flyingStateName = "AmazonMacaw_Rig:ParrotAnimated|Parrot_Flap";

    [Header("手把揮動設定")]
    public float swingThreshold = 2f;    // 揮動速度門檻（單位：公尺/秒）
    public float swingCooldown = 1f;     // 兩次揮動間的冷卻時間，避免觸發過快
    private float lastSwingTime;
    private float maxSpeedInCurrentSwing = 0f; // 記錄本次揮動中的最高速
    private bool isTrackingSwing = false;      // 是否正在追蹤一次有效的揮動

    // Input Actions for new input system
#if ENABLE_INPUT_SYSTEM && UNITY_NEW_INPUT_SYSTEM_INSTALLED
    private InputAction moveForwardAction;
    private InputAction moveLeftAction;
    private InputAction moveRightAction;
    private InputAction moveBackAction;
    private InputAction runAction;
#endif

    void Start()
    {
        // Add eye-depth as a camera offset from the player controller
        var p = CameraRig.transform.localPosition;
        p.z = OVRManager.profile.eyeDepth;
        CameraRig.transform.localPosition = p;

#if ENABLE_INPUT_SYSTEM && UNITY_NEW_INPUT_SYSTEM_INSTALLED
        moveForwardAction = new InputAction(binding: "<Keyboard>/w");
        moveForwardAction.AddBinding("<Keyboard>/upArrow");
        moveLeftAction = new InputAction(binding: "<Keyboard>/a");
        moveLeftAction.AddBinding("<Keyboard>/leftArrow");
        moveRightAction = new InputAction(binding: "<Keyboard>/d");
        moveRightAction.AddBinding("<Keyboard>/rightArrow");
        moveBackAction = new InputAction(binding: "<Keyboard>/s");
        moveBackAction.AddBinding("<Keyboard>/downArrow");
        runAction = new InputAction(binding: "<Keyboard>/leftShift");
        runAction.AddBinding("<Keyboard>/rightShift");

        moveForwardAction.Enable();
        moveLeftAction.Enable();
        moveRightAction.Enable();
        moveBackAction.Enable();
        runAction.Enable();
#endif
    }

    void Awake()
    {
        Controller = gameObject.GetComponent<CharacterController>();

        if (Controller == null)
            Debug.LogWarning("OVRPlayerController: No CharacterController attached.");

        // We use OVRCameraRig to set rotations to cameras,
        // and to be influenced by rotation
        OVRCameraRig[] CameraRigs = gameObject.GetComponentsInChildren<OVRCameraRig>();

        if (CameraRigs.Length == 0)
            Debug.LogWarning("OVRPlayerController: No OVRCameraRig attached.");
        else if (CameraRigs.Length > 1)
            Debug.LogWarning("OVRPlayerController: More then 1 OVRCameraRig attached.");
        else
            CameraRig = CameraRigs[0];

        InitialYRotation = transform.rotation.eulerAngles.y;
    }

    void OnEnable()
    {
    }

    void OnDisable()
    {
        if (playerControllerEnabled)
        {
            OVRManager.display.RecenteredPose -= ResetOrientation;

            if (CameraRig != null)
            {
                CameraRig.UpdatedAnchors -= UpdateTransform;
            }

            playerControllerEnabled = false;
        }

#if ENABLE_INPUT_SYSTEM && UNITY_NEW_INPUT_SYSTEM_INSTALLED
        moveForwardAction.Disable();
        moveLeftAction.Disable();
        moveRightAction.Disable();
        moveBackAction.Disable();
        runAction.Disable();
#endif
    }

    void Update()
    {
        if (!playerControllerEnabled)
        {
            if (OVRManager.OVRManagerinitialized)
            {
                OVRManager.display.RecenteredPose += ResetOrientation;

                if (CameraRig != null)
                {
                    CameraRig.UpdatedAnchors += UpdateTransform;
                }

                playerControllerEnabled = true;
            }
            else
                return;
        }

        // 如果角色控制器存在且处于 kinematic 模式，则跳过输入处理
        Rigidbody rb = GetComponent<Rigidbody>(); 
        if (rb == null) rb = GetComponentInParent<Rigidbody>();

        if (rb == null || rb.isKinematic) 
        {
            // 雖然不給跳，但這裡不要 return！
            // 讓 Update 繼續跑下去，確保 OVR 內部的運算維持活絡
        }
        else if (isJumpGame)
        {
            // 只有非 Kinematic 狀態才偵測跳躍
            float currentHeight = CameraRig.centerEyeAnchor.localPosition.y;
            float heightVelocity = currentHeight - lastHeaderHeight;
            
            if (OVRInput.GetDown(OVRInput.RawButton.A)) Jump_AND_Forward();
            // if (Input.GetKeyDown(KeyCode.Space)) Jump_AND_Forward();
            // Debug.Log($"[JumpDebug] 遊戲狀態:{isJumpGame} | 速度:{heightVelocity:F2} | 當前高度:{currentHeight:F2} | 觸發:{(isJumpGame && heightVelocity > jumpThreshold)}");
            if (heightVelocity > jumpThreshold)
            {
                Debug.Log("[Bird] 偵測到快速站起，觸發跳躍！");
                Jump_AND_Forward();
            }
            lastHeaderHeight = currentHeight;
        }
        else if (isFlyGame)
        {
            // --- 1. 偵測手把揮動 ---
            // 取得左右手把的世界座標速度
            Vector3 leftVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
            Vector3 rightVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);

            // 我們主要看垂直方向的速度（向下揮，所以 y 是負值）
            // 使用 -y 來讓它變成正數方便比較
            float leftSwingSpeed = -leftVel.y;
            float rightSwingSpeed = -rightVel.y;

            // 判斷是否超過門檻且冷卻時間已過
            float currentSpeed = Mathf.Max(leftSwingSpeed, rightSwingSpeed);

            // 1. 開始追蹤：速度超過門檻，且目前還沒觸發過
            if (currentSpeed > swingThreshold && !isTrackingSwing)
            {
                if (Time.time - lastSwingTime > swingCooldown)
                {
                    isTrackingSwing = true;
                    maxSpeedInCurrentSwing = currentSpeed;
                }
            }

            // 2. 追蹤中：更新峰值
            if (isTrackingSwing)
            {
                if (currentSpeed >= maxSpeedInCurrentSwing)
                {
                    // 速度還在上升，更新目前看到的最高速
                    maxSpeedInCurrentSwing = currentSpeed;
                }
                else
                {
                    // 關鍵點：速度開始下降了！代表剛才那一幀就是「峰值」
                    TriggerPeakFly(maxSpeedInCurrentSwing);
                    
                    // 觸發後重置狀態，直到速度降下來
                    isTrackingSwing = false;
                    maxSpeedInCurrentSwing = 0f;
                    lastSwingTime = Time.time;
                }
            }

            // 3. 安全重置：如果速度降得很低，確保狀態是乾淨的
            // 防止玩家手抖動導致 isTrackingSwing 沒被重置
            if (currentSpeed < swingThreshold * 0.5f)
            {
                isTrackingSwing = false;
                maxSpeedInCurrentSwing = 0f;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Fly();
            }
            if (OVRInput.GetDown(OVRInput.RawButton.B))
            {
                Fly();
            }
        }
        if (isFlyGame && birdAnimator != null)
        {
            // 1. 取得當前速度並計算動畫倍率
            float currentSpeed = MoveThrottle.magnitude;
            // 這裡確保即使速度很慢，翅膀也會以 minAnimSpeed 緩慢拍動
            float animSpeed = Mathf.Lerp(minAnimSpeed, maxAnimSpeed, currentSpeed / maxFlySpeed);

            // 2. 判定是否在空中 (離地)
            // 使用 Controller.isGrounded 或是高度門檻來判定
            bool isAirborne = !Controller.isGrounded || transform.position.y > 0.9f;

            if (isAirborne)
            {
                birdAnimator.speed = animSpeed;

                // 3. 取得目前第一層 (Layer 0) 的動畫狀態資訊
                AnimatorStateInfo stateInfo = birdAnimator.GetCurrentAnimatorStateInfo(0);

                // 4. 【核心邏輯】檢查是否正在播放指定動畫
                if (stateInfo.IsName(flyingStateName))
                {
                    // 如果播放進度超過 99% (0.99f)，強制重頭播放
                    // normalizedTime 會隨循環累加，所以用 % 1 取小數部分
                    if (stateInfo.normalizedTime % 1f >= 0.99f)
                    {
                        // Play 參數：(動畫名, 層級, 開始時間點)
                        birdAnimator.Play(flyingStateName, 0, 0f);
                    }
                }
                else
                {
                    // 如果目前不是在播飛行動畫，就切換過去
                    birdAnimator.Play(flyingStateName);
                }
            }
            else
            {
                // 落地停止
                // birdAnimator.speed = 0;
                birdAnimator.Play("AmazonMacaw_Rig:ParrotAnimated|Parrot_Idle");
            }
            // Debug.Log($"高度: {transform.position.y}");
        }
        //todo: enable for Unity Input System
#if ENABLE_LEGACY_INPUT_MANAGER

        //Use keys to ratchet rotation
        if (Input.GetKeyDown(KeyCode.Q))
            buttonRotation -= RotationRatchet;

        if (Input.GetKeyDown(KeyCode.E))
            buttonRotation += RotationRatchet;
#endif
    }

    protected virtual void UpdateController()
    {
        if (useProfileData)
        {
            if (InitialPose == null)
            {
                // Save the initial pose so it can be recovered if useProfileData
                // is turned off later.
                InitialPose = new OVRPose()
                {
                    position = CameraRig.transform.localPosition,
                    orientation = CameraRig.transform.localRotation
                };
            }

            var p = CameraRig.transform.localPosition;
            if (OVRManager.instance.trackingOriginType == OVRManager.TrackingOrigin.EyeLevel)
            {
                p.y = OVRManager.profile.eyeHeight - (0.5f * Controller.height) + Controller.center.y;
            }
            else if (OVRManager.instance.trackingOriginType == OVRManager.TrackingOrigin.FloorLevel)
            {
                p.y = -(0.5f * Controller.height) + Controller.center.y;
            }

            CameraRig.transform.localPosition = p;
        }
        else if (InitialPose != null)
        {
            // Return to the initial pose if useProfileData was turned off at runtime
            CameraRig.transform.localPosition = InitialPose.Value.position;
            CameraRig.transform.localRotation = InitialPose.Value.orientation;
            InitialPose = null;
        }

        CameraHeight = CameraRig.centerEyeAnchor.localPosition.y;

        if (CameraUpdated != null)
        {
            CameraUpdated();
        }

        UpdateMovement();

        Vector3 moveDirection = Vector3.zero;

        float motorDamp = (1.0f + (Damping * SimulationRate * Time.deltaTime));

        MoveThrottle.x /= motorDamp;
        MoveThrottle.y = (MoveThrottle.y > 0.0f) ? (MoveThrottle.y / motorDamp) : MoveThrottle.y;
        MoveThrottle.z /= motorDamp;

        moveDirection += MoveThrottle * SimulationRate * Time.deltaTime;

        // Gravity
        Rigidbody rb = GetComponent<Rigidbody>(); 
        if (rb == null) rb = GetComponentInParent<Rigidbody>();

        if (rb == null || rb.isKinematic)
        {
            currentModifier = 0;
        }
        else if (isFlyGame)
        {
            currentModifier = (FallSpeed <= 0) ? (GravityModifier *  0.002f) : GravityModifier;
        }
        else
        {
            currentModifier = (FallSpeed <= 0) ? (GravityModifier *  1.0f) : GravityModifier;
        }

        if (Controller.isGrounded && FallSpeed <= 0)
            FallSpeed = ((Physics.gravity.y * (currentModifier * 0.002f)));
        else
            FallSpeed += ((Physics.gravity.y * (currentModifier * 0.002f)) * SimulationRate * Time.deltaTime);

        moveDirection.y += FallSpeed * SimulationRate * Time.deltaTime;


        if (Controller.isGrounded && MoveThrottle.y <= transform.lossyScale.y * 0.001f)
        {
            // Offset correction for uneven ground
            float bumpUpOffset = Mathf.Max(Controller.stepOffset,
                new Vector3(moveDirection.x, 0, moveDirection.z).magnitude);
            moveDirection -= bumpUpOffset * Vector3.up;
        }

        if (PreCharacterMove != null)
        {
            PreCharacterMove();
            Teleported = false;
        }

        Vector3 predictedXZ = Vector3.Scale((Controller.transform.localPosition + moveDirection), new Vector3(1, 0, 1));

        // Move contoller
        Controller.Move(moveDirection);
        Vector3 actualXZ = Vector3.Scale(Controller.transform.localPosition, new Vector3(1, 0, 1));

        if (predictedXZ != actualXZ)
            MoveThrottle += (actualXZ - predictedXZ) / (SimulationRate * Time.deltaTime);
    }

    /// <summary>
    /// Performs the movement updates for this character controller. This is a core part of the OVRPlayerController's
    /// update logic and should not be invoked directly.
    /// </summary>
    public virtual void UpdateMovement()
    {
        //todo: enable for Unity Input System
        if (HaltUpdateMovement)
            return;

        if (EnableLinearMovement)
        {
            bool moveForward = false;
            bool moveLeft = false;
            bool moveRight = false;
            bool moveBack = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            moveForward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            moveLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            moveRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            moveBack = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#else
#if UNITY_NEW_INPUT_SYSTEM_INSTALLED
            moveForward = moveForwardAction.phase == InputActionPhase.Started;
            moveLeft = moveLeftAction.phase == InputActionPhase.Started;
            moveRight = moveRightAction.phase == InputActionPhase.Started;
            moveBack = moveBackAction.phase == InputActionPhase.Started;
#endif
#endif

            bool dpad_move = false;

            if (OVRInput.Get(OVRInput.Button.DpadUp))
            {
                moveForward = true;
                dpad_move = true;
            }

            if (OVRInput.Get(OVRInput.Button.DpadDown))
            {
                moveBack = true;
                dpad_move = true;
            }

            MoveScale = 1.0f;

            if ((moveForward && moveLeft) || (moveForward && moveRight) ||
                (moveBack && moveLeft) || (moveBack && moveRight))
                MoveScale = 0.70710678f;

            // No positional movement if we are in the air
            if (!Controller.isGrounded)
                MoveScale = 0.0f;

            MoveScale *= SimulationRate * Time.deltaTime;

            // Compute this for key movement
            float moveInfluence = Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;

            // Run!
#if ENABLE_LEGACY_INPUT_MANAGER
            if (dpad_move || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                moveInfluence *= 2.0f;
#else
#if UNITY_NEW_INPUT_SYSTEM_INSTALLED
            if (dpad_move || runAction.phase == InputActionPhase.Started) {
                moveInfluence *= 2.0f;
            }
#endif
#endif

            Quaternion ort = transform.rotation;
            Vector3 ortEuler = ort.eulerAngles;
            ortEuler.z = ortEuler.x = 0f;
            ort = Quaternion.Euler(ortEuler);

            if (moveForward)
                MoveThrottle += ort * (transform.lossyScale.z * moveInfluence * Vector3.forward);
            if (moveBack)
                MoveThrottle += ort * (transform.lossyScale.z * moveInfluence * BackAndSideDampen * Vector3.back);
            if (moveLeft)
                MoveThrottle += ort * (transform.lossyScale.x * moveInfluence * BackAndSideDampen * Vector3.left);
            if (moveRight)
                MoveThrottle += ort * (transform.lossyScale.x * moveInfluence * BackAndSideDampen * Vector3.right);

            moveInfluence = Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;

#if !UNITY_ANDROID // LeftTrigger not avail on Android game pad
            moveInfluence *= 1.0f + OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
#endif

            Vector2 primaryAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            // If speed quantization is enabled, adjust the input to the number of fixed speed steps.
            if (FixedSpeedSteps > 0)
            {
                primaryAxis.y = Mathf.Round(primaryAxis.y * FixedSpeedSteps) / FixedSpeedSteps;
                primaryAxis.x = Mathf.Round(primaryAxis.x * FixedSpeedSteps) / FixedSpeedSteps;
            }

            if (primaryAxis.y > 0.0f)
                MoveThrottle += ort * (primaryAxis.y * transform.lossyScale.z * moveInfluence * Vector3.forward);

            if (primaryAxis.y < 0.0f)
                MoveThrottle += ort * (Mathf.Abs(primaryAxis.y) * transform.lossyScale.z * moveInfluence *
                                       BackAndSideDampen * Vector3.back);

            if (primaryAxis.x < 0.0f)
                MoveThrottle += ort * (Mathf.Abs(primaryAxis.x) * transform.lossyScale.x * moveInfluence *
                                       BackAndSideDampen * Vector3.left);

            if (primaryAxis.x > 0.0f)
                MoveThrottle += ort * (primaryAxis.x * transform.lossyScale.x * moveInfluence * BackAndSideDampen *
                                       Vector3.right);
        }

        if (EnableRotation)
        {
#pragma warning disable CS0612 // Type of member is obsolete
            Vector3 euler = RotateAroundGuardianCenter ? transform.rotation.eulerAngles : Vector3.zero;
#pragma warning restore CS0612
            float rotateInfluence = SimulationRate * Time.deltaTime * RotationAmount * RotationScaleMultiplier;

            bool curHatLeft = OVRInput.Get(OVRInput.Button.PrimaryShoulder);

            if (curHatLeft && !prevHatLeft)
                euler.y -= RotationRatchet;

            prevHatLeft = curHatLeft;

            bool curHatRight = OVRInput.Get(OVRInput.Button.SecondaryShoulder);

            if (curHatRight && !prevHatRight)
                euler.y += RotationRatchet;

            prevHatRight = curHatRight;

            euler.y += buttonRotation;
            buttonRotation = 0f;


#if !UNITY_ANDROID || UNITY_EDITOR
            if (!SkipMouseRotation)
                euler.y += Input.GetAxis("Mouse X") * rotateInfluence * 3.25f;
#endif

            if (SnapRotation)
            {
                if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickLeft) ||
                    (RotationEitherThumbstick && OVRInput.Get(OVRInput.Button.PrimaryThumbstickLeft)))
                {
                    if (ReadyToSnapTurn)
                    {
                        euler.y -= RotationRatchet;
                        ReadyToSnapTurn = false;
                    }
                }
                else if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickRight) ||
                         (RotationEitherThumbstick && OVRInput.Get(OVRInput.Button.PrimaryThumbstickRight)))
                {
                    if (ReadyToSnapTurn)
                    {
                        euler.y += RotationRatchet;
                        ReadyToSnapTurn = false;
                    }
                }
                else
                {
                    ReadyToSnapTurn = true;
                }
            }
            else
            {
                Vector2 secondaryAxis = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                if (RotationEitherThumbstick)
                {
                    Vector2 altSecondaryAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                    if (secondaryAxis.sqrMagnitude < altSecondaryAxis.sqrMagnitude)
                    {
                        secondaryAxis = altSecondaryAxis;
                    }
                }

                euler.y += secondaryAxis.x * rotateInfluence;
            }

#pragma warning disable CS0612 // Type of member is obsolete
            if (RotateAroundGuardianCenter)
#pragma warning restore CS0612
            {
                transform.rotation = Quaternion.Euler(euler);
            }
            else
            {
                transform.RotateAround(transform.position, Vector3.up, euler.y);
            }
        }
    }


    /// <summary>
    /// Invoked by <see cref="OVRCameraRig"/>'s UpdatedAnchors callback. Allows the Hmd rotation to
    /// update the facing direction of the player (assuming <see cref="HmdRotatesY"/> is enabled).
    /// </summary>
    public void UpdateTransform(OVRCameraRig rig)
    {
        Transform root = CameraRig.trackingSpace;
        Transform centerEye = CameraRig.centerEyeAnchor;

        if (HmdRotatesY && !Teleported)
        {
            Vector3 prevPos = root.position;
            Quaternion prevRot = root.rotation;

            transform.rotation = Quaternion.Euler(0.0f, centerEye.rotation.eulerAngles.y, 0.0f);

            root.position = prevPos;
            root.rotation = prevRot;
        }

        UpdateController();
        if (TransformUpdated != null)
        {
            TransformUpdated(root);
        }
    }

    /// <summary>
    /// Causes the player to jump, unless the Unity Controller associated with this instance has its
    /// isGrounded property set to true.
    /// </summary>
    //public bool Jump()
    //{
    //    if (!Controller.isGrounded)
    //        return false;

    //    MoveThrottle += new Vector3(0, transform.lossyScale.y * JumpForce, 0);

    //    return true;
    //}

    public bool Jump_AND_Forward()
    {
        if (Time.time < lastJumpTime + JumpCooldown)
        {
            Debug.Log("跳躍冷卻中...");
            return false;
        }

        if (!Controller.isGrounded && FallSpeed < -0.1f)
            return false;

        Vector3 forwardDir = CameraRig.centerEyeAnchor.forward;
        forwardDir.y = 0; // 消除 Y 軸分量，確保不會因為往上看就飛到天上去
        forwardDir.Normalize();

        // 4. 同時增加 向上 (JumpForce) 與 向前 (ForwardForce) 的推力
        // MoveThrottle 通常是處理水平位移的，所以我們把力道加進去
        MoveThrottle += new Vector3(0, JumpForce, 0) + (forwardDir * ForwardForce);

        // 5. 重置下墜速度並更新冷卻時間
        FallSpeed = 0;
        lastJumpTime = Time.time; // 更新最後跳躍時間

        Debug.Log("【成功跳躍前衝】");

        return true;
    }

    private void TriggerPeakFly(float peakSpeed)
    {
        // 使用剛才捕捉到的最高速度來飛行
        Fly(peakSpeed);
        Debug.Log($"[Peak Detection] 偵測到峰值速度: {peakSpeed:F2}，執行飛行！");
    }

    public void Fly(float swingSpeed = 0f) // 傳入揮動速度，預設 0 是給按鍵用的
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic) return;

        // 1. 計算力道倍率 (如果是按鍵觸發 swingSpeed 會是 0，則使用 1 倍力)
        // 如果是揮動，根據速度給予 1.0 ~ 1.5 倍的加成 (數值可自行調整)
        float speedMultiplier = 1.0f;
        if (swingSpeed > swingThreshold)
        {
            // 映射：從 swingThreshold 到 maxSwingSpeed 映射成 1.0 到 1.5
            speedMultiplier = Mathf.Lerp(0.5f, 0.8f, swingSpeed / 3.0f); 
        }

        // 2. 只有在垂直動力還沒超過上限時才增加
        if (MoveThrottle.y < maxFlySpeed)
        {
            // 注入推力 (乘上倍率)
            MoveThrottle += new Vector3(0, flapForce * speedMultiplier, 0);

            // 3. 強制限速 (防止 MoveThrottle 瞬間加到天文數字)
            if (MoveThrottle.y > maxFlySpeed)
            {
                MoveThrottle = new Vector3(MoveThrottle.x, maxFlySpeed * speedMultiplier, MoveThrottle.z);
            }

            Debug.Log($"[Fly] 觸發力道: {flapForce * speedMultiplier:F2} | 當前動力: {MoveThrottle.y:F2}");
        }
    }

    /// <summary>
    /// Causes the player to immediately stop moving, losing all speed and inertia.
    /// </summary>
    /// <remarks>
    /// Stopping merely eliminates all ongoing motion and does not lock the player in place; motion can
    /// resume as early as the next update.
    /// </remarks>
    public void Stop()
    {
        Controller.Move(Vector3.zero);
        MoveThrottle = Vector3.zero;
        FallSpeed = 0.0f;
    }

    /// <summary>
    /// Accessor for the move scale multiplier, populating that value into the ref float argument.
    /// </summary>
    /// <param name="moveScaleMultiplier">Move scale multiplier output argument</param>
    public void GetMoveScaleMultiplier(ref float moveScaleMultiplier)
    {
        moveScaleMultiplier = MoveScaleMultiplier;
    }

    /// <summary>
    /// Setter for the move scale multiplier.
    /// </summary>
    /// <param name="moveScaleMultiplier">New move scale multiplier value</param>
    public void SetMoveScaleMultiplier(float moveScaleMultiplier)
    {
        MoveScaleMultiplier = moveScaleMultiplier;
    }

    /// <summary>
    /// Accessor for the rotation scale multiplier, populating that value into the ref float argument.
    /// </summary>
    /// <param name="rotationScaleMultiplier">Rotation scale multiplier output argument</param>
    public void GetRotationScaleMultiplier(ref float rotationScaleMultiplier)
    {
        rotationScaleMultiplier = RotationScaleMultiplier;
    }

    /// <summary>
    /// Setter for the rotation scale multiplier.
    /// </summary>
    /// <param name="rotationScaleMultiplier">New rotation scale multiplier value</param>
    public void SetRotationScaleMultiplier(float rotationScaleMultiplier)
    {
        RotationScaleMultiplier = rotationScaleMultiplier;
    }

    /// <summary>
    /// Accessor for the "skip mouse rotation" setting, populating the value into the ref bool argument.
    /// </summary>
    /// <param name="skipMouseRotation">Skip mouse rotation output argument</param>
    public void GetSkipMouseRotation(ref bool skipMouseRotation)
    {
        skipMouseRotation = SkipMouseRotation;
    }

    /// <summary>
    /// Setter for the "skip mouse rotation" setting, which determines whether mouse movement is excluded
    /// from turning logic.
    /// </summary>
    /// <param name="skipMouseRotation">New setting for skipping mouse rotation</param>
    /// <remarks>
    /// Mouse rotation in VR can be extremely uncomfortable for users and is disabled by default.
    /// </remarks>
    public void SetSkipMouseRotation(bool skipMouseRotation)
    {
        SkipMouseRotation = skipMouseRotation;
    }

    /// <summary>
    /// Accessor for the "halt update movement" setting, populating the value into the ref bool argument.
    /// </summary>
    /// <param name="haltUpdateMovement">Halt update movement output argument</param>
    public void GetHaltUpdateMovement(ref bool haltUpdateMovement)
    {
        haltUpdateMovement = HaltUpdateMovement;
    }

    /// <summary>
    /// Setter for the "halt update movement" setting, which determines whether user movement updating will
    /// be paused.
    /// </summary>
    /// <param name="haltUpdateMovement">New setting for halting the movement update</param>
    public void SetHaltUpdateMovement(bool haltUpdateMovement)
    {
        HaltUpdateMovement = haltUpdateMovement;
    }

    /// <summary>
    /// Resets the player look rotation when the device orientation is reset.
    /// </summary>
    public void ResetOrientation()
    {
        if (HmdResetsY && !HmdRotatesY)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.y = InitialYRotation;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}
