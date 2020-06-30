using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    private Animator animator;
    private GameObject markedCanvas;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        markedCanvas = transform.GetChild(0).gameObject;
    }

    public void GetHit()
    {
        animator.SetTrigger("GetHit");
    }

    public void Mark()
    {
        markedCanvas.SetActive(true);
    }

    public void UnMark()
    {
        markedCanvas.SetActive(false);
    }
}
