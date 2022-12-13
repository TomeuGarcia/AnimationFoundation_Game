using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Security.Cryptography;
using UnityEngine;

public class MovingBall : MonoBehaviour
{
    [SerializeField]
    IK_tentacles _myOctopus;

    [SerializeField]
    IK_Scorpion _myScorpion;

    [Header("UI Controller")]
    [SerializeField] private UI_Controller _uiController;

    [Header("Blue Target")]
    [SerializeField] private MovingTarget _blueTarget;

    //movement speed in units per second
    [Header("Movement")]
    [SerializeField, Range(-1.0f, 1.0f)] private float _movementSpeed = 5f;

    Vector3 _dir;

    private bool _interceptShotBall = true;

    private bool _ballWasShot = false;
    public bool BallWasShot => _ballWasShot;

    private Vector3 _startPosition;

    private Vector3 _startShootPosition;
    private Vector3 _startVelocity;
    private readonly Vector3 _gravityVector = Vector3.down * 9.8f;
    private float _shootTime = 0f;

    private float _shootTimeDuration;

    private float _shootStrengthPer1 = 0f;

    //MAGNUS

    Vector3 _instantLinearVelocity;
    Vector3 _angularVelocity;
    Vector3 _rotationAxis;
    Vector3 _magnusForce;

    Vector3 _acceleration;
    [SerializeField] private Transform _tailTarget;

    public readonly float _ballRadius = 0.0016f;



    // ARROWS & POINTS
    [Header("Arrows")]
    private const int _numArrows = 20;
    private const int _numPoints = 40;
    private bool _areArrowsVisible = true;
    [SerializeField] private GameObject _greyArrowPrefab;
    [SerializeField] private GameObject _bluePointsPrefab;

    [SerializeField] private GameObject _arrowContainer;

    private Transform[] _greyArrows;
    private Transform[] _bluePoints;


    [SerializeField] private Transform _greenArrow;





    public Vector3 Position => transform.position;
    public Vector3 Forward => transform.forward;
    public Vector3 Right => transform.right;

    void Awake()
    {
        _greyArrows = new Transform[_numArrows];

        for(int i = 0; i < _numArrows; i++)
        {
            _greyArrows[i] = Instantiate(_greyArrowPrefab, _arrowContainer.transform).transform; 
        }

        _bluePoints = new Transform[_numPoints];

        for (int i = 0; i < _numPoints; i++)
        {
            _bluePoints[i] = Instantiate(_bluePointsPrefab, _arrowContainer.transform).transform;
        }



    }

    // Start is called before the first frame update
    void Start()
    {
        _startPosition = transform.position;
        _shootTime = 0f;
        _ballWasShot = false;

        _areArrowsVisible = true;

        ResetBluePoints();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInputs();

        if (_ballWasShot)
        {
            //transform.position = GetPositionInTime(_shootTime, _acceleration);
            //_instantLinearVelocity = GetVelocityInTime(_shootTime, _acceleration);

            transform.position = GetEulerPosition(transform.position, _instantLinearVelocity);
            _instantLinearVelocity = GetEulerVelocity(_instantLinearVelocity, _acceleration);

            _acceleration = ComputeAcceleration(_angularVelocity, _instantLinearVelocity);
            _shootTime += Time.deltaTime;
            RotateBall();
            SetGreenArrowsTransform();

            if (_shootTime <= _shootTimeDuration)
            {
                //SetBluePointsTransforms();
                //_bluePoints.transform.position = Position;
                SetBluePointsTransforms();
            }

        }
        else
        {
            if (_areArrowsVisible) UpdateArrows();

            transform.rotation = Quaternion.identity;

            _acceleration = _gravityVector;

            //get the Input from Horizontal axis
            float horizontalInput = Input.GetAxis("Horizontal");
            //get the Input from Vertical axis
            float verticalInput = Input.GetAxis("Vertical");
            

            //update the position
            //transform.position = transform.position + new Vector3(-horizontalInput * _movementSpeed * Time.deltaTime, verticalInput * _movementSpeed * Time.deltaTime, 0);
        }

    }

