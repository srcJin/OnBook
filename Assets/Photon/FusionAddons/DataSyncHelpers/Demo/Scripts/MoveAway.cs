using Fusion;
using UnityEngine;

public class MoveAway : NetworkBehaviour
{
    public Vector3 direction = new Vector3(0, 1, 0);
    public float speed = 0.1f;

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (Object.HasStateAuthority)
        {
            transform.position += speed * direction * Runner.DeltaTime;
        }
    }
}
