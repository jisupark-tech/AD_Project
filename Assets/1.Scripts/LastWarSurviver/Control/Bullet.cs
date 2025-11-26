using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 10f;
    public int damage = 1;
    public float destroyY = 10f;

    void OnEnable()
    {
        GetComponent<Rigidbody>().linearVelocity = Vector3.forward * speed;
    }

    void Update()
    {
        if (transform.position.z > destroyY)
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            gameObject.SetActive(false);
        }
        if(other.CompareTag("Item"))
        {
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (GetComponent<Rigidbody>() != null)
            GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }
}