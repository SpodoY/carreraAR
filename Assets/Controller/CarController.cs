using System;
using System.Collections;
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
        
        public TextMeshPro lapTimeText;
        public TextMeshPro lapCountText;
        public TextMeshPro lastLapTime;
        
        public float maxRadiusDelta = 90f;  // Maximum radius delta in degrees
        public float weightSpeed = 0.6f;    // Weight of speed in the probability formula
        public float weightRadius = 0.4f;   //
        
        public SplineContainer spline; // Reference to the SplineContainer
        private float _splineProgressPercentage = 0f; // Tracks progress along the spline
        private float splineLength;

        public bool isAccelerating;
        public bool isCrashing;

        [FormerlySerializedAs("speed")] public float curSpeed = 0f; // Speed of the car
        
        private float maxSpeed;
        private float maxFlyOffSpeed;
        private float Acceleration;
        private float Deceleration;
        
        private float elapsedTime = 0f; // Tracks time for lap
        private float bestTime = float.MaxValue; // Tracks time for lap
        private const int REQUIRED_LAPS = 3;
        private int _currentLap = 1;

        private float lastRotation;
        private Vector3 flyOffPosition; // Store the initial position
        public Rigidbody rb;
        
        // The following particle systems are used as tire smoke when the car drifts.
        public ParticleSystem RLWParticleSystem;
        public ParticleSystem RRWParticleSystem;

        void Start()
        {
            FormatBestLapTime();
            
            isAccelerating = false;
            isCrashing = false;

            maxSpeed = 20f;
            maxFlyOffSpeed = 20f;
            Acceleration = maxSpeed * 0.75f;
            Deceleration = maxSpeed * 0.75f;
            
            lastRotation = transform.rotation.y;
            splineLength = spline.CalculateLength();
            flyOffPosition = transform.position; // Record the initial position
        }

        void Update()
        {
            if (!isCrashing)
            {
                CalcCurSpeed();
                MoveAlongSpline();
                CheckOverSpeed();
            }
            FlyOffTrack();
            CalcTrackTime();
            
            // Loop back to the start if the end is reached
            if (_splineProgressPercentage > 1f)
            {
                _splineProgressPercentage -= 1f;
            }
        }

        private void FormatBestLapTime()
        {
            lastLapTime.text = bestTime == float.MaxValue ? $"Best time{Environment.NewLine}Not set yet" : $"Best time{Environment.NewLine}{bestTime:F2}";
        }

        private void CalcCurSpeed()
        {
            float curAcceleration = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
            
            isAccelerating =  curAcceleration > 0.1f || Input.GetMouseButton(0);
            
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

                // Compute weighted probability and clamp the result to ensure it is between 0 and 1
                float probability = Mathf.Clamp01(weightSpeed * normalizedSpeed) + (weightRadius * normalizeDeltaY);
                
                // print("We are in corner: " + probability);
                if (probability >= 0.3f && curSpeed >= maxSpeed * 0.4f)
                {
                    RRWParticleSystem.Play();
                    RLWParticleSystem.Play();
                }
                
                if (probability > 0.5f)
                {
                    if (Random.Range(0f, 1f) > 0.9f)
                    {
                        // Temporarily disable movement
                        isCrashing = true;
                        isAccelerating = false;
                        
                        // Schedule respawn after a delay
                        Invoke(nameof(Respawn), 3f);
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
            // Skip if car not crashing
            if (!isCrashing) return;
            
            // Add a "fly-off" force (e.g., upward and outward)
            Vector3 flyOffDir = 5f * Vector3.up + transform.forward * 3f;
            rb.AddForce(flyOffDir * maxFlyOffSpeed, ForceMode.Impulse);

            StartCoroutine(ApplyDrag());
        }
        
        private IEnumerator ApplyDrag()
        {
            float originalDrag = rb.drag;
            rb.drag = 0.5f;

            yield return new WaitForSeconds(3f);  // Apply drag for 3 second
            
            rb.drag = originalDrag;  // Reset drag after the car has slowed
        }

        private void Respawn()
        {
            // Reset position and remove flying effects
            transform.position = flyOffPosition;
            transform.rotation = Quaternion.identity;

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
                if (elapsedTime < bestTime) bestTime = elapsedTime;
                FormatBestLapTime();
                _currentLap = 1;
                elapsedTime = 0f;
            }
            
            lapTimeText.text = $"Lap Time: {elapsedTime:F2} s";
            lapCountText.text = $"Lap {_currentLap}/{REQUIRED_LAPS}";
            
        }
        
    }
}