using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OctopusController;

public class IK_Scorpion : MonoBehaviour
{
    MyScorpionController _myController= new MyScorpionController();

    public IK_tentacles _myOctopus;

    [Header("Body")]
    float animTime;
    public float animDuration = 5;
    bool animPlaying = false;
    public Transform Body;
    public Transform StartPos;
    public Transform EndPos;

    private Quaternion _desiredLookRotation;

    private Vector3 _lastBodyPosition = Vector3.zero;
    private Vector3 _currentForward = Vector3.zero;

    private Vector3 _moveOffset = Vector3.zero;

    [SerializeField] private Transform[] _fakeEE;
    [SerializeField] private Transform[] _realEE;

    [Header("Tail")]
    public Transform tailTarget;
    public Transform tail;
    private float tailTargetBallOffsetLength;
    private bool _isGoalTargetRightSide = false;

    private Vector3 _ballHitToCenterDir;
    public Vector3 BallHitToCenterDir => _ballHitToCenterDir;


    [Header("Legs")]
    public Transform[] legs;
    public Transform[] legTargets;
    public Transform[] futureLegBases;

    private bool _reset = false;

    [Header("Ball")]
    [SerializeField] private MovingBall _movingBall;

    [Header("UI Controller")]
    [SerializeField] private UI_Controller _uiController;

    [Header("Body Animation")]
    [SerializeField] private Transform mainBody;
    private Vector3 _bodyToLegsOffset;

    readonly float _futureLegBaseOriginDisplacement = 2f;
    readonly float _futureLegBaseProbeDist = 5f;
    readonly Vector3 _futureLegBaseProbeDirection = Vector3.down;



    // Start is called before the first frame update
    void Start()
    {
        _myController.InitLegs(legs,futureLegBases,legTargets);
        _myController.InitTail(tail);

        tailTargetBallOffsetLength = _movingBall._ballRadius * 2;
        SetTailTargetPosition(Vector3.forward);


        _bodyToLegsOffset = (mainBody.position.y - futureLegBases[0].position.y) * Vector3.up;

        _lastBodyPosition = mainBody.position;
    }

    // Update is called once per frame
    void Update()
    {
        NotifyTailTarget();
        UpdateInputs();
        UpdateLegsEE();

        if (!_movingBall.BallWasShot)
        {
            UpdateBallTrajectory();
        }

        if (animPlaying) animTime += Time.deltaTime;

        if (animTime < animDuration)
        {
            float t = animTime / animDuration;
            float sint = Mathf.Clamp01(t * 1.2f) * 2f * Mathf.PI;

            _moveOffset.x = Mathf.Sin(sint) * 3f;

            _currentForward = mainBody.position - _lastBodyPosition;

            if(_currentForward.sqrMagnitude > 0.0001f)
            {
                _currentForward = _currentForward.normalized;
                Debug.DrawLine(mainBody.position, mainBody.position + _currentForward * 2);
            }

            _lastBodyPosition = mainBody.position;

            Body.position = Vector3.Lerp(StartPos.position, EndPos.position, t) + _moveOffset;
            
            ComputeTailTargetPosition();

            UpdateLegsAndBody();

            RotateBody();
        }
        else if (animTime >= animDuration && animPlaying)
        {
            Body.position = EndPos.position + _moveOffset;
            animPlaying = false;
        }

        // Reset legs after updating Body's position, just in case
        if (_reset)
        {
            _myController.ResetLegs();
            _movingBall.ResetStateToStart();

            _moveOffset = Vector3.zero;
            _lastBodyPosition = mainBody.position;

            _reset = false; // toggle reset off
        }

        _myController.UpdateIK();        
    }
    
    //Function to send the tail target transform to the dll
    public void NotifyTailTarget()
    {
        _myController.NotifyTailTarget(tailTarget);
    }

    //Trigger Function to start the walk animation
    public void NotifyStartWalk()
    {
        _myController.NotifyStartWalk();
    }

    public void NotifyStopWalk()
    {
        _myController.NotifyStopWalk();
    }

    public void NotifyShoot()
    {
        NotifyStopWalk();
    }


