using UnityEngine;

public class SuperPowerCan : MonoBehaviour
{
    [SerializeField] public SuperPower SuperPower;
    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.position = startPos + new Vector3(0f,Mathf.Cos(Time.time * 2)/20,0);
    }
}
