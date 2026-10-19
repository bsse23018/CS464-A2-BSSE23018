# CS464 Assignment 02 · Muhammad Tayyab · BSSE23018

- **Video:** [YouTube (unlisted) or Google Drive link: 30–90 s, all five levels]
- **Input:** Legacy Input with Active Input Handling set to Both (compatible with InputSystem.actions)
- **Ground check:** CheckSphere on a Ground layer with Gizmo visualization, plus Contact Normal fallback
- **Camera:** Exact smoothing (1 - e^(-k*dt)) in LateUpdate with velocity look-ahead
- **Juice:** Eased squash on landing driven by Ease.OutBack, triggered after airborne time > 0.1s

## Tuning

| Setting | Value | Why this number |
|---|---|---|
| Speed | 5 m/s | Standard brisk walking speed matching lecture baseline; comfortable for precise platforming |
| Jump height | 1.8 m | Allows player to clear 1.2 m - 1.5 m obstacles with a 0.3 m safety margin (Lecture 04/05 baseline) |
| Time to apex | 0.61 s | Derived from kinematics: t_apex = v0 / g = sqrt(2 * 9.81 * 1.8) / 9.81 ≈ 5.94 / 9.81 ≈ 0.61 s |
| Fall multiplier | 2.2× | Makes descent feel snappy (0.41 s vs 0.61 s rise) without making mid-air control abrupt or punishing |
| Coyote time | 0.1 s | At 5 m/s that is 0.5 m past the edge, enough for Level04's run-off jump forgiveness |
| Jump buffer | 0.12 s | Forgives early presses up to 120 ms (approx 7 frames at 60 fps) before landing, making chained jumps responsive |
| Camera smoothing | 5 | Sharpness k = 5 gives responsive tracking (~0.45 s lag) without camera jitter or frame-rate dependency |
| Look-ahead or dead zone | 0.35 s | 0.35 s look-ahead shifts framing ahead by ~1.75 m at full speed so player can see upcoming hazards |

## Four answers

1. **The jump, in numbers.** Launch speed $v_0 = \sqrt{2gh} = \sqrt{2 \times 9.81 \times 1.8} \approx 5.94\text{ m/s}$, with an impulse of $m \cdot v_0 = 5.94\text{ N}\cdot\text{s}$ on a 1 kg body. Time to apex is $t_{\text{rise}} = v_0 / g \approx 0.61\text{ s}$, and fall time under $2.2\times$ gravity is $t_{\text{fall}} = t_{\text{rise}} / \sqrt{2.2} \approx 0.41\text{ s}$, giving a total air hang time of $1.02\text{ s}$. At $5\text{ m/s}$ forward running speed, the player covers $5\text{ m/s} \times 1.02\text{ s} = 5.1\text{ m}$ horizontally, which comfortably clears our widest platform gap of $3\text{ m}$.
2. **Update and FixedUpdate.** Update runs once per rendered frame (variable $\Delta t$) where user input is polled, movement directions are calculated relative to the camera, and jump windows (coyote time and buffer) are stamped. FixedUpdate runs on fixed 0.02 s physics steps where velocity (`rb.linearVelocity`) is assigned, queued jump impulses are applied, and extra fall gravity is integrated. If swapped, input calls like `GetKeyDown` in FixedUpdate miss button presses (two out of three at 144 fps), while physics forces in Update drift across frame rates.
3. **Vector3.Dot.** In `PlayerController.cs` (and Lecture 04 Slide 27 / Lecture 05 Slide 11), `Vector3.Dot(contact.normal, Vector3.up)` (or checking `contact.normal.y > 0.5f`) checks alignment between the surface normal and the vertical up vector. A positive dot product above $0.5$ ($\cos 60^\circ$) signifies a walk-able floor/slope under $60^\circ$, whereas a dot near zero indicates a vertical wall, preventing wall-jumping or wall-sticking.
4. **The juice.** The squash on landing uses the `Ease.OutBack` curve over a duration of $0.2\text{ s}$ (12 frames at 60 fps). OutBack overshoots the target to ~110% before settling, giving an elastic, squishy feel that conveys weight upon impact. Twelve frames is the ideal duration: long enough to be clearly perceived and read as impactful feedback, but short enough to avoid lingering or impeding continuous movement.

## Levels

| Level | What the controller must do there | What I changed for this jump |
|---|---|---|
| Level01 | Navigate starting corridor, climb initial 1 m ramp, and cross standard 1.5 m gap | Standard 1.8 m jump height and 5 m/s speed clear the ramp and gap with ease |
| Level02 | Jump across sequential 2 m platform gaps and ascend stepped ledges | Tuned coyote time (0.1 s) forgives edge run-offs across the gaps |
| Level03 | Clear a wide 2.8 m gap across broken terrain and land on narrow platforms | Used jump buffer (0.12 s) to allow seamless chained jumping between platforms |
| Level04 | Sprint and execute a precision run-off jump over a 3 m void | 5.1 m maximum jump reach comfortably spans the 3 m void |
| Level05 | Ascend vertical platform towers with tight timing and obstacles | Fall gravity (2.2×) provides rapid, controlled descents to stick narrow landings |