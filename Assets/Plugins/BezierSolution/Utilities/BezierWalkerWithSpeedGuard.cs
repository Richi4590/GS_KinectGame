using UnityEngine;
using UnityEngine.Events;

namespace BezierSolution
{
    [AddComponentMenu("Bezier Solution/Bezier Walker With Speed Guard")]
    [HelpURL("https://github.com/yasirkula/UnityBezierSolution")]
    public class BezierWalkerWithSpeedGuard : BezierWalker
    {
        public bool shouldWalk = true;
        public float moveAmount = 0.1f;

        public BezierSpline spline;
        public TravelMode travelMode;

        public float speed = 5f;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_normalizedT = 0f;

        private float initialSpeed = 0;
        private BezierPoint lastCheckpoint;

        public override BezierSpline Spline { get { return spline; } }

        public override float NormalizedT
        {
            get { return m_normalizedT; }
            set { m_normalizedT = value; }
        }

        //public float movementLerpModifier = 10f;
        public float rotationLerpModifier = 10f;

        public LookAtMode lookAt = LookAtMode.ZForward;

        private bool isGoingForward = true;
        public override bool MovingForward
        {
            get { return (speed >= 0f) == isGoingForward; }
            set { isGoingForward = (speed >= 0f) == value; }
        }

        public UnityEvent onPathCompleted = new UnityEvent();
        private bool onPathCompletedCalledAt1 = false;
        private bool onPathCompletedCalledAt0 = false;


        private void Awake()
        {

        }

        public void StartMoving()
        {
            shouldWalk = true;
            speed = initialSpeed;
        }

        public void StopMoving()
        {
            shouldWalk = false;
            speed = 0f;
        }

        public void ToggleMove()
        {
            if (shouldWalk)
                StopMoving();
            else
                StartMoving();
        }

        public void MoveForwards()
        {
            shouldWalk = false;
            m_normalizedT -= moveAmount * Time.deltaTime;

            Execute(Time.deltaTime);
        }

        public void MoveBackwards()
        {
            shouldWalk = false;
            m_normalizedT += moveAmount * Time.deltaTime;
            Execute(Time.deltaTime);
        }

        public void ResetToLastCheckpoint()
        {
            m_normalizedT = spline.GetNormalizedTFromBezierPoint(lastCheckpoint);
            Execute(Time.deltaTime);
        }

        public void ResetToLastCheckpointNotMoving()
        {
            shouldWalk = false;
            m_normalizedT = spline.GetNormalizedTFromBezierPoint(lastCheckpoint);
            Execute(Time.deltaTime);
        }

        public void Reset()
        {
            shouldWalk = false;
            speed = 0f;
        }

        private void Start()
        {
            initialSpeed = speed;
            lastCheckpoint = spline.endPoints[0];
        }

        private void Update()
        {
            if (shouldWalk)
            {
                Execute(Time.deltaTime);

                if (transform.position.x <= 0.2f && transform.position.z <= 0.2f)
                {
                    BezierPoint p = spline.GetPrevBezierPoint(m_normalizedT);
                    if (lastCheckpoint != p)
                        lastCheckpoint = p;
                }

            }
        }

        public override void Execute(float deltaTime)
        {
            Vector3 newPos = spline.MoveAlongSpline(ref m_normalizedT, (isGoingForward ? speed : -speed) * deltaTime);

            transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
            RotateTarget(transform, m_normalizedT, lookAt, rotationLerpModifier * deltaTime);
            PostProcessMovement(travelMode, ref onPathCompletedCalledAt0, ref onPathCompletedCalledAt1, onPathCompleted);
        }
    }
}