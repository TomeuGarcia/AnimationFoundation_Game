# AnimationFoundation_Game


EXERCISE 1
==========
Ex1.1
=====
Inside MovingBall.cs in the OnCollisionEnter() is where we tell the MyOctopusController.cs
if it either has to intercept or not the ball with the NotifyShoot() function.

Ex1.2
===== 
Ball no longer moves with WASD, only goal blue target does.

Ex1.3
=====
We added a UI_Controller.sc script (attached at the Camera Canvas), which controls the strength
slider. In IK_Scorpion.cs, in UpdateInputs() we call the UI_Controller functions.
IK_Scorpion.cs, in StartShootBall() sets the ball's shootStrengthPer1 based on the strength slider 
value.

Ex1.4
=====
In the IKScorpion.cs when we press the Space Key, the ResetLegs() from MyScorpionController.cs 
is called, also ResetPosition() from MovingBall.cs.

Ex1.5
===== 
In MovingBall.cs:
- First we compute the shootTimeDuration by lerping 2 arbitrary floats using the shootStrength 
  perviously set.
- Then we compute the startVelocity given the startShootPosition, the endPosition (blue target),
  shootTimeDuration and the acceleration (gravity). 
	
UARM Formula:				startVelocity Formula:
Xf = Xo + (Vo*t) + (1/2*a*t^2)  ----->  Vo = (Xf - Xo - (1/2*a*t^2)) / t

- To compute the instantaneous position we use the UARM formula and increment t by deltaTime
  each frame



EXERCISE 2
==========
Ex2.1
=====
IK_Scorpion.cs checks the Z and X input keys and calls UI_Controller.cs UpdateEffectStrengthSlider()
method.
IK_Scorpion.cs calls its method ComputeTailTargetPosition() to change ow the tail hits the ball
depending on the effect value.

Ex2.2
===== 
MovingBall.cs computes the rotation velocity in ComputeRotationAxis() (called at OnCollisionEnter()).
...

Ex2.3
===== 
MovingBall.cs method UpdateArrows() updates the arrows' transform, it also checks the I input key
to toggle the arrows' visibility.

Ex2.4
===== 
...

Ex2.5
===== 
...

Ex2.6
===== 
...