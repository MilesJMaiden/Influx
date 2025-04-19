using UnityEngine;

/// <summary>
/// Base behaviour for anything you can hover/hover‐click in the world.
/// Must have a child named "HighlightCylinder" for the hover highlight.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    protected GameObject _highlight;

    protected virtual void Awake()
    {
        // find the child highlight indicator
        var t = transform.Find("HighlightCylinder");
        if (t != null)
        {
            _highlight = t.gameObject;
            _highlight.SetActive(false);
        }
    }

    private void OnMouseEnter()
    {
        if (_highlight != null)
            _highlight.SetActive(true);
        UICursorManager.Instance?.SetHoverCursor();
    }

    private void OnMouseExit()
    {
        if (_highlight != null)
            _highlight.SetActive(false);
        UICursorManager.Instance?.ResetCursorImage();
    }

    private void OnMouseDown()
    {
        OnClicked();
    }

    /// <summary>
    /// Override to react to clicks.
    /// </summary>
    protected virtual void OnClicked() { }
}
