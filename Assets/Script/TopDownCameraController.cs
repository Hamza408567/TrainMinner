using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -5);
    [SerializeField] private float followSpeed = 5f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float xRotation = 45f; // X-axis rotation (pitch)
    [SerializeField] private float zRotation = 0f;  // Z-axis rotation (roll, usually 0 for top-down)
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool lookAtTarget = false;
    
    [Header("Smooth Damping Settings")]
    [SerializeField] private bool useSmoothDamping = true;
    [SerializeField] private float positionSmoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.3f;
    
    [Header("Boundaries (Optional)")]
    [SerializeField] private bool useBoundaries = false;
    [SerializeField] private Vector2 boundaryMin = new Vector2(-50, -50);
    [SerializeField] private Vector2 boundaryMax = new Vector2(50, 50);
    
    private Vector3 positionVelocity = Vector3.zero;
    private float rotationVelocity = 0f;
    private Quaternion targetRotation;
    private float currentXRotation;
    private float currentZRotation;
    
    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target not assigned! Please assign a target in the inspector.");
            return;
        }
        
        // Set initial values
        currentXRotation = xRotation;
        currentZRotation = zRotation;
        
        // Set initial position and rotation
        transform.position = target.position + offset;
        
        if (lookAtTarget)
        {
            transform.LookAt(target);
            // Extract current rotation from lookat
            currentXRotation = transform.rotation.eulerAngles.x;
            currentZRotation = transform.rotation.eulerAngles.z;
            targetRotation = transform.rotation;
        }
        else
        {
            targetRotation = Quaternion.Euler(currentXRotation, 0, currentZRotation);
            transform.rotation = targetRotation;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate target position with offset
        Vector3 targetPosition = target.position + offset;
        
        // Apply boundaries if enabled
        if (useBoundaries)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, boundaryMin.x, boundaryMax.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, boundaryMin.y, boundaryMax.y);
        }
        
        // Smooth position movement
        if (useSmoothDamping)
        {
            // SmoothDamp for smooth camera movement
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref positionVelocity, 
                positionSmoothTime
            );
        }
        else
        {
            // Linear interpolation for smoother movement
            transform.position = Vector3.Lerp(
                transform.position, 
                targetPosition, 
                Time.deltaTime * followSpeed
            );
        }
        
        // Update target rotation based on settings
        if (lookAtTarget)
        {
            // Calculate direction to target for lookat
            Vector3 direction = target.position - transform.position;
            
            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction);
                
                // Apply custom X and Z rotations
                Vector3 eulerAngles = targetRotation.eulerAngles;
                eulerAngles.x = xRotation;
                eulerAngles.z = zRotation;
                targetRotation = Quaternion.Euler(eulerAngles);
            }
        }
        else
        {
            // Smoothly interpolate to target X and Z rotations
            currentXRotation = Mathf.SmoothDamp(currentXRotation, xRotation, ref rotationVelocity, rotationSmoothTime);
            currentZRotation = Mathf.SmoothDamp(currentZRotation, zRotation, ref rotationVelocity, rotationSmoothTime);
            
            targetRotation = Quaternion.Euler(currentXRotation, 0, currentZRotation);
        }
        
        // Apply rotation damping
        if (useSmoothDamping)
        {
            // Smooth rotation with time-based interpolation
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                Time.deltaTime * (1f / rotationSmoothTime)
            );
        }
        else
        {
            // Linear interpolation for rotation
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                Time.deltaTime * rotationSpeed
            );
        }
    }
    
    // Public methods to control camera
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    public void SetFollowSpeed(float newSpeed)
    {
        followSpeed = newSpeed;
    }
    
    public void SetXRotation(float newXRotation)
    {
        xRotation = Mathf.Clamp(newXRotation, 0f, 90f);
    }
    
    public void SetZRotation(float newZRotation)
    {
        zRotation = Mathf.Clamp(newZRotation, -45f, 45f);
    }
    
    public void SetLookAtTarget(bool shouldLookAt)
    {
        lookAtTarget = shouldLookAt;
    }
    
    public void SetBoundaries(Vector2 min, Vector2 max)
    {
        boundaryMin = min;
        boundaryMax = max;
        useBoundaries = true;
    }
    
    public void DisableBoundaries()
    {
        useBoundaries = false;
    }
    
    public void SetSmoothTime(float newPositionSmoothTime, float newRotationSmoothTime = 0.3f)
    {
        positionSmoothTime = Mathf.Max(0.01f, newPositionSmoothTime);
        rotationSmoothTime = Mathf.Max(0.01f, newRotationSmoothTime);
    }
    
    // Camera shake effect with rotation
    public void ShakeCamera(float duration, float magnitude, float rotationMagnitude = 1f)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude, rotationMagnitude));
    }
    
    private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude, float rotationMagnitude)
    {
        Vector3 originalPosition = transform.localPosition;
        Quaternion originalRotation = transform.localRotation;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Position shake
            float x = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;
            
            // Rotation shake
            float rotX = Random.Range(-1f, 1f) * rotationMagnitude;
            float rotZ = Random.Range(-1f, 1f) * rotationMagnitude;
            
            transform.localPosition = new Vector3(
                originalPosition.x + x, 
                originalPosition.y, 
                originalPosition.z + z
            );
            
            // Apply rotation shake (only to X and Z axes)
            Quaternion shakeRotation = Quaternion.Euler(
                originalRotation.eulerAngles.x + rotX,
                originalRotation.eulerAngles.y,
                originalRotation.eulerAngles.z + rotZ
            );
            
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, 
                shakeRotation, 
                Time.deltaTime * 20f
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Return to original position and rotation smoothly
        float returnTime = 0.5f;
        float timer = 0f;
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;
        
        while (timer < returnTime)
        {
            timer += Time.deltaTime;
            float t = timer / returnTime;
            
            transform.localPosition = Vector3.Lerp(startPosition, originalPosition, t);
            transform.localRotation = Quaternion.Slerp(startRotation, originalRotation, t);
            yield return null;
        }
        
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Draw camera target line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        
        // Draw camera forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
        
        // Draw boundaries if enabled
        if (useBoundaries)
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3(
                (boundaryMin.x + boundaryMax.x) * 0.5f,
                target.position.y,
                (boundaryMin.y + boundaryMax.y) * 0.5f
            );
            Vector3 size = new Vector3(
                boundaryMax.x - boundaryMin.x,
                0.1f,
                boundaryMax.y - boundaryMin.y
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}