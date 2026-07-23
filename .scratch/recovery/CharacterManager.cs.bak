using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private GameObject joinPopup;
    [SerializeField] private TextMeshProUGUI joinPopupText;

    private bool infrontOfPartyMember;
    private GameObject joinableMember;
    private PlayerControls playerControls;
    private List<GameObject> overworldCharacters = new List<GameObject>();

    private const string PARTY_JOINED_MESSAGE = " Joined the party!";
    private const string NPC_JOINABLE_TAG = "NPCJoinable";
  

    private void Awake()
    {
        playerControls = new PlayerControls();
    }
    void Start()
    {
        playerControls.Player.Interact.performed += _ => Interact();
        SpawnOverworldMembers();
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
        bool added = GameObject.FindFirstObjectByType<PartyManager>()
            .AddMemberToPartyByName(partyMember.MemberName);

        if (!added) return;

        joinableMember.GetComponent<JoinableCharacterScript>().CheckIfJoined();
        joinPopup.SetActive(true);
        joinPopupText.text = partyMember.MemberName + PARTY_JOINED_MESSAGE;
        SpawnOverworldMembers();
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

    private void SpawnOverworldMembers()
    {
        for (int i = 0; i < overworldCharacters.Count; i++)
        {
            Destroy(overworldCharacters[i]);
        }
        overworldCharacters.Clear();

        List<PartyMember> currentParty = GameObject.FindFirstObjectByType<PartyManager>().GetCurrentParty();

        for (int i = 0; i < currentParty.Count; i++)
        {
            if(i==0)
            {
                GameObject player = gameObject;
                GameObject playerVisual = Instantiate(currentParty[i].MemberOverworldVisualPrefab, player.transform.position, Quaternion.identity);

                playerVisual.transform.SetParent(player.transform);

                player.GetComponent<PlayerController>().SetOverworldVisuals(playerVisual.GetComponent<Animator>(), 
                playerVisual.GetComponent<SpriteRenderer>(), playerVisual.transform.localScale);
                playerVisual.GetComponent<MemberFollowAI>().enabled = false;
                overworldCharacters.Add(playerVisual);
            }
            else
            {
                Vector3 positionToSpawn = transform.position;
                positionToSpawn.x -=1;
                GameObject tempFollower = Instantiate(currentParty[i].MemberOverworldVisualPrefab, positionToSpawn, Quaternion.identity);

                tempFollower.GetComponent<MemberFollowAI>().SetFollowDistance(i);
                overworldCharacters.Add(tempFollower);
            }
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
