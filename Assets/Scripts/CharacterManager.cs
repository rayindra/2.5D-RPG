using UnityEngine;

public class CharacterManager : MonoBehaviour
{

    private bool infrontOfPartyMember;
    private GameObject joinableMember;
    private PlayerControls playerControls;

    private const string NPC_JOINABLE_TAG = "NPCJoinable";
  

    private void Awake()
    {
        playerControls = new PlayerControls();
    }
    void Start()
    {
        playerControls.Player.Interact.performed += _ => Interact();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void Interact()
    {
        if(infrontOfPartyMember == true && joinableMember != null)
        {
           MemberJoined(joinableMember.GetComponent<JoinableCharacterScript>().MemberToJoin);
           infrontOfPartyMember = false;
           joinableMember = null;
        }
    }
    private void MemberJoined(PartyMemberInfo partyMember)
    {
        GameObject.FindFirstObjectByType<PartyManager>().AddMemberToPartyByName(partyMember.MemberName);
        joinableMember.GetComponent<JoinableCharacterScript>().CheckIfJoined();
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag(NPC_JOINABLE_TAG))
        {
            infrontOfPartyMember = true;
            joinableMember = other.gameObject;
            joinableMember.GetComponent<JoinableCharacterScript>().ShowInteractPrompt(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag(NPC_JOINABLE_TAG))
        {
            infrontOfPartyMember = false;
            joinableMember.GetComponent<JoinableCharacterScript>().ShowInteractPrompt(false);
            joinableMember = null;
        }
    }
}
