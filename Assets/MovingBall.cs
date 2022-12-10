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

    float _angularVelocity;
    Vector3 _rotationAxis;
    private readonly float _maxRotationSpeed = 100f;
    private readonly float _minRotationSpeed = 0f;

    public readonly float ballRadius = 0.0016f;

    // Movement Arrows
    [Header("Arrows")]
    private const int _numArrows = 20;
    private bool _areArrowsVisible = true;
    [SerializeField] private GameObject _greyArrowPrefab;
    [SerializeField] private GameObject _arrowContainer;

    private Transform[] _greyArrows;
    private Transform[] _blueArrows;



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
    }

    // Start is called before the first frame update
    void Start()
    {
        _startPosition = transform.position;
        _shootTime = 0f;
        _ballWasShot = false;

        _areArrowsVisible = true;
        _arrowContainer.SetActive(_areArrowsVisible);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInputs();

        if (_ballWasShot)
        {
            transform.position = GetPositionInTime(_shootTime);
            _shootTime += Time.deltaTime;
            RotateBall();
        }
        else
        {
            if (_areArrowsVisible) UpdateArrows();

            transform.rotation = Quaternion.identity;

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

        ComputeRotationAxis();
    }

    public void ResetPosition()
    {
        transform.position = _startPosition;
        _ballWasShot = false;
        _shootTime = 0f;

        _blueTarget.ResetPosition();
        _blueTarget.canMove = true;
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

    public Vector3 GetPositionInTime(float time)
    {
        return _startShootPosition + (_startVelocity * time) + (0.5f * _gravityVector * Mathf.Pow(time, 2));
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

        //float angularVelocity = Mathf.Lerp(0f, 100f, 1f - dot);
        float ballRadius = 0.5f;
        _angularVelocity = _startVelocity.magnitude / ballRadius;

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
        transform.Rotate(_rotationAxis, (_angularVelocity * Mathf.Rad2Deg) * Time.deltaTime);

        _uiController.SetAngularVelocityText(_angularVelocity * Mathf.Rad2Deg); // TODO fix
    }


    private void SetGreyArrowsTransforms()
    {
        float timeStep = _shootTimeDuration / _numArrows;
        float accumulatedTime = 0;
        Vector3 futurePosition = GetPositionInTime(accumulatedTime);

        for (int i = 0; i < _greyArrows.Length; i++)
        {
            _greyArrows[i].position = futurePosition;

            futurePosition = GetPositionInTime(accumulatedTime + timeStep);
            _greyArrows[i].rotation = Quaternion.LookRotation((futurePosition - _greyArrows[i].position).normalized, Vector3.up);

            accumulatedTime += timeStep;
        }
    }

}
