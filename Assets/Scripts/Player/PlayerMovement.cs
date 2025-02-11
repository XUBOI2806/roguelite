using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using DG.Tweening;

namespace Player
{
    [RequireComponent(typeof (CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader = default;
        [SerializeField]
        private float moveSpeed = 4f;
        public float currentHorizontalSpeed;
        [SerializeField]
        private float animAcceleration = 10f;
        [SerializeField]
        private float animDamper = 0.5f;
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        [SerializeField]
        private float playerEyeHeight = 0.75f;
        [SerializeField]
        private float gravity = -15.0f;
        [SerializeField]
        private float digSpeedMultiplier = 1.2f;

        [SerializeField]
        private float gamepadDeadzone = 0.01f;

        [SerializeField]
        private float gamepadRotateSmoothing = 500f;

        [SerializeField]
        private BoolEventChannelSO usingGamepadSO = default;
        [SerializeField]
        private bool isGamepad;
                
        [SerializeField]
        private TransformAnchor mainCamera = default;

        [SerializeField] private Transform aimTransform;
        [SerializeField] private Transform gfxTransform;
        [Tooltip("Angular speed in degrees per sec.")]
        [SerializeField] private float gfxRotateSpeed = 30;
        [SerializeField] private Animator gfxAnimator;
        Quaternion lookAt;

        private CharacterController controller;

        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        public bool allowMovement = true;
        public bool allowRotation = true;

        public float dashSpeed = 15;
        public float dashTime = 0.25f;
        public float dashCoolTime = 1;
        public bool isDashing = false;
        public bool canDash = true;
        public Vector3 lastHorizontalVelocity;

        public GameObject dashVFX;

        [SerializeField] private PlayerInputAnchor _playerInputAnchor = default;

        private Vector3 playerForward;
        private Vector3 playerRight;

        public PlayerCharacter playerCharacter;
        public bool attackOverridesMovement = false;

        [Header("Broadcasting on")]
        [SerializeField] private PlayerMovementAnchor _playerMovementAnchor = default;
        [SerializeField] private BoolEventChannelSO _updateDashUI = default;

        private void Awake()
        {
            Initialise();
            _playerMovementAnchor.Provide(this);
            playerCharacter = GetComponent<PlayerCharacter>();
            if (_updateDashUI != null)
                _updateDashUI.RaiseEvent(false);
        }
        private void OnEnable()
        {
            InputUser.onChange += OnDeviceChange;
            inputReader.OnJumpPerformed += jump;
        }

        private void OnDisable()
        {
            InputUser.onChange -= OnDeviceChange;
            inputReader.OnJumpPerformed -= jump;
        }
        public void Initialise()
        {
            controller = GetComponent<CharacterController>();
            if (_playerInputAnchor.isSet)
            {
                isGamepad = _playerInputAnchor.Value.currentControlScheme.Equals("Gamepad") ? true : false;
            }
        }

        private void jump()
        {
            // Test the jump key is working.
            //Debug.Log("Test: Jump Key working");
            if (canDash && !playerCharacter.isDigging)
            {
                if(_updateDashUI != null)
                    _updateDashUI.RaiseEvent(true);
                StartCoroutine(BeginDash());
            }
        }

        private IEnumerator BeginDash()
        {
            //invulnerable
            //Code base description for invulnerable:
            /*
             * so in this whole code, we will set invulnerable as true when we dash, 
             * then if the player has been trigger the takedamage function, 
             * this function will check the invulnerable status first, 
             * if is true take no damage, when the dash has gone for 0.2 second the 
             * "yield return new WaitForSeconds(0.2f);" will run so now after 0.2 second 
             * set invulnerable to false.
             */
            playerCharacter.invulnerable = true;
            //
            allowMovement = false;
            allowRotation = false;
            isDashing = true;
            lastHorizontalVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
            if (dashVFX)
                Instantiate(dashVFX, transform.position, gfxTransform.rotation);

            float startTime = Time.time;

            while(Time.time < startTime + dashTime)
            {
                controller.Move(lastHorizontalVelocity.normalized * dashSpeed * Time.deltaTime);
                yield return null;
            }
            isDashing =false;
            allowMovement = true;
            allowRotation = true;
            lastHorizontalVelocity = Vector3.zero;
            StartCoroutine(DashCooldown());

            //invulnerable
            yield return new WaitForSeconds(0.2f);
            playerCharacter.invulnerable = false;
            //
        }

        IEnumerator DashCooldown()
        {
            canDash = false;
            yield return new WaitForSeconds(dashCoolTime);
            canDash = true;
        }

        public void MoveTowardsTarget(Transform target, float duration)
        {
            if (target)
            {
                gfxTransform.LookAt(target, Vector3.up);
                transform.DOMove(TargetOffset(target.position), duration);
            }
        }

        public Vector3 TargetOffset(Vector3 targetPosition)
        {
            return Vector3.MoveTowards(targetPosition, transform.position, .95f);
        }

        private void Update()
        {
            ApplyGravity();
            if(allowMovement && !attackOverridesMovement)
                HandleMovement();
            if(aimTransform && allowRotation && !attackOverridesMovement)
                HandleRotation();
            HandleAnimation();
        }

        private void HandleAnimation()
        {
            currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;
            if(gfxAnimator)
                gfxAnimator.SetFloat("Speed", currentHorizontalSpeed / moveSpeed, animDamper, Time.deltaTime * animAcceleration);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded)
            {
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }
            }
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }
        }

