using Meta.XR.ImmersiveDebugger.UserInterface;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

namespace Controller
{
    public class CarController : MonoBehaviour
    {
        
        public TextMeshProUGUI lapTimeText;
        public TextMeshProUGUI lapCountText;
        public TextMeshProUGUI lastLapTime;
        
        public SplineContainer spline; // Reference to the SplineContainer
        private float _splineProgressPercentage = 0f; // Tracks progress along the spline
        private float splineLength;

        public bool isAccelerating;
        public bool isCrashing;

        [FormerlySerializedAs("speed")] public float curSpeed = 0f; // Speed of the car
        
        public float maxSpeed = 15f;
        public float Acceleration = 5f;
        public float Deceleration = 10f;
        
        private float elapsedTime = 0f; // Tracks time for lap
        private const int REQUIRED_LAPS = 3;
        private int _currentLap = 1;
        
        private Vector3 startPosition; // Store the initial position

        void Start()
        {
            isAccelerating = false;
            isCrashing = false;
            lastLapTime.SetText("");

            splineLength = spline.CalculateLength();
            startPosition = transform.position; // Record the initial position
        }

        void Update()
        {
            
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

        private void SetAcceleration(bool state)
        {
            isAccelerating = state;
        }

        public void SetSpeed(float newSpeed)
        {
            curSpeed = newSpeed;
            SetAcceleration(newSpeed > 0);
        }

        private void CheckOverSpeed()
        {
            //TODO: Make new check over speed based on curve angle
        }

        private void MoveAlongSpline()
        {
            // Increment progress based on speed
            _splineProgressPercentage += Time.deltaTime * curSpeed / splineLength;

            // Update the car's position and rotation along the spline
            spline.Evaluate(_splineProgressPercentage, out float3 position, out float3 tangent, out float3 up);
            
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(tangent, up) * Quaternion.Euler(0, -90, 0); // Adjust rotation offset.
        }

        private void FlyOffTrack()
        {
            // Temporarily disable movement
            isAccelerating = false;
            isCrashing = true;

            // Add a "fly-off" force (e.g., upward and outward)
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.AddForce(transform.up * 5f + transform.forward * 10f, ForceMode.Impulse);

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