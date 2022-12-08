using System.Collections;
using System.Collections.Generic;
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

    private Vector3 _startPosition;

    private Vector3 _startShootPosition;
    private Vector3 _startVelocity;
    private readonly Vector3 _gravityVector = Vector3.down * 9.8f;
    private float _shootTime = 0f;

    private float _shootStrengthPer1 = 0f;

    float _angularVelocity;
    Vector3 _rotationAxis;
    private readonly float _maxRotationSpeed = 100f;
    private readonly float _minRotationSpeed = 0f;

    public readonly float ballRadius = 0.0016f;


    public Vector3 Position => transform.position;
    public Vector3 Forward => transform.forward;
    public Vector3 Right => transform.right;


    // Start is called before the first frame update
    void Start()
    {
        _startPosition = transform.position;
        _shootTime = 0f;
        _ballWasShot = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (_ballWasShot)
        {
            transform.position = GetPositionInTime();
            _shootTime += Time.deltaTime;
            RotateBall();
        }
        else
        {
            transform.rotation = Quaternion.identity;

            //get the Input from Horizontal axis
            float horizontalInput = Input.GetAxis("Horizontal");
            //get the Input from Vertical axis
            float verticalInput = Input.GetAxis("Vertical");

            //update the position
            //transform.position = transform.position + new Vector3(-horizontalInput * _movementSpeed * Time.deltaTime, verticalInput * _movementSpeed * Time.deltaTime, 0);
        }
        

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
        float shootTimeDuration = Mathf.Lerp(2.5f, 0.5f, _shootStrengthPer1);
        
        _startShootPosition = transform.position;

        // Xf = Xo + Vo*t + 1/2*a*t^2
        _startVelocity = _blueTarget.Position - _startShootPosition - (0.5f * _gravityVector * Mathf.Pow(shootTimeDuration, 2));
        _startVelocity /= shootTimeDuration;
    }

    public Vector3 GetPositionInTime()
    {
        return _startShootPosition + (_startVelocity * _shootTime) + (0.5f * _gravityVector * Mathf.Pow(_shootTime, 2));
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

}
