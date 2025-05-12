using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.InputSystem.HID;
using TMPro;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    //hacia donde apunte el character
    Vector3 desiredDirection;
    public float speed = 1.0f;
    float characetSpeed = 0f;

    //salud del jugador
    //public int health = 100;

    [Header("Camera")]
    public Vector3 CameraOffset = new Vector3(0, 4, -3);
    public Vector3 CameraViewOffset = new Vector3(0, 1.5f, 0);
    Camera cam;


    [Header("Weapon")]
    public GameObject proyectilePrefab;
    public Transform weaponSocket;
    public float weaponCadence = 0.8f;
    float lastShootTimer = 0;

    [Header("Accesorios")]
    public Transform hatSocket;
    public List<GameObject> prefabhats = new List<GameObject>();
    bool hatSpawned = false;



    [Header("SFX")]
    public AudioClip DamageSound;
    public AudioClip DeathSound;
    AudioSource audioSource;


    public int ID = 0; 
    NetworkVariable<int> NameID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<int> SombreroID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    [Header("Animator")]
    Animator animator;
    NetworkAnimator networkAnimator;

    private TMP_Text playerName;

    //networkvariable para replicar la vida
    NetworkVariable<int> health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private UiManager hud;
    private GameManager gameManager;
    public override void OnNetworkSpawn()
    {
        Debug.Log("Hola mundo soy un " + (IsClient? "cliente" : "servidor"));
        Debug.Log("IsClient = " + IsClient + ", IsServer = " + IsServer + ", IsHost = " + IsHost);
        Debug.Log(name + " Is owner " + IsOwner);
        SombreroID.OnValueChanged += SpawnSombrero;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hud = GameObject.Find("GameManager").GetComponent<UiManager>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();

        transform.position = gameManager.GetSpawnPoint();

        //asignar la camara
        if (IsOwner)
        {
            cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            cam.transform.position = transform.position + CameraOffset;
            cam.transform.LookAt(transform.position + CameraViewOffset);
            SetNameIDRpc(hud.selectedNameIndex);
            SetSombreroIDRpc(hud.SelectedSombrero);
        }

        //ID = hud.selectedNameIndex;

        if (!IsOwner)
        {
            SpawnSombrero(0, SombreroID.Value);
        }

        CreatePlayerNameHUD();

    }

    void SpawnSombrero(int old, int newval)
    {
        if (hatSpawned)
        {
            if(newval != old)
            {
                Destroy(hatSocket.GetChild(0).gameObject);
                hatSpawned = false;
            }
        }
        GameObject hat = Instantiate(prefabhats[SombreroID.Value], hatSocket.position, hatSocket.rotation, hatSocket);
        hatSpawned = true;
    }

    void CreatePlayerNameHUD()
    {
        if (IsClient)
        {
          playerName =  Instantiate(hud.playerNameTemplate, hud.PanelHUD).GetComponent<TMP_Text>();
          playerName.gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 currentPosition = transform.position;
        if (IsOwner)
        {
            desiredDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            desiredDirection.Normalize();

           

            if (IsAlive())
            {
                float mag = desiredDirection.magnitude;

                if (mag > 0)
                {
                    //transform.forward = desiredDirection;
                    //Interpolar entre la rotación actual y la desesda

                    Quaternion q = Quaternion.LookRotation(desiredDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * 10);
                    transform.Translate(0, 0, speed * Time.deltaTime);
                    
                }
               

                //Prueba del sistema, de vida
                if (Input.GetKeyDown(KeyCode.T))
                {
                    TakeDamage(5);
                }

                if (Input.GetButtonDown("Fire1"))
                {
                    FireWeaponRPC();
                }

            }

            //actualizar la camara
            cam.transform.position = transform.position + CameraOffset;
            cam.transform.LookAt(transform.position + CameraViewOffset);

            //actualizar Hud
            hud.labelHealth.text = health.Value.ToString();

        }
        characetSpeed = (transform.position - currentPosition).magnitude / Time.deltaTime;
        Debug.Log("Del jugador " +  name + characetSpeed);
        Animate("movement", characetSpeed);

        if (IsServer)
        {
            lastShootTimer += Time.deltaTime;
            Debug.Log(lastShootTimer);
        }


        if (IsClient)
        {
            Camera mainCam = GameObject.Find("Main Camera").GetComponent<Camera>();
            playerName.transform.position = mainCam.WorldToScreenPoint(transform.position + new Vector3(0, 1.2f, 0));

            playerName.text = hud.namesList[NameID.Value];
        }
    }


    //RPC= 
    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int amount)
    {
        Debug.Log("RPC recibido TakeDamage");
        TakeDamage(amount);
    }

    [Rpc(SendTo.Server)]
    public void AnimateRPC(string parameter, float value)
    {
        Animate(parameter, value);
    }

    public void Animate(string parameter, float value)
    {
        if (!IsServer) 
        { 
          AnimateRPC(parameter, value);
        }
        else
        {
            networkAnimator.Animator.SetFloat(parameter, value);
        }

    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive()) return;


        if (!IsServer) 
        {
          TakeDamageRpc(amount);
        }
        else
        {
            health.Value -= amount;

            if (health.Value <= 0)
            {
                health.Value = 0;

                if (!IsAlive())
                {
                    OnDeath();
                }
            }
            else
            {
                audioSource.clip = DamageSound;
                audioSource.Play();
            }
        }     
    }

    public void OnDeath()
    {
        //efectos
        Debug.Log(name + " me muero");
        audioSource.clip = DeathSound;
        audioSource.Play();
        animator.SetBool("dead", true);
        animator.SetFloat("movement", 0f);
        
        
    }

    public bool IsAlive()
    {
        return health.Value > 0;
    }

    [Rpc(SendTo.Server)]
    public void FireWeaponRPC()
    {

        if (lastShootTimer < weaponCadence) return;


        if (proyectilePrefab != null) 
        {
            Projectile proj = Instantiate(proyectilePrefab, weaponSocket.position,weaponSocket.rotation).GetComponent<Projectile>();
            proj.direction = transform.forward; //SAle en la direccion que apuntta eñl personaje 
            proj.instigator = this; // quien dispara el proyectile
            proj.GetComponent<NetworkObject>().Spawn();
            lastShootTimer = 0;
        }       
    }

    [Rpc(SendTo.Server)]
    public void SetNameIDRpc(int idx)
    {
        NameID.Value = idx;
    
    }


    [Rpc(SendTo.Server)]
    public void SetSombreroIDRpc(int idx)
    {
        SombreroID.Value = idx;
    }
}
