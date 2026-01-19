using UnityEngine;

public class OnInteract_PlayAudio : MonoBehaviour
{
    public AudioSource target;

    public void Play()
    {
        if (target != null)
        {
            target.Play();
        }
    }
}

public class OnInteract_AnimatorTrigger : MonoBehaviour
{
    public Animator animator;
    public string triggerName = "OnInteract";

    public void Fire()
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
}

public class OnInteract_SetActive : MonoBehaviour
{
    public GameObject[] targets;
    public bool activeValue = true;

    public void Apply()
    {
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t != null) t.SetActive(activeValue);
        }
    }
}

public class OnInteract_ToggleActive : MonoBehaviour
{
    public GameObject[] targets;

    public void Apply()
    {
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t != null) t.SetActive(!t.activeSelf);
        }
    }
}

public class OnInteract_DebugLog : MonoBehaviour
{
    public string message = "OnInteract_DebugLog fired.";

    public void Log()
    {
        Debug.Log(message, this);
    }
}

public class OnInteract_QuitApplication : MonoBehaviour
{
    public void Quit()
    {
        Application.Quit();
    }
}

public class OnInteract_ReloadScene : MonoBehaviour
{
    public void Reload()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
    }
}

public class OnInteract_InvokeEvent : MonoBehaviour
{
    public UnityEngine.Events.UnityEvent onInteractEvent;

    public void Invoke()
    {
        if (onInteractEvent != null)
        {
            onInteractEvent.Invoke();
        }
    }
}

public class OnInteract_EnableComponent : MonoBehaviour
{
    public Behaviour[] componentsToEnable;

    public void Enable()
    {
        if (componentsToEnable == null) return;
        foreach (var comp in componentsToEnable)
        {
            if (comp != null) comp.enabled = true;
        }
    }
}

public class OnInteract_DisableComponent : MonoBehaviour
{
    public Behaviour[] componentsToDisable;

    public void Disable()
    {
        if (componentsToDisable == null) return;
        foreach (var comp in componentsToDisable)
        {
            if (comp != null) comp.enabled = false;
        }
    }
}

