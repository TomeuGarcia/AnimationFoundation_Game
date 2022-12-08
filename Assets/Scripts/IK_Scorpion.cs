﻿using System.Collections;
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


    // Start is called before the first frame update
    void Start()
    {
        _myController.InitLegs(legs,futureLegBases,legTargets);
        _myController.InitTail(tail);

        tailTargetBallOffsetLength = _movingBall.ballRadius * 5;
        SetTailTargetPosition(Vector3.forward);
    }

    // Update is called once per frame
    void Update()
    {
        if(animPlaying)
            animTime += Time.deltaTime;

        NotifyTailTarget();
        UpdateInputs();


        if (animTime < animDuration)
        {
            Body.position = Vector3.Lerp(StartPos.position, EndPos.position, animTime / animDuration);
            
            ComputeTailTargetPosition();
        }
        else if (animTime >= animDuration && animPlaying)
        {
            Body.position = EndPos.position;
            animPlaying = false;
        }

        // Reset legs after updating Body's position, just in case
        if (_reset)
        {
            _myController.ResetLegs();
            _movingBall.ResetPosition();
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
        Vector3 offsetDirection = Vector3.Lerp(_movingBall.Forward, right, effectStrengthPer1);
        _ballHitToCenterDir = -offsetDirection;

        SetTailTargetPosition(offsetDirection);
    }

    private void SetTailTargetPosition(Vector3 offsetDirection)
    {
        tailTarget.localPosition = offsetDirection * tailTargetBallOffsetLength;
    }

}
