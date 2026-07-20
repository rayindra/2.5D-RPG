using UnityEngine;
using System.Collections.Generic;

public class JoinableCharacterScript : MonoBehaviour
{
    public PartyMemberInfo MemberToJoin;
    [SerializeField] private GameObject InteractPrompt;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CheckIfJoined();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowInteractPrompt(bool showPrompt)
    {
        if(showPrompt == true)
        {
            InteractPrompt.SetActive(true);
        }
        else
        {
            InteractPrompt.SetActive(false);
        }
    }

    public void CheckIfJoined()
    {
        List<PartyMember> currParty = GameObject.FindFirstObjectByType<PartyManager>().GetCurrentParty();

        for (int i = 0; i < currParty.Count; i++)
        {
            if(currParty[i].MemberName == MemberToJoin.MemberName)
            {
                Destroy(this.gameObject);
            }
        }
    }
}