        private void HandleMovement()
        {
            Vector3 move;
            if (mainCamera.isSet)
            {
                playerForward = Vector3.ProjectOnPlane(mainCamera.Value.forward, Vector3.up);
                playerRight = Vector3.ProjectOnPlane(mainCamera.Value.right, Vector3.up);

                Vector2 inputMap = playerCharacter.isDigging ? new Vector2(inputReader.MoveComposite.x, inputReader.MoveComposite.y) : new Vector2(inputReader.MoveComposite.normalized.x, inputReader.MoveComposite.normalized.y);
                move = playerForward.normalized * inputMap.y + playerRight.normalized * inputMap.x;
            } else
            { 
                //No CameraManager exists in the scene, so the input is just used absolute in world-space
                Debug.LogWarning("No gameplay camera in the scene. Movement orientation will not be correct.");
                move = new Vector3(inputReader.MoveComposite.normalized.x, 0f, inputReader.MoveComposite.normalized.y);
            }

            lookAt = (inputReader.MoveComposite.magnitude > 0.1f) ? Quaternion.LookRotation(move) : lookAt;
            if(gfxTransform)
                gfxTransform.rotation = Quaternion.RotateTowards(gfxTransform.rotation, lookAt, Time.deltaTime * gfxRotateSpeed);
            float digSpeedFactor = playerCharacter.isDigging ? digSpeedMultiplier : 1;
            controller.Move((move  * moveSpeed * digSpeedFactor * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void HandleRotation()
        {
            
            if (isGamepad)
                GamepadLook();
            else
                MouseLook();
        }

        private void GamepadLook()
        {
            if ( Mathf.Abs(inputReader.Look.x) > gamepadDeadzone || Mathf.Abs(inputReader.Look.y) > gamepadDeadzone )
            {
                Vector3 playerDirection =
                    playerRight * inputReader.Look.x +
                    playerForward * inputReader.Look.y;
                if (playerDirection.sqrMagnitude > 0.0f)
                {
                    Quaternion newRot = Quaternion.LookRotation(playerDirection, Vector3.up);
                    aimTransform.rotation = Quaternion.RotateTowards(aimTransform.rotation, newRot, gamepadRotateSmoothing * Time.deltaTime);
                }
            }
        }

        private void MouseLook()
        {
            Ray ray = Camera.main.ScreenPointToRay(inputReader.MousePosition);
            Plane xzPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y + playerEyeHeight, 0));
            float rayDistance;

            if(xzPlane.Raycast(ray, out rayDistance))
            {
                Vector3 intersect = ray.GetPoint(rayDistance);
                Vector3 point = new(intersect.x, transform.position.y, intersect.z);
                aimTransform.LookAt(point);
            }
        }

        public void OnDeviceChange(InputUser user, InputUserChange change, InputDevice device)
        {
            if (change == InputUserChange.ControlSchemeChanged)
            {
                isGamepad = user.controlScheme.Value.name.Equals("Gamepad") ? true : false;
            }
        }
    }
}
