using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    public bool rotateX = true; // Whether to rotate around the X-axis
    public bool rotateY = true; // Whether to rotate around the Y-axis
    public bool rotateZ = true; // Whether to rotate around the Z-axis
    public bool unscaledTime = true; // Whether to use unscaled time
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 360f; // Degrees per second (360 = one full rotation per second)
    
    [Space(10)]
    [Header("Wheel-Specific Settings (Optional)")]
    public bool isWheel = false; // Enable wheel-specific rotation
    [Range(-1, 1)]
    public int direction = 1; // 1 for forward, -1 for backward
    public float wheelRadius = 0.5f; // Optional: For calculating rotation based on movement
    
    private Vector3 rotationAxis;
    private float currentSpeedMultiplier = 1f;
    private bool isRotating = true;

    private void Start()
    {
        // Calculate initial rotation axis based on selections
        UpdateRotationAxis();
        
        // If this is marked as a wheel and we're rotating around Z, adjust for typical wheel orientation
        if (isWheel && rotateZ)
        {
            // For typical wheels, they rotate around their local Z axis
            transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
    }

    private void Update()
    {
        if (!isRotating) return;
        
        // Calculate delta time
        float deltaTime = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        
        // Calculate rotation amount for this frame
        float rotationAmount = rotationSpeed * currentSpeedMultiplier * deltaTime * direction;
        
        // Apply rotation
        if (rotateX || rotateY || rotateZ)
        {
            transform.Rotate(rotationAxis * rotationAmount, Space.Self);
        }
    }

    private void UpdateRotationAxis()
    {
        // Normalize the rotation axis based on which axes are selected
        Vector3 axis = Vector3.zero;
        if (rotateX) axis.x = 1;
        if (rotateY) axis.y = 1;
        if (rotateZ) axis.z = 1;
        
        // If multiple axes are selected, normalize to prevent faster rotation
        rotationAxis = axis.normalized;
    }

    // Public methods to control rotation
    public void StartRotation()
    {
        isRotating = true;
    }

    public void StopRotation()
    {
        isRotating = false;
    }

    public void PauseRotation()
    {
        isRotating = false;
    }

    public void ResumeRotation()
    {
        isRotating = true;
    }

    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }

    public void SetDirection(int newDirection)
    {
        direction = Mathf.Clamp(newDirection, -1, 1);
    }

    public void ReverseDirection()
    {
        direction *= -1;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeedMultiplier = Mathf.Max(0, multiplier);
    }

    public void ResetSpeed()
    {
        currentSpeedMultiplier = 1f;
    }

    // Method to simulate wheel rotation based on movement speed
    public void SetRotationBasedOnSpeed(float movementSpeed)
    {
        if (!isWheel || wheelRadius <= 0) return;
        
        // Calculate angular velocity: ω = v / r
        // Convert to degrees per second (360 degrees = 2π radians)
        float angularVelocityRadPerSec = movementSpeed / wheelRadius;
        float angularVelocityDegPerSec = angularVelocityRadPerSec * Mathf.Rad2Deg;
        
        rotationSpeed = Mathf.Abs(angularVelocityDegPerSec);
        direction = movementSpeed >= 0 ? 1 : -1;
    }

    // Optional: Smooth acceleration/deceleration
    public void SmoothSetSpeed(float targetSpeed, float duration)
    {
        StartCoroutine(SmoothSpeedChange(targetSpeed, duration));
    }

    private IEnumerator SmoothSpeedChange(float targetSpeed, float duration)
    {
        float startSpeed = rotationSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Smooth interpolation
            rotationSpeed = Mathf.Lerp(startSpeed, targetSpeed, t);
            
            yield return null;
        }
        
        rotationSpeed = targetSpeed;
    }

    // Gizmos for visualization (optional)
    private void OnDrawGizmosSelected()
    {
        if (!isWheel) return;
        
        // Draw wheel axis
        Gizmos.color = Color.red;
        Vector3 axisStart = transform.position - transform.right * wheelRadius;
        Vector3 axisEnd = transform.position + transform.right * wheelRadius;
        Gizmos.DrawLine(axisStart, axisEnd);
        
        // Draw wheel circumference
        Gizmos.color = Color.blue;
        DrawCircle(transform.position, transform.right, wheelRadius, 32);
    }

    private void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments)
    {
        Vector3 v1 = Vector3.Cross(normal, Vector3.up);
        if (v1.magnitude < 0.1f)
            v1 = Vector3.Cross(normal, Vector3.forward);
        
        v1.Normalize();
        Vector3 v2 = Vector3.Cross(normal, v1);
        
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + (v1 * Mathf.Cos(angle1) + v2 * Mathf.Sin(angle1)) * radius;
            Vector3 point2 = center + (v1 * Mathf.Cos(angle2) + v2 * Mathf.Sin(angle2)) * radius;
            
            Gizmos.DrawLine(point1, point2);
        }
    }

    // Toggle methods for runtime control
    public void ToggleRotation()
    {
        isRotating = !isRotating;
    }

    public void ToggleUnscaledTime()
    {
        unscaledTime = !unscaledTime;
    }

    public void ToggleAxis(string axis)
    {
        switch (axis.ToLower())
        {
            case "x":
                rotateX = !rotateX;
                break;
            case "y":
                rotateY = !rotateY;
                break;
            case "z":
                rotateZ = !rotateZ;
                break;
        }
        UpdateRotationAxis();
    }

    // OnEnable/OnDisable for component lifecycle
    private void OnEnable()
    {
        isRotating = true;
    }

    private void OnDisable()
    {
        isRotating = false;
    }
}