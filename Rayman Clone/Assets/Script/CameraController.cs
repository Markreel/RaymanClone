using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] CinemachineTargetGroup targetGroup;
    [SerializeField] CinemachineFreeLook freeLookCamera;
    [SerializeField] GameObject rayman;

    public void FocusOnTarget(Transform _target)
    {
        targetGroup.m_Targets[0].target = _target;
        freeLookCamera.m_LookAt = targetGroup.transform;
    }

    public void FreeLook()
    {
        freeLookCamera.m_LookAt = rayman.transform;
    }
}
