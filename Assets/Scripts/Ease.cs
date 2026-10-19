using UnityEngine;

/// <summary>
/// Easing curves from Lecture 05: Maths, Physics & Game Feel (slides 27-30).
/// Used by coroutines with Vector3.LerpUnclamped for game juice and feel.
/// </summary>
public static class Ease
{
    public static float Linear(float t) => t;

    public static float InQuad(float t) => t * t;

    public static float OutQuad(float t) => t * (2f - t);

    public static float InOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    /// <summary>
    /// OutBack curve: fast start, overshoots the target (~110%), then settles back to rest.
    /// Used for squash on landing juice.
    /// </summary>
    public static float OutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
