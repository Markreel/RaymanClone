using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] CinemachineTargetGroup targetGroup;
    [SerializeField] CinemachineFreeLook defaultCamera;
    [SerializeField] CinemachineVirtualCamera focusCamera;
    [SerializeField] GameObject rayman;

    public void FocusOnTarget(Transform _target)
    {
        targetGroup.m_Targets[1].target = _target;
        Focus();
        focusCamera.m_LookAt = targetGroup.transform;
    }

    public void Focus()
    {
        defaultCamera.gameObject.SetActive(false);
        focusCamera.gameObject.SetActive(true);

        defaultCamera.transform.parent = focusCamera.transform;
        defaultCamera.transform.localPosition = Vector3.zero;
        defaultCamera.transform.localEulerAngles = Vector3.zero;
    }

    public void FreeLook()
    {
        defaultCamera.transform.parent = null;

        focusCamera.gameObject.SetActive(false);
        defaultCamera.gameObject.SetActive(true);
        defaultCamera.m_LookAt = rayman.transform;
    }
}
