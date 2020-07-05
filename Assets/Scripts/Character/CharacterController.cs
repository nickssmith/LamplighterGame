using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{

    //Control settings
    public bool isPlayer = false;

    //Objects and vars not loaded from save file
    private Transform CharacterTransform;
    private Camera cam;
    private Rigidbody rb;
    private Physics physics;

    [SerializeField] GameObject ManaUI;
    [SerializeField] GameObject HealthUI;
    [SerializeField] GameObject StaminaUI;
    [SerializeField] GameObject TargetUI;//healthbar for target
    [SerializeField] GameObject TargetName;//name of target


    //Character save manager
    public string CharacterSaveFileFolder = "Assets/CharacterJson";
    public string CharacterSaveFile = "Player1.json";
    private CharacterDataManager CDM = new CharacterDataManager();

    private CharacterData Character = new CharacterData();

    // variables that are used for interacting with world but dont matter for save
    private string ItemStatus = "";//action items status, for swapping and dropping
    private bool HasItemInHand = false;
    private bool IsMoving = false;
    private bool IsGrounded = true;

    // Targeting and interacting
    private GameObject FollowTarget;
    private GameObject CombatTarget = null;
    private CharacterData TargetCharacter = null;//save info on target character

    public GameObject TargetBeacon; // prefab of the target beacon

    private bool hasTarget = false; //toggle if a target exists
    private int rand; //random number used to isolate targets
    private GameObject TargetBeaconObject = null; // the actual instance of the target beacon


    // animation parts and locations
    public GameObject AnimationTarget;// TODO point this at self / this.
    private Animator CharacterAnimator;

    public GameObject Hand;

    //TODO implement this with items
    public GameObject Back;
    public GameObject Belt;



    private string CurrentAnimationState = "Idle01";
    private string LastAnimationState = "Idle01";


    //When character comes online, set vars needed for init
    private void Awake()
    {

        rb = gameObject.GetComponent<Rigidbody>();
        CharacterTransform = gameObject.GetComponent<Transform>();



        CDM.Init(CharacterSaveFileFolder, CharacterSaveFile);
        Character = CDM.Load();
        Debug.Log("Loaded data for " + Character.Name + " from " + CharacterSaveFile);

        CharacterAnimator = AnimationTarget.GetComponent<Animator>();


        if (isPlayer)
        {
            cam = Camera.main;
            this.tag = "player";

            //TODO make it so all character can be animated later
            //TODO replace target with this
            CharacterAnimator = AnimationTarget.GetComponent<Animator>();

        }
    }

    private void FixedUpdate()
    {
        if (isPlayer)
        {
            PlayerMove();
            DoUI();

            //Debug save and load functions
            if (Input.GetKeyDown("i"))
            {
                Character = CDM.Load();
            }
            if (Input.GetKeyDown("o"))
            {
                CDM.Save(Character);
            }
            if (Input.GetKeyDown("e"))
            {
                Interact();
            }
            if (Input.GetKeyDown("tab"))
            {
                Target();
            }
            if (Input.GetKey("q"))
            {
                ItemStatus = "Dropping";//applies to habd item
            }
            else if (Input.GetKeyDown("f"))
            {
                ItemStatus = "SwapHandBack";
            }
            else if (Input.GetKeyDown("g"))
            {
                ItemStatus = "SwapHandBelt";
            }
            else
            {
                ItemStatus = "";
            }
            CheckIfItemInHand();


            /*
            if (Input.GetKeyDown("g")) {

            }*/

        }
        else
        {
            NPCMove();
        }
    }

    // things do to at frame time
    private void Update()
    {
        //TODO rm this check so all charactes are animated
        //if (isPlayer)
        //{

        DoAnimationState();
        if (LastAnimationState != CurrentAnimationState)
        {
            if (LastAnimationState == "MidAir" && IsGrounded)
            {
                CurrentAnimationState = "Landing";
            }
            CharacterAnimator.Play(CurrentAnimationState, 0, 0);
            LastAnimationState = CurrentAnimationState;
        }
        //}
    }


    private void DoAnimationState()
    {
        /*
        if ((IsGrounded == true) && (Input.GetButton("Jump")))
        {
            CurrentAnimationState = "Jump";
            Debug.Log("Juumping");
        }
        else if (!IsGrounded && LastAnimationState == "MidAir")
        {
            CurrentAnimationState = "MidAir";
            Debug.Log("airing");
        }
        else */
        if (!IsGrounded)
        {
            CurrentAnimationState = "MidAir";
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Character.CurrentStamina > 10)
        {
            if (isPlayer)
            {
                //TODO check state of character
                if (Input.GetKey("w"))
                {
                    CurrentAnimationState = "Running";

                }
                else if (Input.GetKey("s"))
                {
                    CurrentAnimationState = "RunningBackwards";

                }
            }
            else
            {
                //TODO
            }

        }
        else
        {
            if (IsMoving)
            {
                CurrentAnimationState = "Walking";
            }
            else
            {
                CurrentAnimationState = "Idle01";
            }


        }
    }

    private void DoUI()
    {
        DoHealthUI();
        DoStaminaUI();
        DoManaUI();
        DoTargetHealtBarUI();
    }

    private void DoHealthUI()
    {
        HealthUI.GetComponent<FillUI>().SetTo(Character.CurrentHealth / Character.MaxHealth);

    }
    private void DoStaminaUI()
    {
        StaminaUI.GetComponent<FillUI>().SetTo(Character.CurrentStamina / Character.MaxStamina);
    }

    private void DoManaUI()
    {
        ManaUI.GetComponent<FillUI>().SetTo(Character.CurrentMana / Character.MaxMana);

    }

    private void DoTargetHealtBarUI()
    {
        if (hasTarget)
        //check if freindly, if so show only name, else show health bar
        {
            TargetName.GetComponent<Text>().text = TargetCharacter.Name;
            if (!TargetCharacter.IsFollower)
            {
                //TargetUI.GetComponent<FillUI>().SetTo(TargetCharacter.CurrentHealth);
                //float targetsHealth = CombatTarget.GetComponent<CharacterController>().GetCurrentHealth();
                TargetUI.GetComponent<FillUI>().SetTo(TargetCharacter.CurrentHealth/ TargetCharacter.MaxHealth);
            }

        }
        else// if no target, hide UI
        {
            TargetUI.GetComponent<FillUI>().SetTo(0.0f);
            TargetName.GetComponent<Text>().text = "";
        }
    }

    // Movement and npc

    private void NPCMove()
    {
        //TODO if follower
        // todo if enemy
        // make sure if figting, also take control of is moving
        // TODO if enemy follower sees, target instead
        if (Character.IsFollowing)
        {
            FollowPlayer();
        }
        else
        {
            IsMoving = false;
        }

    }

    private void FollowPlayer()
    {

        float rotationSpeed = 6f; //speed of turning
        float range = 10f;
        float range2 = 10f;
        float stop = 1f; // this is range to player

        Transform TargetTransform = FollowTarget.GetComponent<Transform>();
        //rotate to look at the player
        var distance = Vector3.Distance(CharacterTransform.position, TargetTransform.position);
        if (distance <= range2 && distance >= range)
        {
            IsMoving = false;
            CharacterTransform.rotation = Quaternion.Slerp(CharacterTransform.rotation,
                Quaternion.LookRotation(TargetTransform.position - CharacterTransform.position), rotationSpeed * Time.deltaTime);
        }
        else if (distance <= range && distance > stop)
        {
            //move towards the player
            IsMoving = true;
            CharacterTransform.rotation = Quaternion.Slerp(CharacterTransform.rotation,
            Quaternion.LookRotation(TargetTransform.position - CharacterTransform.position), rotationSpeed * Time.deltaTime);
            CharacterTransform.position += CharacterTransform.forward * Character.CurrentSpeed * Time.deltaTime;
        }
        else if (distance <= stop)
        {
            IsMoving = false;
            CharacterTransform.rotation = Quaternion.Slerp(CharacterTransform.rotation,
                Quaternion.LookRotation(TargetTransform.position - CharacterTransform.position), rotationSpeed * Time.deltaTime);
        }
        else
        {
            IsMoving = false;
        }

    }

    private void RandomMove()
    {
        float maxForce = 50f;
        Vector3 position = new Vector3(Random.Range(-1f * maxForce, maxForce), Random.Range(-1f * maxForce, maxForce), Random.Range(-1f * maxForce, maxForce));
        //Debug.Log(position);
        rb.AddForce(position);

    }

    // Player movement

    private void PlayerMove()
    {

        // Getting the direction to move through player input
        float hMove = Input.GetAxis("Horizontal");
        float vMove = Input.GetAxis("Vertical");

        //if moving forward, slightly fasater
        if (Input.GetKey("w"))
        {
            Character.CurrentSpeed = Character.BaseMovementSpeed * 1.15f;
        }

        //sprinting
        if (Input.GetKey(KeyCode.LeftShift) && Character.CurrentStamina > 1)
        {
            Character.CurrentStamina = Character.CurrentStamina - Character.StaminaUseRate;
            Character.CurrentSpeed = Character.BaseMovementSpeed + (Character.StaminaBonusSpeed * (Character.CurrentStamina / Character.MaxStamina * 0.7f));
        }
        else
        {

            if (Character.CurrentStamina < Character.MaxStamina)
            {
                if (Character.CurrentStamina <= 1)
                {
                    Character.CurrentStamina = 2;
                }
                else
                {
                    Character.CurrentStamina = Character.CurrentStamina + Character.CurrentStamina * Character.StaminaRechargeRate;
                }
            }
            Character.CurrentSpeed = Character.BaseMovementSpeed;
        }

        //Actual movemment

        // Get directions relative to camera
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        // Project forward and right direction on the horizontal plane (not up and down), then
        // normalize to get magnitude of 1
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Set the direction for the player to move
        Vector3 dir = right * hMove + forward * vMove;

        IsMoving = dir != Vector3.zero;

        // Set the direction's magnitude to 1 so that it does not interfere with the movement speed
        dir.Normalize();

        // Move the player by the direction multiplied by speed and delta time 
        transform.position += dir * Character.CurrentSpeed * Time.deltaTime;

        // Set rotation to direction of movement if moving
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), 0.2f);
        }

        //Jumping
        float DisstanceToTheGround = GetComponent<Collider>().bounds.extents.y;

        //IsGrounded = Physics.Raycast(CharacterTransform.position, Vector3.up, DisstanceToTheGround + 0.1f);

        if ((IsGrounded == true) && (Input.GetButton("Jump"))) //if canjump boolean is true and if the player press the button jump , the player can jump.
        {
            CurrentAnimationState = "Jump";

            Vector3 up = new Vector3(0f, Character.JumpHeight, 0.0f); // script for jumping
                                                                      //rb.AddForce (up * upper);
            rb.AddForce(up * Character.JumpHeight);

        }

    }


    void OnCollisionExit(Collision hit)
    {
        if (hit.gameObject.tag == "Ground")
        {
            IsGrounded = false;
        }
    }

    void OnCollisionEnter(Collision hit)
    {
        if (hit.gameObject.tag == "Ground")
        {
            IsGrounded = true;
        }
    }

    // Targeting and interacting

    private void Interact()
    {

        RaycastHit hit;
        if (Physics.Raycast(CharacterTransform.position, CharacterTransform.forward, out hit, Character.Reach))
        {
            //Debug.Log (hit);
            Debug.Log("Interacted with" + hit.collider.gameObject);
            hit.collider.gameObject.GetComponent<CharacterController>().DoInteractAction(this.gameObject);
            //hit.collider.gameObject.target = this;

            //IInteractable interactable = hit.collider.GetComponent<IInteractable> ();

            //if (interactable != null) {
            //    interactable.ShowInteractability ();
            //    interactable.Interact ();
            //}
        }
        else
        {
            if (CombatTarget != null)
            {
                CombatTarget.GetComponent<CharacterController>().DoInteractAction(this.gameObject);
            }
        }
    }

    private void Target()
    {

        if (!hasTarget)// if doesnt have a target, find one
        {
            rand = Random.Range(1, 254);

            float radius = Character.TargetRange / 2.0f;
            Vector3 center = CharacterTransform.position + (CharacterTransform.forward * Character.TargetRange / 2.0f);
            Collider[] hitColliders = Physics.OverlapSphere(center, radius);
            int i = 0;
            Vector3 SummonPositon = center;

            while (i < hitColliders.Length)
            {
                if (hitColliders[i].gameObject.GetComponent<CharacterController>() != null)
                {
                    Transform TargetTransform = hitColliders[i].gameObject.GetComponent<Transform>();
                    int TargetRand = hitColliders[i].gameObject.GetComponent<CharacterController>().GetRand();
                    if (TargetRand != rand)
                    {
                        SummonPositon = TargetTransform.position + new Vector3(0.0f, 2.0f, 0.0f);
                        TargetBeaconObject = Instantiate(TargetBeacon, SummonPositon, Quaternion.identity);
                        hasTarget = true;
                        CombatTarget = hitColliders[i].gameObject;
                        TargetCharacter = hitColliders[i].gameObject.GetComponent<CharacterController>().GetCharacter();
                        //make the target beacon a child of its taret
                        TargetBeaconObject.gameObject.GetComponent<Transform>().parent = CombatTarget.GetComponent<Transform>();
                        break;
                    }
                }
                i++;
            }
        }
        else// if does have target, de-target
        {
            Destroy(TargetBeaconObject);
            CombatTarget = null;
            TargetCharacter = null;
            hasTarget = false;
        }

    }

    private void DoInteractAction(GameObject WhoInteracted)
    {
        Debug.Log("I was interacted with by " + WhoInteracted);
        Debug.Log(Character.IsFollower);

        // If a follower, then make then interact toggles follow
        if (Character.IsFollower)
        {
            Debug.Log("im a follwer who was interacted with");
            Character.IsFollowing = !Character.IsFollowing;
            FollowTarget = WhoInteracted;

            //if was told to stop following, then also stop moving
            if (Character.IsFollowing)
            {
                IsMoving = false;
                CurrentAnimationState = "Idle01";
            }
        }
    }

    private void DoTargetedAction()// character will make a beacon above their head
    {

        Debug.Log("I was targetd");

        //TODO reacte to being targeted etc

    }


    private void CheckIfItemInHand()//updates the hand var
    {
        Transform handTransform = Hand.GetComponent<Transform>();
        int i = 0;
        foreach (Transform child in handTransform)
        {
            //Debug.Log("is child of hand" + child);
            i+=1;
        }
        if(i>=1){
            HasItemInHand = true;
        }
        else{
            HasItemInHand = false;
        }
    }

    // Getters and setters for interactions

    public CharacterData GetCharacter()
    {
        return this.Character;
    }

    public bool GetIsPlayer()
    {
        return this.isPlayer;
    }

    public Transform GetCharacterTransform()
    {
        return this.CharacterTransform;
    }

    public Transform GetHandTransform()
    {
        return Hand.transform;
    }

    public Transform GetBackTransform()
    {
        return Back.transform;
    }

    public Transform GetBeltTransform()
    {
        return Belt.transform;
    }

    public int GetRand()
    {
        return rand;
    }

    public string GetItemStatus()
    {
        return this.ItemStatus;
    }


    public bool GetHasItemInHand()
    {
        return this.HasItemInHand;
    }


    // TODO add value to health etc
    // add negative value to reduce
    public void AddValueToStamina(float value)
    {
        Character.CurrentStamina += value;
    }

    public void AddValueToHealth(float value)
    {
        Debug.Log(Character.Name + "    "+Character.CurrentHealth+"    "+ (Character.CurrentHealth + value) );
        Character.CurrentHealth += value;
    }

    public float GetCurrentHealth(){
        return this.Character.CurrentHealth;
    }

}