    private void UpdateInputs()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            _areArrowsVisible = !_areArrowsVisible;
            _arrowContainer.SetActive(_areArrowsVisible);
        }
    }

    private void UpdateArrows()
    {
        SetGreyArrowsTransforms();
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (_ballWasShot) return;

        _myOctopus.NotifyShoot(_interceptShotBall);
        _interceptShotBall = !_interceptShotBall;

        _myScorpion.NotifyShoot();

        ComputeStartVelocity();
        _ballWasShot = true;
        _blueTarget.canMove = false;
        _greenArrow.gameObject.SetActive(true);

        _angularVelocity = ComputeAngularVelocity();
        ComputeRotationAxis();

        _instantLinearVelocity = _startVelocity;
        SetBluePointsTransforms();
    }

    public void ResetPosition()
    {
        transform.position = _startPosition;
        _ballWasShot = false;
        _shootTime = 0f;

        _blueTarget.ResetPosition();
        _blueTarget.canMove = true;
        _greenArrow.gameObject.SetActive(false);

        ResetBluePoints();
    }

    private void ResetBluePoints()
    {
        _greenArrow.gameObject.SetActive(true);
        _arrowContainer.SetActive(_areArrowsVisible);
        _greenArrow.gameObject.SetActive(false);
    }


    public void SetShootStrength(float strength)
    {
        _shootStrengthPer1 = strength;
    }

    public void ComputeStartVelocity()
    {
        _shootTimeDuration = Mathf.Lerp(2.5f, 0.5f, _shootStrengthPer1);
        
        _startShootPosition = transform.position;

        // Xf = Xo + Vo*t + 1/2*a*t^2
        _startVelocity = _blueTarget.Position - _startShootPosition - (0.5f * _gravityVector * Mathf.Pow(_shootTimeDuration, 2));
        _startVelocity /= _shootTimeDuration;
    }

    public Vector3 ComputeAcceleration(Vector3 angularVelocity, Vector3 instantLinearVelocity)
    {
        _magnusForce = ComputeMagnusForce(angularVelocity, instantLinearVelocity);
        return _gravityVector + _magnusForce;
    }

    private Vector3 ComputeAngularVelocity()
    {
        //We have created all these Vector3 to make it more llegible
        Vector3 impactPoint = ((_tailTarget.position - Position).normalized * _ballRadius);
        Vector3 angularMomentum = Vector3.Cross(impactPoint,_startVelocity); //sliderValue * target - position
        Vector3 torque = angularMomentum;
        Vector3 angularVelocity = torque * Mathf.Lerp(0,10,_uiController.GetEffectStrengthPer1());
        Debug.Log(angularVelocity);
        return angularVelocity;
    }

    private Vector3 ComputeMagnusForce(Vector3 angularVelocity, Vector3 instantLinearVelocity)
    {
        return Vector3.Cross(angularVelocity, instantLinearVelocity);
    }



    public Vector3 GetPositionInTime(float time, Vector3 acceleration)
    {
        return _startShootPosition + (_startVelocity * time) + (0.5f * acceleration * Mathf.Pow(time, 2));
    }

    public Vector3 GetVelocityInTime(float time, Vector3 acceleration)
    {
        return _startVelocity + acceleration * time;
    }

    public Vector3 GetEulerPosition(Vector3 position, Vector3 velocity)
    {
        return position + velocity * Time.deltaTime; 
    }

    public Vector3 GetEulerVelocity(Vector3 velocity, Vector3 acceleration)
    {
        return velocity + acceleration * Time.deltaTime;
    }


    public Vector3 GetBlueTargetPosition()
    {
        return _blueTarget.Position;
    }


    private void ComputeRotationAxis()
    {
        Vector3 ballToGoalTargetDir = (_blueTarget.Position - Position).normalized;
        Vector3 ballHitToCenterDir = _myScorpion.BallHitToCenterDir.normalized;

        float dot = Vector3.Dot(ballToGoalTargetDir, ballHitToCenterDir);

        if (dot > 0.999f)
        {
            _rotationAxis = Vector3.zero;
        }
        else
        {
            _rotationAxis = Vector3.Cross(ballToGoalTargetDir, ballHitToCenterDir);
        }

    }

    private void RotateBall()
    {
        float angleRotationPerSecond = _angularVelocity.magnitude * Mathf.Rad2Deg;

        transform.Rotate(_angularVelocity.normalized, angleRotationPerSecond * Time.deltaTime);
        Debug.Log(transform.rotation);

        _uiController.SetAngularVelocityText(angleRotationPerSecond); // TODO fix
    }


    private void SetGreyArrowsTransforms()
    {
        float timeStep = _shootTimeDuration / _numArrows;
        float accumulatedTime = 0;
        Vector3 futurePosition = GetPositionInTime(accumulatedTime, _gravityVector);

        for (int i = 0; i < _greyArrows.Length; i++)
        {
            _greyArrows[i].position = futurePosition;

            futurePosition = GetPositionInTime(accumulatedTime + timeStep, _gravityVector);
            _greyArrows[i].rotation = Quaternion.LookRotation((futurePosition - _greyArrows[i].position).normalized, Vector3.up);

            accumulatedTime += timeStep;
        }
    }

    private void SetBluePointsTransforms()
    {
        float timeStep = _shootTimeDuration / _numArrows;
        Vector3 position = _startPosition;
        Vector3 velocity = _startVelocity;

        float accumulatedTime = 0;
        Vector3 futurePosition = GetPositionInTime(accumulatedTime,ComputeAcceleration(_angularVelocity,_startVelocity));


        for (int i = 0; i < _numPoints ; ++i)
        {
            _bluePoints[i].position = futurePosition;

            //futurePosition = GetPositionInTime(accumulatedTime + timeStep, ComputeAcceleration(_angularVelocity,GetVelocityInTime(accumulatedTime + timeStep,) ));


            //_bluePoints[i].position = position = GetEulerPosition(position,velocity);

            // velocity = GetEulerVelocity(velocity, ComputeAcceleration(_angularVelocity,velocity));
        }
    }


    private void SetGreenArrowsTransform()
    {

        //--------------------------NEED TO CHANGE AND ADD MAGNUS FORCES------------------------

        _greenArrow.rotation = Quaternion.LookRotation(_instantLinearVelocity.normalized, Vector3.up);
    }

}
