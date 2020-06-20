using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public enum ArrowAimMode { Middle, Left, Right, }

    [SerializeField] GameObject targetUI;
    [SerializeField] GameObject rayman;
    [Space]
    [SerializeField] Material targetUIArrowMaterial;
    [SerializeField] Texture middleArrowTexture;
    [SerializeField] Texture leftArrowTexture;
    [SerializeField] Texture rightArrowTexture;

    private bool isTargeting;

    private void Awake()
    {
        targetUI.SetActive(false);
    }

    private void Update()
    {
        if (isTargeting)
        {
            targetUI.transform.LookAt(Camera.main.transform);
            //targetUI.transform.GetChild(0).GetChild(0).transform.LookAt(rayman.transform);
        }
    }

    public void SetTargetUI(GameObject _target, ArrowAimMode _aimMode = ArrowAimMode.Middle)
    {
        isTargeting = true;

        targetUI.transform.parent = _target.transform;
        targetUI.transform.localPosition = Vector3.zero;

        targetUI.SetActive(true);

        switch (_aimMode)
        {
            default:
            case ArrowAimMode.Middle:
                targetUIArrowMaterial.mainTexture = middleArrowTexture;
                break;
            case ArrowAimMode.Left:
                targetUIArrowMaterial.mainTexture = leftArrowTexture;
                break;
            case ArrowAimMode.Right:
                targetUIArrowMaterial.mainTexture = rightArrowTexture;
                break;
        }
    }

    public void DisableTargetUI()
    {
        isTargeting = false;
        targetUI.transform.parent = null;
        targetUI.SetActive(false);
    }
}
