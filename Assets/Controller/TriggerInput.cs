using UnityEngine;
using UnityEngine.InputSystem;

namespace Controller
{
    public class TriggerInput : MonoBehaviour
    {
        public InputActionAsset inputActions; // Drag your Input Actions asset here
        private InputAction triggerAction;

        public CarController car;

        [Range(0, 1)] public float triggerValue; // To monitor trigger value in the Inspector

        void OnEnable()
        {
            // Enable the action map and find the trigger action
            triggerAction = inputActions.FindAction("Accelerate", true);
            triggerAction.Enable();
        }

        void Update()
        {
            // Read the trigger value (0 to 1)
            // triggerValue = triggerAction.ReadValue<float>();
            
            // Example usage: Print or control acceleration
            
            float acceleration = car.maxSafeSpeed * 1.2f * triggerValue;
            
            if (!car.isCrashing) car.SetSpeed(acceleration);
            
            // Example usage: Print or control acceleration
            // Debug.Log($"Acceleration: {acceleration}");
        }

        void OnDisable()
        {
            // Disable the action map
            triggerAction.Disable();
        }
    }
}