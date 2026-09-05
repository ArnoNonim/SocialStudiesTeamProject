using _00_Members.KYM.Scripts.Soldiers.Fleeing;
using _00_Members.KYM.Scripts.Soldiers.Movement;
using KimLIb.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class SoldierFleeModule : MonoBehaviour, IModule
    {
        private enum EscapePosture
        {
            Running,
            Falling,
            Crawling
        }

        [Header("Flee Permission")]
        [SerializeField] private bool canFlee = true;

        [Header("Threat")]
        [Tooltip("Optional. When empty, the active drone is found automatically.")]
        [SerializeField] private Transform droneOverride;
        [SerializeField, Min(1f)] private float detectionRadius = 24f;
        [SerializeField, Min(0.5f)] private float panicRadius = 9f;
        [SerializeField, Min(1f)] private float safeRadius = 32f;
        [SerializeField, Min(0f)] private float minimumApproachSpeed = 0.35f;
        [SerializeField, Min(0.1f)] private float calmDelay = 2f;

        [Header("Escape Route")]
        [SerializeField, Min(2f)] private float fleeDistance = 14f;
        [SerializeField, Min(0.1f)] private float repathInterval = 0.35f;
        [SerializeField, Range(0f, 60f)] private float routeVariation = 35f;
        [SerializeField, Min(90f)] private float turnSpeed = 420f;
        [SerializeField, Min(0.5f)] private float navMeshSampleRadius = 4f;

        [Header("Trip")]
        [SerializeField] private bool canTrip = true;
        [Tooltip("Probability of tripping during one second of running.")]
        [SerializeField, Range(0f, 1f)] private float tripChancePerSecond = 0.08f;
        [SerializeField, Min(0f)] private float minimumRunTimeBeforeTrip = 1.5f;
        [SerializeField] private bool onlyTripOncePerFlee = true;

        [Header("Animation States")]
        [SerializeField] private string runStateName = "Base Layer.RUN";
        [SerializeField] private string idleStateName = "Base Layer.IDLE";
        [SerializeField] private string fallStateName = "Base Layer.FALL_DOWN";
        [SerializeField] private string crawlStateName = "Base Layer.CRAWL";
        [SerializeField, Min(0f)] private float transitionDuration = 0.2f;

        [Header("Animation Timing Fallback")]
        [Tooltip("Used when the fall state is missing or cannot report completion.")]
        [SerializeField, Min(0.1f)] private float fallDuration = 0.9f;

        private Soldier _soldier;
        private ISoldierNavigation _navigation;
        private SoldierRootMotionRelay _rootMotionRelay;
        private Animator _animator;
        private Rigidbody _rootRigidbody;
        private IFleeThreatSensor _threatSensor;
        private IEscapeRoutePlanner _routePlanner;
        private IFleeAnimationPlayer _animationPlayer;
        private EscapePosture _posture = EscapePosture.Running;
        private bool _isFleeing;
        private bool _initialApplyRootMotion;
        private bool _initialKinematic;
        private bool _hasTrippedThisFlee;
        private float _nextRepathTime;
        private float _safeSince = -1f;
        private float _runningElapsed;
        private float _postureElapsed;
        private int _activePostureStateHash;
        private Vector3 _threatPosition;

        public bool CanFlee
        {
            get => canFlee;
            set
            {
                canFlee = value;
                if (!canFlee)
                {
                    StopFleeing();
                }
            }
        }

        public bool IsFleeing => _isFleeing;
        public bool IsCrawling => _isFleeing && _posture == EscapePosture.Crawling;

        public void Initialize(ModuleOwner owner)
        {
            _soldier = owner as Soldier;
            _navigation = owner.GetModule<ISoldierNavigation>();
            _rootMotionRelay = owner.GetModule<SoldierRootMotionRelay>();
            _animator = owner.GetComponentInChildren<Animator>(true);
            _rootRigidbody = owner.GetComponent<Rigidbody>();
            _initialApplyRootMotion = _animator != null && _animator.applyRootMotion;
            _initialKinematic = _rootRigidbody != null && _rootRigidbody.isKinematic;
            _threatSensor = new FleeThreatSensor(droneOverride);
            _routePlanner = new NavMeshEscapeRoutePlanner(
                fleeDistance,
                routeVariation,
                navMeshSampleRadius);
            _animationPlayer = new FleeAnimationPlayer(_animator, transitionDuration);
        }

        private void Update()
        {
            if (!canFlee || _soldier == null || _soldier.IsDead)
            {
                if (_isFleeing)
                {
                    StopFleeing();
                }

                return;
            }

            if (!_threatSensor.TryObserve(
                    transform.position,
                    Time.deltaTime,
                    Time.time,
                    out FleeThreatObservation threat))
            {
                return;
            }

            _threatPosition = threat.Position;

            if (!_isFleeing)
            {
                bool isPanicking = threat.Distance <= panicRadius;
                bool isApproaching = threat.Distance <= detectionRadius &&
                                     threat.ApproachSpeed >= minimumApproachSpeed;
                if (isPanicking || isApproaching)
                {
                    StartFleeing();
                }
            }

            if (_isFleeing)
            {
                UpdateEscape(threat.Distance, threat.ApproachSpeed);
            }
        }

        private void UpdateEscape(float threatDistance, float approachSpeed)
        {
            _postureElapsed += Time.deltaTime;

            switch (_posture)
            {
                case EscapePosture.Running:
                    _runningElapsed += Time.deltaTime;
                    UpdateCalmState(threatDistance, approachSpeed);
                    if (!_isFleeing)
                    {
                        return;
                    }

                    UpdateNavigation();
                    TryTrip();
                    break;

                case EscapePosture.Falling:
                    if (!IsCurrentAnimationFinished(fallDuration))
                    {
                        return;
                    }

                    BeginCrawling();
                    break;

                case EscapePosture.Crawling:
                    UpdateNavigation();
                    break;
            }
        }

        private void UpdateNavigation()
        {
            NavMeshAgent agent = _navigation?.NavAgent;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                if (NeedsNewEscapeRoute(agent))
                {
                    SelectEscapeRoute();
                }
            }

            RotateTowardPath();
        }

        private bool NeedsNewEscapeRoute(NavMeshAgent agent)
        {
            if (agent.pathPending)
            {
                return false;
            }

            if (!agent.hasPath ||
                agent.pathStatus != NavMeshPathStatus.PathComplete ||
                agent.remainingDistance <= Mathf.Max(agent.stoppingDistance + 0.5f, 1f))
            {
                return true;
            }

            float currentSeparation = HorizontalDistance(transform.position, _threatPosition);
            float destinationSeparation = HorizontalDistance(agent.destination, _threatPosition);
            return destinationSeparation < currentSeparation + fleeDistance * 0.2f;
        }

        private void TryTrip()
        {
            if (!canTrip ||
                tripChancePerSecond <= 0f ||
                _runningElapsed < minimumRunTimeBeforeTrip ||
                (onlyTripOncePerFlee && _hasTrippedThisFlee))
            {
                return;
            }

            float frameChance = 1f - Mathf.Pow(1f - tripChancePerSecond, Time.deltaTime);
            if (Random.value <= frameChance)
            {
                BeginFalling();
            }
        }

        private void BeginFalling()
        {
            if (!_isFleeing || _posture != EscapePosture.Running)
            {
                return;
            }

            _hasTrippedThisFlee = true;
            _posture = EscapePosture.Falling;
            _postureElapsed = 0f;
            _runningElapsed = 0f;
            _activePostureStateHash = _animationPlayer.Play(fallStateName);
            _navigation?.StopPath();
        }

        private void BeginCrawling()
        {
            _posture = EscapePosture.Crawling;
            _postureElapsed = 0f;
            _nextRepathTime = 0f;
            _activePostureStateHash = _animationPlayer.Play(crawlStateName);
        }

        private void BeginRunning()
        {
            _posture = EscapePosture.Running;
            _postureElapsed = 0f;
            _runningElapsed = 0f;
            _nextRepathTime = 0f;
            _activePostureStateHash = _animationPlayer.Play(runStateName);
        }

        private bool IsCurrentAnimationFinished(float fallbackDuration)
        {
            return _animationPlayer.IsFinished(
                _activePostureStateHash,
                _postureElapsed,
                fallbackDuration);
        }

        private void StartFleeing()
        {
            if (_navigation == null || !_navigation.BeginRootMotionNavigation())
            {
                return;
            }

            _isFleeing = true;
            _safeSince = -1f;
            _hasTrippedThisFlee = false;

            if (_rootRigidbody != null)
            {
                _rootRigidbody.linearVelocity = Vector3.zero;
                _rootRigidbody.angularVelocity = Vector3.zero;
                _rootRigidbody.isKinematic = true;
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = true;
            }

            _rootMotionRelay?.SetApplying(true);
            BeginRunning();
        }

        private void StopFleeing()
        {
            if (!_isFleeing)
            {
                return;
            }

            _isFleeing = false;
            _safeSince = -1f;
            _posture = EscapePosture.Running;
            _activePostureStateHash = 0;
            _rootMotionRelay?.SetApplying(false);
            _navigation?.EndRootMotionNavigation();

            if (_animator != null)
            {
                _animationPlayer.Play(idleStateName);
                _animator.applyRootMotion = _initialApplyRootMotion;
            }

            if (_rootRigidbody != null && (_soldier == null || !_soldier.IsDead))
            {
                _rootRigidbody.isKinematic = _initialKinematic;
            }
        }

        private void UpdateCalmState(float threatDistance, float approachSpeed)
        {
            if (threatDistance < safeRadius || approachSpeed > 0f)
            {
                _safeSince = -1f;
                return;
            }

            if (_safeSince < 0f)
            {
                _safeSince = Time.time;
                return;
            }

            if (Time.time - _safeSince >= calmDelay)
            {
                StopFleeing();
            }
        }

        private void SelectEscapeRoute()
        {
            _routePlanner.TrySetEscapeRoute(
                _navigation,
                transform.position,
                transform.forward,
                _threatPosition);
        }

        private void RotateTowardPath()
        {
            NavMeshAgent agent = _navigation?.NavAgent;
            if (agent == null || agent.pathPending || !agent.hasPath)
            {
                return;
            }

            // steeringTarget points at the next corner, so the soldier starts
            // turning before root motion reaches and rubs against the boundary.
            Vector3 direction = agent.steeringTarget - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = agent.desiredVelocity;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f)
                {
                    return;
                }
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float turnAngle = Quaternion.Angle(transform.rotation, targetRotation);
            float cornerMultiplier = Mathf.Lerp(1f, 1.75f, Mathf.InverseLerp(20f, 120f, turnAngle));
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * cornerMultiplier * Time.deltaTime);
        }

        [ContextMenu("Force Trip While Playing")]
        private void ForceTripWhilePlaying()
        {
            BeginFalling();
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDisable()
        {
            StopFleeing();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.72f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.color = new Color(0.9f, 0.12f, 0.08f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, panicRadius);
            Gizmos.color = new Color(0.1f, 0.7f, 0.35f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, safeRadius);
        }
    }
}
