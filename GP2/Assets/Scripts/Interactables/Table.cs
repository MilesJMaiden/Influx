// Table.cs
using UnityEngine;

public class Table : Interactable
{
    protected override void OnClicked()
    {
        // your Table-specific click logic here
        Debug.Log("Table clicked!");
    }
}
