using Unity.VisualScripting;
using UnityEngine;

public class RewindableChild : MonoBehaviour
{
    public delegate void OnMouseEnterDelegate();
    public event OnMouseEnterDelegate onMouseEnter;
    
    public delegate void OnMouseExitDelegate();
    public event OnMouseExitDelegate onMouseExit;
    
    public delegate void OnMouseDownDelegate();
    public event OnMouseDownDelegate onMouseDown;
    
    private void OnMouseEnter() => onMouseEnter?.Invoke();
    private void OnMouseExit() => onMouseExit?.Invoke();
    private void OnMouseDown() => onMouseDown?.Invoke();
}
