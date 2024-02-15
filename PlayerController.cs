using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput;

    public bool invertLook;

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;

    public CharacterController charCon;

    private Camera cam;

    public float jumpForce = 12f, gravityMod = 2.5f;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;
    //public float timeBetweenShots = .1f;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleCounter;

    //maxHeat - max level the heat can go up to
    //heatPerShot - what each shot that we do, it add 1 heat
    //coolRate - How fast the heat meter will go back down to zero when it is not overheated
    //overheatCoolRate - How fast the heat meter will go back down to zero when it is overheated
    public float maxHeat = 10f, /*heatPerShot = 1f, */ coolRate = 4f, overheatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    public Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    // public int maxHealth = 100;
    // private int currentHealth;

    // public Animator anim;
    // public GameObject playerModel;
    // public Transform modelGunPoint, gunHolder;

    // public Material[] allSkins;

    // public float adsSpeed = 5f;
    // public Transform adsOutPoint, adsInPoint;

    // public AudioSource footstepSlow, footstepFast;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        UIController.instance.weaponTempSlider.maxValue = maxHeat;

        SwitchGun();

        
        // photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        // currentHealth = maxHealth;

        //Transform newTrans = SpawnManager.instance.GetSpawnPoint();
        //transform.position = newTrans.position;
        //transform.rotation = newTrans.rotation;

        // if(photonView.IsMine)
        // {
        //     playerModel.SetActive(false);

        //     UIController.instance.healthSlider.maxValue = maxHealth;
        //     UIController.instance.healthSlider.value = currentHealth;
        // } else
        // {
        //     gunHolder.parent = modelGunPoint;
        //     gunHolder.localPosition = Vector3.zero;
        //     gunHolder.localRotation = Quaternion.identity;
        // }

        // playerModel.GetComponent<Renderer>().material = allSkins[photonView.Owner.ActorNumber % allSkins.Length];
    }

    // Update is called once per frame
    void Update()
    {
        if(photonView.IsMine)
        {
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            verticalRotStore += mouseInput.y;
            verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

            if (invertLook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }

            moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;

                // if(!footstepFast.isPlaying && moveDir != Vector3.zero)
                // {
                //     footstepFast.Play();
                //     footstepSlow.Stop();
                // }
            }
            else
            {
                activeMoveSpeed = moveSpeed;

                // if (!footstepSlow.isPlaying && moveDir != Vector3.zero)
                // {
                //     footstepFast.Stop();
                //     footstepSlow.Play();
                // }
            }

            // if(moveDir == Vector3.zero || !isGrounded)
            // {
            //     footstepSlow.Stop();
            //     footstepFast.Stop();
            // }

            float yVel = movement.y;
            movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
            movement.y = yVel;

            if (charCon.isGrounded)
            {
                movement.y = 0f;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charCon.Move(movement * Time.deltaTime);

            
            // MuzzleFlash countdown
            if(allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;

                if(muzzleCounter <= 0)
                {
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }
                
            }


            if(!overHeated)
            {
                // If LMB is clicked
                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();
                }

                // If LMB is being held down
                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;

                    if (shotCounter <= 0)
                    {
                        Shoot();
                    }
                }

                heatCounter -= coolRate * Time.deltaTime;
            } else
            {
                heatCounter -= overheatCoolRate * Time.deltaTime;
                if (heatCounter <= 0)
                {
                    overHeated = false;
                    UIController.instance.overheatedMessage.gameObject.SetActive(false);
                }
            }
            
            if (heatCounter < 0)
            {
                heatCounter = 0;
            }

            UIController.instance.weaponTempSlider.value = heatCounter;


            

            // Weapon Switching
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;

                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;
                }
                SwitchGun();
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;

                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                SwitchGun();
            }



            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }

    private void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log("We hit " + hit.collider.gameObject.name);

            if(hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                // Getting the victim to trigger their DealDamage function
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName);
            } 
            else 
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 10f);
            }
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;


        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }

        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;

        // allGuns[selectedGun].shotSound.Stop();
        // allGuns[selectedGun].shotSound.Play();
    }

    // RPC - Remote Procedure Call
    [PunRPC]
    public void DealDamage(string damager)
    {
        TakeDamage(damager);
    }

    public void TakeDamage(string damager)
    {
        if(photonView.IsMine)
        {
            Debug.Log(photonView.Owner.NickName + " has been hit by " + damager);
            PlayerSpawner.instance.Die(damager);
        }
    }


    private void LateUpdate()
    {
        if(photonView.IsMine)
        {
            cam.transform.position = viewPoint.position;
            cam.transform.rotation = viewPoint.rotation;
        }
    }

    void SwitchGun()
    {
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);

        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

}