    private void UpdateInputs()
    {              
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _uiController.ResetStrengthSlider();
            animTime = 0;
            animPlaying = false;
            _reset = true;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            _uiController.UpdateStrengthSlider();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            StartShootBall();
        }

        if (Input.GetKey(KeyCode.Z))
        {
            _uiController.UpdateEffectStrengthSlider(-1);
        }
        else if (Input.GetKey(KeyCode.X))
        {
            _uiController.UpdateEffectStrengthSlider(1);
        }
    }

    public void StartShootBall()
    {
        animPlaying = true;
        NotifyStartWalk();
        _movingBall.SetShootStrength(_uiController.GetStrengthPer1());
    }

    private void ComputeTailTargetPosition()
    {
        float goalTargetPositionX = _movingBall.GetBlueTargetPosition().x;
        float ballPositionX = _movingBall.Position.x;

        _isGoalTargetRightSide = ballPositionX < goalTargetPositionX;
        Vector3 right = _movingBall.Right * (_isGoalTargetRightSide ? -1 : 1);

        float effectStrengthPer1 = _uiController.GetEffectStrengthPer1();
        
        Vector3 offsetDirection = Vector3.Lerp(_movingBall.Forward, right, effectStrengthPer1).normalized;

        _ballHitToCenterDir = -offsetDirection;

        SetTailTargetPosition(offsetDirection);
    }

    private void SetTailTargetPosition(Vector3 offsetDirection)
    {
        tailTarget.localPosition = offsetDirection * tailTargetBallOffsetLength;
    }


    private void UpdateBallTrajectory()
    {
        _movingBall.SetShootStrength(_uiController.GetStrengthPer1());
        _movingBall.ComputeStartVelocity();
    }



    private void UpdateLegsAndBody()
    {
        Vector3 bodyLegsAvgPos = Vector3.zero;

        Vector3 leftLegsAvgPos = Vector3.zero;
        Vector3 rightLegsAvgPos = Vector3.zero;


        for (int legI = 0; legI < futureLegBases.Length; ++legI)
        {
            RaycastHit hit;

            Vector3 hitOrigin = futureLegBases[legI].position + (-_futureLegBaseProbeDirection * _futureLegBaseOriginDisplacement);

            Debug.DrawLine(hitOrigin, hitOrigin + (_futureLegBaseProbeDirection * _futureLegBaseProbeDist), Color.magenta, Time.deltaTime);

            if (Physics.Raycast(hitOrigin, _futureLegBaseProbeDirection, out hit, _futureLegBaseProbeDist))
            {
                futureLegBases[legI].position = hit.point;
            }

            bodyLegsAvgPos += futureLegBases[legI].position;


            if(legI % 2 == 0)
                rightLegsAvgPos += futureLegBases[legI].position;
            
            else
                leftLegsAvgPos += futureLegBases[legI].position;
        }


        bodyLegsAvgPos /= (float)futureLegBases.Length;
        mainBody.position = bodyLegsAvgPos + _bodyToLegsOffset;

        rightLegsAvgPos /= 3f;
        leftLegsAvgPos /= 3f;

        if(_currentForward.sqrMagnitude > 0.0001f) { 

            Vector3 newRightBodyAxis = (rightLegsAvgPos - leftLegsAvgPos).normalized;

            Vector3 newUpBodyAxis = Vector3.Cross(_currentForward, newRightBodyAxis).normalized;
            Vector3 newForwardAxis = Vector3.Cross(newUpBodyAxis, newRightBodyAxis).normalized;

            Debug.DrawLine(mainBody.position, mainBody.position - newForwardAxis * 2f);

            _desiredLookRotation = Quaternion.LookRotation(-_currentForward, newUpBodyAxis);
        }
    }

    private void RotateBody()
    {
        if (_currentForward.sqrMagnitude > 0.0001f)
        {
            mainBody.rotation = Quaternion.RotateTowards(mainBody.rotation, _desiredLookRotation, 300f * Time.deltaTime);
        }
    }

    private void UpdateLegsEE()
    {
        for(int i = 0; i < _fakeEE.Length; ++i)
        {
            _realEE[i].position = _fakeEE[i].position;
        }

    }

}
