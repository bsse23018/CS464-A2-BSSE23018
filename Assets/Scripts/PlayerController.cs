using System.Collections;
using UnityEngine;

/// <summary>
/// PlayerController implementing movement and jumping mechanics from Lectures 04 and 05.
/// Part A: Update/FixedUpdate split, linearVelocity, CheckSphere/ContactNormal grounded check, jumpHeight impulse.
/// Part B: Coyote time, jump buffer, asymmetric fall gravity, and eased squash on landing juice.
/// Part C: Camera-relative movement (W walks away from the camera).
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement (Part A & C)")]
    [Tooltip("Movement speed in metres per second")]
    [SerializeField] float speed = 5f;

    [Header("Jump Physics (Part A & B)")]
    [Tooltip("Target jump apex height in metres")]
    [SerializeField] float jumpHeight = 1.8f;
    [Tooltip("Multiplier applied to gravity while falling")]
    [SerializeField] float fallMultiplier = 2.2f;

    [Header("Forgiveness Windows (Part B)")]
    [Tooltip("Grace period in seconds after leaving a ledge where jump is still accepted")]
    [SerializeField] float coyoteTime = 0.1f;
    [Tooltip("Buffer window in seconds before landing to queue an incoming jump")]
    [SerializeField] float jumpBuffer = 0.12f;

    [Header("Ground Check (Part A)")]
    [SerializeField] Transform feet;
    [SerializeField] float checkRadius = 0.25f;
    [SerializeField] LayerMask groundMask;

    [Header("Juice (Part B)")]
    [SerializeField] Transform body;
    [SerializeField] float squashDuration = 0.2f;

    Rigidbody rb;
    Transform camTransform;
    Vector3 move; // Camera-relative input direction, clamped to magnitude 1

    bool isGrounded;
    bool jumpQueued;
    float lastGroundedTime = -1f;
    float lastPressedTime = -1f;

    // Contact normal tracking (Lecture 04, slide 27)
    bool contactGrounded;
    Vector3 lastContactPoint;
    Vector3 lastContactNormal = Vector3.up;

    // Juice tracking
    float airTime;
    bool wasGrounded;
    Coroutine squashRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        // Default feet transform if not assigned in Inspector
        if (feet == null)
        {
            Transform foundFeet = transform.Find("Feet");
            if (foundFeet != null)
            {
                feet = foundFeet;
            }
        }

        // Default body transform if not assigned in Inspector
        if (body == null)
        {
            Transform foundBody = transform.Find("Body");
            if (foundBody != null)
            {
                body = foundBody;
            }
        }
    }

    void Update()
    {
        // Cache camera transform if not already found
        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        // --- 1. Read Input & Build Camera-Relative Movement (Lecture 04 slide 29, Lecture 05 slide 7) ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 forward = camTransform != null ? camTransform.forward : Vector3.forward;
        Vector3 right = camTransform != null ? camTransform.right : Vector3.right;
        forward.y = 0f;
        forward.Normalize();
        right.y = 0f;
        right.Normalize();

        move = forward * v + right * h;
        move = Vector3.ClampMagnitude(move, 1f);

        // --- 2. Ground Check (Lecture 04, slides 27 & 30) ---
        if (feet != null)
        {
            LayerMask mask = groundMask.value != 0 ? groundMask : ~LayerMask.GetMask("Ignore Raycast");
            isGrounded = Physics.CheckSphere(feet.position, checkRadius, mask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            isGrounded = contactGrounded;
        }

        // --- 3. Forgiveness Windows: Coyote Time & Jump Buffer (Lecture 05, slides 21 & 23) ---
        // Stamp ground time only when grounded, no jump is queued, and not rising
        if (isGrounded && !jumpQueued && rb.linearVelocity.y <= 0.01f)
        {
            lastGroundedTime = Time.time;
        }

        // Note the jump press
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
        if (jumpPressed)
        {
            lastPressedTime = Time.time;
        }

        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;
        bool wantJump = (Time.time - lastPressedTime) <= jumpBuffer;

        // When both windows are open, queue the jump and consume both timestamps
        if (canJump && wantJump)
        {
            jumpQueued = true;
            lastGroundedTime = -1f;
            lastPressedTime = -1f;
        }

        // --- 4. Game Juice: Squash on Landing (Lecture 05, slides 27, 30, 32) ---
        if (!isGrounded)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            // Trigger squash only on a real landing (in the air for more than 0.1 s)
            if (!wasGrounded && airTime > 0.1f && body != null)
            {
                if (squashRoutine != null) StopCoroutine(squashRoutine);
                squashRoutine = StartCoroutine(Squash());
            }
            airTime = 0f;
        }
        wasGrounded = isGrounded;
    }

    void FixedUpdate()
    {
        // Set linearVelocity from move, keeping vertical velocity (Lecture 04, slide 29)
        rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);

        // Apply queued jump once (Lecture 05, slide 20 & 23)
        if (jumpQueued)
        {
            // Zero the vertical velocity first so jumps taken while falling do not come out short
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // Kinematic launch speed: v0 = sqrt(2 * g * h)
            float g = -Physics.gravity.y;
            float v0 = Mathf.Sqrt(2f * g * jumpHeight);

            // Apply impulse = m * v0
            rb.AddForce(Vector3.up * (rb.mass * v0), ForceMode.Impulse);

            jumpQueued = false;
        }

        // Fall gravity: extra gravity only while falling (Lecture 05, slide 23)
        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    // Squash on landing coroutine driven by Ease.OutBack (Lecture 05, slide 30)
    IEnumerator Squash()
    {
        // Volume preservation: at y = 0.7, x and z are 1 / sqrt(0.7) ≈ 1.1952
        float squashedY = 0.7f;
        float squashedXZ = 1f / Mathf.Sqrt(squashedY);
        Vector3 squashedScale = new Vector3(squashedXZ, squashedY, squashedXZ);

        for (float elapsed = 0f; elapsed < squashDuration; elapsed += Time.deltaTime)
        {
            float p = Ease.OutBack(elapsed / squashDuration);
            // LerpUnclamped allows the OutBack overshoot to expand before returning to 1
            body.localScale = Vector3.LerpUnclamped(squashedScale, Vector3.one, p);
            yield return null;
        }

        // Exactly at rest
        body.localScale = Vector3.one;
        squashRoutine = null;
    }

    // Contact normal ground check (Lecture 04, slide 27)
    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // Normal pointing upward (> 0.5f) counts as ground
            if (contact.normal.y > 0.5f)
            {
                contactGrounded = true;
                lastContactPoint = contact.point;
                lastContactNormal = contact.normal;
                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        contactGrounded = false;
    }

    void OnDrawGizmosSelected()
    {
        // Draw CheckSphere gizmo (Lecture 04, slide 30)
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 spherePos = feet != null ? feet.position : transform.position + Vector3.down * 0.9f;
        Gizmos.DrawWireSphere(spherePos, checkRadius);

        // Draw Contact normal ray gizmo if grounded via contact (Lecture 04, slide 27)
        if (contactGrounded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(lastContactPoint, lastContactNormal);
        }
    }
}
