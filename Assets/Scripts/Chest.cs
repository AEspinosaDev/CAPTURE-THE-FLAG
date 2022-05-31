using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    SpriteRenderer m_ChestRender;
    void Start()
    {
        m_ChestRender = GetComponent<SpriteRenderer>();
    }
    private void OnMouseDown()
    {
        print("hey");
        
    }
    private void OnMouseEnter()
    {
        m_ChestRender.color = new Color(1, 0.25f, 0);
    }
    private void OnMouseExit()
    {
        m_ChestRender.color = new Color(1, 1, 1);
    }
}
