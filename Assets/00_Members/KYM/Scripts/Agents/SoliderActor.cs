using _00_Members.KYM.Scripts.Agents;
using UnityEngine;

public enum SoldierCommandType
{
    None,
    Idle,
    MoveTo,
    FleeFrom,
    LookAt
}

public class SoldierActor : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SoldierMover mover;
    // 나중에 추가
    // [SerializeField] private SoldierRenderer soldierRenderer;
    // [SerializeField] private SoldierHealth health;
    // [SerializeField] private SoldierBrain brain;

    public SoldierMover Mover => mover;
    public SoldierCommandType CurrentCommand { get; private set; }
    public bool IsDead { get; private set; }

    private void Reset()
    {
        mover = GetComponent<SoldierMover>();
    }

    public void MoveTo(Vector3 destination, bool run = false)
    {
        if (IsDead)
            return;

        CurrentCommand = SoldierCommandType.MoveTo;
        mover.MoveTo(destination, run);
    }

    public void FleeFrom(Vector3 threatPosition)
    {
        if (IsDead)
            return;

        CurrentCommand = SoldierCommandType.FleeFrom;
        mover.FleeFrom(threatPosition);
    }

    public void LookAt(Transform target)
    {
        if (IsDead)
            return;

        CurrentCommand = SoldierCommandType.LookAt;
        mover.LookAt(target);
    }

    public void Stop()
    {
        if (IsDead)
            return;

        CurrentCommand = SoldierCommandType.Idle;
        mover.Stop();
    }

    public void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        CurrentCommand = SoldierCommandType.None;

        mover.Stop();

        // 나중에 연결
        // brain.enabled = false;
        // soldierRenderer.PlayDeath();
        // health.DisableHitDetection();
    }
}