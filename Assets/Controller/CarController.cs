using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

namespace Controller
{
    public class CarController : MonoBehaviour
    {
        
        public TextMeshProUGUI lapTimeText;
        public TextMeshProUGUI lapCountText;
        public TextMeshProUGUI lastLapTime;
        
        public float maxRadiusDelta = 90f;  // Maximum radius delta in degrees
        public float weightSpeed = 0.7f;    // Weight of speed in the probability formula
        public float weightRadius = 0.3f;   //
        
        public SplineContainer spline; // Reference to the SplineContainer
        private float _splineProgressPercentage = 0f; // Tracks progress along the spline
        private float splineLength;

        public bool isAccelerating;
        public bool isCrashing;

        [FormerlySerializedAs("speed")] public float curSpeed = 0f; // Speed of the car
        
        public float maxSpeed = 10f;
        private float Acceleration;
        private float Deceleration;
        
        private float elapsedTime = 0f; // Tracks time for lap
        private const int REQUIRED_LAPS = 3;
        private int _currentLap = 1;

        private float lastRotation;
        
        private Vector3 startPosition; // Store the initial position
        
        // The following particle systems are used as tire smoke when the car drifts.
        public ParticleSystem RLWParticleSystem;
        public ParticleSystem RRWParticleSystem;

        void Start()
        {
            isAccelerating = false;
            isCrashing = false;
            lastLapTime.SetText("");

            Acceleration = maxSpeed * 0.75f;
            Deceleration = maxSpeed * 0.75f;
            
            lastRotation = transform.rotation.y;
            splineLength = spline.CalculateLength();
            startPosition = transform.position; // Record the initial position
        }

        void Update()
        {
            if (isCrashing) return;
            
            CalcCurSpeed();
            MoveAlongSpline();
            CheckOverSpeed();
            CalcTrackTime();
            
            // Loop back to the start if the end is reached
            if (_splineProgressPercentage > 1f)
            {
                _splineProgressPercentage -= 1f;
            }
        }

        private void CalcCurSpeed()
        {
            float curAcceleration = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
            isAccelerating =  curAcceleration > 0.1f;
            
            if (isAccelerating) { curSpeed += Acceleration * Time.deltaTime; }
            else { curSpeed -= Deceleration * Time.deltaTime; }
            
            curSpeed = Mathf.Clamp(curSpeed, 0f, maxSpeed);
        }

        private void CheckOverSpeed()
        {
            float currentRotation = transform.rotation.y;
            float rotationDelta = Mathf.Abs(currentRotation - lastRotation);
            
            if (rotationDelta > 0.05f)
            {
                float normalizedSpeed = Mathf.Clamp01(curSpeed / maxSpeed);
                float normalizeDeltaY= Mathf.Clamp01(rotationDelta / maxRadiusDelta);
                
                

                // Compute weighted probability
                float probability = (weightSpeed * normalizedSpeed) + (weightRadius * normalizeDeltaY);

                // Clamp the result to ensure it is between 0 and 1
                Mathf.Clamp01(probability);
                
                print("We are in corner: " + probability);
                if (probability >= 0.3f && curSpeed >= maxSpeed * 0.4f)
                {
                    RRWParticleSystem.Play();
                    RLWParticleSystem.Play();
                }
                
                if (probability > 0.5f)
                {
                    if (Random.Range(0f, 1f) > 0.7f)
                    {
                        // FlyOffTrack();
                    }
                }
            }
            else
            {
                RRWParticleSystem.Stop();
                RLWParticleSystem.Stop();
            }
            
            lastRotation = currentRotation;
        }

        private void MoveAlongSpline()
        {
            // Increment progress based on speed
            _splineProgressPercentage += Time.deltaTime * curSpeed / splineLength;

            // Update the car's position and rotation along the spline
            spline.Evaluate(_splineProgressPercentage, out float3 position, out float3 tangent, out float3 up);
            
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(tangent, up); // Adjust rotation offset. * Quaternion.Euler(0, -90, 0)
        }

        private void FlyOffTrack()
        {
            // Temporarily disable movement
            isAccelerating = false;
            isCrashing = true;
            
            var flyOffDamper = 0.4f;

            // Add a "fly-off" force (e.g., upward and outward)
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.AddForce(transform.up * curSpeed * flyOffDamper + transform.forward * curSpeed * flyOffDamper, ForceMode.Impulse);

            // Schedule respawn after a delay
            Invoke(nameof(Respawn), 3f);
        }

        private void Respawn()
        {
            // Reset position and remove flying effects
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb); // Remove the Rigidbody to re-enable path-following

            // Reset the state
            isAccelerating = false;
            isCrashing = false;
            curSpeed = 0;
        }

        private void CalcTrackTime()
        {
            elapsedTime += Time.deltaTime;

            if (_splineProgressPercentage >= 1f && _currentLap < REQUIRED_LAPS)
            {
                _currentLap += 1;
            } else if (_splineProgressPercentage >= 1f && _currentLap == REQUIRED_LAPS)
            {
                lastLapTime.text = $"Prev. {elapsedTime:F2}s";
                _currentLap = 1;
                elapsedTime = 0f;
            }
            
            lapTimeText.text = $"Lap Time: {elapsedTime:F2} s";
            lapCountText.text = $"Lap {_currentLap}/{REQUIRED_LAPS}";
            
        }
        
    }
}