using UnityEngine;

/// <summary>
/// Camera follow system from Lecture 05: Maths, Physics & Game Feel (slide 42).
/// Upgrades Lecture 04's camera: moves in LateUpdate and uses frame-rate independent
/// smoothing (1 - e^(-k*dt) or SmoothDamp) with velocity look-ahead.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public enum SmoothingMethod
    {
        Exponential,
        SmoothDamp
    }

    [Header("Target & Offset")]
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 4f, -7f);

    [Header("Smoothing")]
    [SerializeField] SmoothingMethod smoothing = SmoothingMethod.Exponential;
    [SerializeField] float sharpness = 5f;     // For exponential: blend = 1 - e^(-sharpness * dt)
    [SerializeField] float smoothTime = 0.3f;  // For SmoothDamp

    [Header("Look-Ahead")]
    [SerializeField] bool enableLookAhead = true;
    [SerializeField] float lookAhead = 0.35f;  // seconds ahead (slide 42)

    Rigidbody targetRb;
    Vector3 smoothDampVelocity;

    void Awake()
    {
        FindTargetIfMissing();
    }

    void LateUpdate()
    {
        if (target == null && !FindTargetIfMissing())
            return;

        // Aim point includes look-ahead: flat velocity * lookAhead (Lecture 05, slide 42)
        Vector3 aimPoint = target.position;
        if (enableLookAhead && targetRb != null)
        {
            Vector3 flatVelocity = targetRb.linearVelocity;
            flatVelocity.y = 0f;
            aimPoint += flatVelocity * lookAhead;
        }

        Vector3 wanted = aimPoint + offset;

        // Frame-rate independent smoothing
        if (smoothing == SmoothingMethod.Exponential)
        {
            float blend = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, wanted, blend);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, wanted, ref smoothDampVelocity, smoothTime);
        }

        // Look at the aim point (not raw target), preserving look-ahead
        transform.LookAt(aimPoint);
    }

    bool FindTargetIfMissing()
    {
        if (target != null)
        {
            if (targetRb == null) targetRb = target.GetComponent<Rigidbody>();
            return true;
        }

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            target = player.transform;
            targetRb = target.GetComponent<Rigidbody>();
            return true;
        }

        return false;
    }
}
