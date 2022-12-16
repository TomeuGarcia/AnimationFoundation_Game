using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OctopusController
{
    public enum TentacleMode { LEG, TAIL, TENTACLE };

    public class MyOctopusController
    {

        MyTentacleController[] _tentacles = new MyTentacleController[4];

        Transform _currentRegion;
        Transform _target;

        Transform[] _randomTargets;// = new Transform[4];
        int _tentacleToTargetIndex = -1; // start at 0 
        bool _interceptShotBall;

        float _twistMin, _twistMax;
        float _swingMin, _swingMax;


        float _theta;
        float _sin;
        float _cos;

        private Dictionary<Transform, int> regionToTentacleIndex;


        // Max number of tries before the system gives up (Maybe 10 is too high?)
        private int _mtries = 10;
        // The number of tries the system is at now
        private int[] _tries;

        // the range within which the target will be assumed to be reached
        readonly float _epsilon = 0.1f;

        // To check if the target is reached at any point
        bool _done = false;

        // To store the position of the target
        private Vector3[] tpos;


        readonly float _targetDuration = 3f;
        float _targetTimer = 0f;



        #region public methods
        //DO NOT CHANGE THE PUBLIC METHODS!!

        public float TwistMin { set => _twistMin = value; }
        public float TwistMax { set => _twistMax = value; }
        public float SwingMin { set => _swingMin = value; }
        public float SwingMax { set => _swingMax = value; }


        public void TestLogging(string objectName)
        {


            Debug.Log("hello, I am initializing my Octopus Controller in object " + objectName);


        }

        public void Init(Transform[] tentacleRoots, Transform[] randomTargets)
        {
            _randomTargets = randomTargets;

            _tentacles = new MyTentacleController[tentacleRoots.Length];
            tpos = new Vector3[tentacleRoots.Length];
            _tries = new int[tentacleRoots.Length];
            regionToTentacleIndex = new Dictionary<Transform, int>();


            // foreach (Transform t in tentacleRoots)
            for (int i = 0; i < tentacleRoots.Length; i++)
            {

                _tentacles[i] = new MyTentacleController();
                _tentacles[i].LoadTentacleJoints(tentacleRoots[i], TentacleMode.TENTACLE);

                //TODO: initialize any variables needed in ccd
                tpos[i] = randomTargets[i].position;
                _tries[i] = 0;

                //TODO: use the regions however you need to make sure each tentacle stays in its region
                regionToTentacleIndex.Add(randomTargets[i].parent, i);
            }

            _tentacleToTargetIndex = -1;
            _interceptShotBall = false;
            _targetTimer = 0f;
        }


        public void NotifyTarget(Transform target, Transform region)
        {
            if (!_interceptShotBall || _targetTimer >= _targetDuration) return;

            _currentRegion = region;
            _target = target;



            if (regionToTentacleIndex.ContainsKey(region))
            {
                _tentacleToTargetIndex = regionToTentacleIndex[region];
            }

        }

        public void NotifyShoot(bool interceptShotBall)
        {
            //TODO. what happens here?
            Debug.Log("Shoot");

            _interceptShotBall = interceptShotBall;
            if (interceptShotBall)
            {
                _targetTimer = 0f;
            }
        }


        public void UpdateTentacles()
        {
            //TODO: implement logic for the correct tentacle arm to stop the ball and implement CCD method
            update_ccd();

            if (_interceptShotBall)
            {
                if (_targetTimer < _targetDuration)
                {
                    _targetTimer += Time.deltaTime;
                }
                else
                {
                    _tentacleToTargetIndex = -1;
                }

            }
        }




        #endregion


        #region private and internal methods
        //todo: add here anything that you need

        void update_ccd()
        {

            for (int tentacleI = 0; tentacleI < _tentacles.Length; ++tentacleI)
            {
                Transform[] tentacleBones = _tentacles[tentacleI].Bones;

                Transform tentacleTarget = (_interceptShotBall && tentacleI == _tentacleToTargetIndex) ? _target : _randomTargets[tentacleI];

                _done = false;
                if (!_done)
                {

                    if (_tries[tentacleI] <= _mtries)
                    {
                        for (int i = tentacleBones.Length - 2; i >= 0; i--)
                        {
                            // The vector from the ith joint to the end effector
                            Vector3 r1 = (tentacleBones[tentacleBones.Length - 1].transform.position - tentacleBones[i].transform.position).normalized;

                            // The vector from the ith joint to the target
                            Vector3 r2 = (tentacleTarget.position - tentacleBones[i].transform.position).normalized;

                            // to avoid dividing by tiny numbers
                            if (r1.magnitude * r2.magnitude <= 0.001f)
                            {
                                // cos ? sin? 
                                _cos = 1.0f;
                                _sin = 0.0f;
                            }
                            else
                            {
                                // find the components using dot and cross product
                                float dot = Vector3.Dot(r1, r2);
                                _cos = dot;
                                Vector3 cross = Vector3.Cross(r1, r2);
                                _sin = cross.magnitude;
                            }


                            // The axis of rotation 
                            Vector3 axis = Vector3.Cross(r1, r2).normalized;

                            // find the angle between r1 and r2 (and clamp values if needed avoid errors)
                            _theta = Mathf.Acos(Mathf.Clamp(_cos, -1f, 1f));

                            //Optional. correct angles if needed, depending on angles invert angle if sin component is negative
                            if (_sin < 0.0f)
                                _theta = -_theta;

                            if(_theta > 180.0f)
                            {
                                _theta = 180.0f - _theta;
                            }


                            // obtain an angle value between -pi and pi, and then convert to degrees
                            _theta = _theta * Mathf.Rad2Deg;

                            // rotate the ith joint along the axis by theta degrees in the world space.
                            tentacleBones[i].transform.rotation = Quaternion.AngleAxis(_theta, axis) * tentacleBones[i].transform.rotation;

                            Quaternion swingLocalRotation = GetSwing(tentacleBones[i].transform.localRotation, Vector3.up);

                            Vector3 clampedSwing = swingLocalRotation.eulerAngles;
                            if(clampedSwing.x > 180.0f)
                            {
                                clampedSwing.x = 180.0f - clampedSwing.x;
                            }
                            clampedSwing.x = Mathf.Clamp(clampedSwing.x, -20.0f, 20.0f);

                            if (clampedSwing.z > 180.0f)
                            {
                                clampedSwing.z = 180.0f - clampedSwing.z;
                            }
                            clampedSwing.z = Mathf.Clamp(clampedSwing.z, -20.0f, 20.0f);

                            Quaternion clampedRotation = Quaternion.Euler(clampedSwing.x,clampedSwing.y,clampedSwing.z);

                            tentacleBones[i].transform.localRotation = clampedRotation;

                            ++_tries[tentacleI];
                        }
                    }

                    // find the difference in the positions of the end effector and the target
                    Vector3 targetToEffector = tentacleBones[tentacleBones.Length - 1].transform.position - tentacleTarget.position;

                    // if target is within reach (within epsilon) then the process is done
                    if (targetToEffector.magnitude < _epsilon)
                    {
                        _done = true;
                    }
                    // if it isn't, then the process should be repeated
                    else
                    {
                        _done = false;
                    }

                    // the target has moved, reset tries to 0 and change tpos
                    if (tentacleTarget.position != tpos[tentacleI])
                    {
                        _tries[tentacleI] = 0;
                        tpos[tentacleI] = tentacleTarget.position;
                    }

                }

                _tentacles[tentacleI].EndEffectorSphere = tentacleBones[tentacleBones.Length - 1];

            }



        }

        private Quaternion GetTwist(Quaternion rotation , Vector3 twistAxis)
        {
            return new Quaternion(rotation.x * twistAxis.x, rotation.y * twistAxis.y, rotation.z * twistAxis.z, rotation.w);
        }

        private Quaternion GetSwing(Quaternion rotation, Vector3 twistAxis)
        {
            return rotation * Quaternion.Inverse(GetTwist(rotation, twistAxis));
        }




        #endregion






    }
}
