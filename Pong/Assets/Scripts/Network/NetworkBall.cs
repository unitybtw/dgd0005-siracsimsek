using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkBall : NetworkBehaviour
{
    public static NetworkBall Instance { get; private set; }
    public readonly SyncVar<bool> InPlay = new SyncVar<bool>();
    [SerializeField] private float speed = 7f;
    private Vector3 velocity;
    private void Awake() { Instance = this; InPlay.OnChange += PlayChanged; }
    private void OnDestroy() { InPlay.OnChange -= PlayChanged; if (Instance == this) Instance = null; }
    public override void OnStartClient() { base.OnStartClient(); PaintBall(); }
    public override void OnStartServer() { base.OnStartServer(); ResetToCentre(); }
    private void PlayChanged(bool previous, bool next, bool asServer) => PaintBall();
    private void PaintBall() => Court.Paint(gameObject, InPlay.Value ? Color.white : new Color(0.45f, 0.49f, 0.55f));
    public void ResetToCentre()
    {
        if (!IsServerInitialized) return;
        velocity = Vector3.zero;
        InPlay.Value = false;
        transform.position = new Vector3(0, Court.BallY, 0);
        PaintBall();
    }
    public void Serve(int towardSide)
    {
        if (!IsServerInitialized || InPlay.Value || towardSide < 0 || towardSide > 1) return;
        velocity = new Vector3(towardSide == 0 ? -1f : 1f, 0, Random.Range(-0.6f, 0.6f)).normalized * speed;
        InPlay.Value = true;
    }
    private void Update()
    {
        if (!IsServerInitialized) return;
        if (!InPlay.Value) return;
        // Substeps preserve paddle crossings even after a long frame.
        float remaining = Mathf.Min(Time.deltaTime, 0.25f);
        while (remaining > 0 && InPlay.Value)
        {
            float dt = Mathf.Min(remaining, 1f / 120f);
            MoveBall(dt);
            remaining -= dt;
        }
    }
    private void MoveBall(float dt)
    {
        Vector3 before = transform.position;
        Vector3 p = before + velocity * dt;
        float zLimit = Court.HalfHeight - Court.BallRadius;
        if (Mathf.Abs(p.z) > zLimit) { p.z = Mathf.Sign(p.z) * zLimit; velocity.z = -velocity.z; }
        foreach (var paddle in PaddleController.All)
        {
            if (paddle == null || !paddle.IsServerInitialized || paddle.Side.Value >= 2) continue;
            Vector3 pp = paddle.transform.position;
            float face = pp.x + (paddle.Side.Value == 0 ? 0.5f : -0.5f);
            bool crossing = paddle.Side.Value == 0 ? velocity.x < 0 && before.x >= face && p.x <= face
                                                    : velocity.x > 0 && before.x <= face && p.x >= face;
            if (!crossing) continue;
            float fraction = (face - before.x) / (p.x - before.x);
            float hitZ = Mathf.Lerp(before.z, p.z, fraction);
            if (Mathf.Abs(hitZ - pp.z) > Court.PaddleHalfLength + Court.BallRadius) continue;
            float angle = Mathf.Clamp((hitZ - pp.z) / (Court.PaddleHalfLength + Court.BallRadius), -1f, 1f) * 60f * Mathf.Deg2Rad;
            velocity = new Vector3(paddle.Side.Value == 0 ? Mathf.Cos(angle) : -Mathf.Cos(angle), 0, Mathf.Sin(angle)) * speed;
            p.x = face;
        }
        transform.position = p;
        if (p.x < -Court.HalfWidth - 0.5f) Score(1);
        else if (p.x > Court.HalfWidth + 0.5f) Score(0);
    }
    private void Score(int side)
    {
        ResetToCentre();
        if (GameState.Instance != null) GameState.Instance.Goal(side);
    }
}
