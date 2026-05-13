using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    [SerializeField] private GameObject mobileInputCanvas;

    void Start()
    {
        // Check if we are on a mobile platform OR if we are in the editor simulating mobile
        bool isMobile = Application.isMobilePlatform;

#if UNITY_EDITOR
        // You can toggle this to test in editor
        // isMobile = true; 
#endif

        if (mobileInputCanvas != null)
        {
            mobileInputCanvas.SetActive(isMobile);
        }
    }
}
