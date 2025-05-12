using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    public float damage = 55f;
    public PlayerController instigator; //quien disparo el proyectil
    public Vector3 direction;
    public GameObject impactPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        lifeTime -= Time.deltaTime;
        
        if(lifeTime < 0)
        {
            GetComponent<NetworkObject>().Despawn();
        }

        //servidor es quien tiene la autoridad sobre el proyectil
        if (IsServer)
        {
            transform.position += direction * speed * Time.deltaTime;

        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerController otherPlayer = other.GetComponent<PlayerController>();

        if (otherPlayer != null && otherPlayer != instigator) 
        {
            otherPlayer.TakeDamage((int)damage);
            OnImpactRpc();
            GetComponent<NetworkObject>().Despawn();
            
        }

    }

    [Rpc(SendTo.ClientsAndHost)]
    public void OnImpactRpc()
    {
        //spanera el efecto de impacto
        if (impactPrefab != null) 
        { 
          GameObject impact = Instantiate(impactPrefab, transform.position, Quaternion.identity);
          Destroy(impact,2f);
        }
    }
}
