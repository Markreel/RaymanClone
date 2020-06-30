using UnityEngine;

public class SuperPowerCan : MonoBehaviour
{
    [SerializeField] public SuperPower SuperPower;
    [SerializeField] private GameObject beam;
    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.position = startPos + new Vector3(0f,Mathf.Cos(Time.time * 2)/20,0);
    }

    private void OnTriggerEnter(Collider other)
    {
        Rayman _rayman = other.GetComponent<Rayman>();
        if(_rayman != null && !beam.activeInHierarchy) { beam.SetActive(true); }
    }

    private void OnTriggerExit(Collider other)
    {
        Rayman _rayman = other.GetComponent<Rayman>();
        if (_rayman != null && beam.activeInHierarchy) { beam.SetActive(false); }
    }
}
