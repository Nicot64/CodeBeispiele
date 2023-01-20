using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class huntingGhost : MonoBehaviour
{
    //Verschiedene Geisterarten haben andere Eigenschaften, deswegen gibt es hier verschiedene Variablen
    public float regularSpeed, fastSpeed, slowSpeed;
    public List<string> fastGhosts, slowGhosts;

    GameObject manager, activityMetre;
    ghostManager gM;
    HouseManager hM;
    public NavMeshAgent agent;
    public PhotonView phv; //Ich habe den Dienst "Photon" für die Online-Funktionen verwendet

    Transform player, chasingPlayer;
    Vector3 currDest;
    public bool started, canSeePlayer, chasing, cancelingSight, playerInSight;
    RaycastHit rh;
    public Transform headObj;
    public LayerMask lM;
    Coroutine csp;
    AudioSource heartBeat;

    //Hier erscheint der Geist an seinem Spawn-Ort und wartet fünf Sekunden, bevor er losläuft.
    IEnumerator Start()
    {
        activityMetre = GameObject.Find("ActivityMetre");
        activityMetre.GetComponent<ActivityMetre>().currentLevel += 10;
        hM = GameObject.Find("houseManager").GetComponent<HouseManager>();
        heartBeat = GameObject.Find("Heartbeat").GetComponent<AudioSource>();
        manager = GameObject.Find("MANAGER");
        gM = manager.GetComponent<ghostManager>();

        yield return new WaitForSeconds(5f);

        if (GameObject.FindWithTag("Player") != null)
            player = GameObject.FindWithTag("Player").transform;

        if (phv.IsMine) //Der Host-Client der Lobby kontrolliert den Geist.
        {
            int d = Random.Range(0, gM.walkingPositions.Count);
            agent.destination = gM.walkingPositions[d].position;
            currDest = gM.walkingPositions[d].position;
        }

        if (fastGhosts.Contains(gM.ghostType))
        {
            agent.speed = fastSpeed;
        }
        else if (slowGhosts.Contains(gM.ghostType))
        {
            agent.speed = slowSpeed;
        }
        else
        {
            agent.speed = regularSpeed;
        }
        started = true;

        for (int i = 0; i < 3; i++)
        {
            //Es kann auch zusätzliche Aufgaben für Extrapunkte geben. Eine der drei könnte sein, sich jagen zu lassen.
            if (manager.GetComponent<OptionalObjectives>().chosenObjectives[i] == "Lasst euch jagen")
                manager.GetPhotonView().RPC("GotObjective", RpcTarget.All, i);
        }
    }

    private void Update()
    {
        if (started)
        {
            if (PhotonNetwork.IsMasterClient) //Lauf-Logik des Geistes
            {
                float dist = Vector3.Distance(transform.position, currDest);
                if (dist < 0.5f && !chasing) //Herumwandern im Haus
                {
                    int d = Random.Range(0, gM.walkingPositions.Count);
                    agent.destination = gM.walkingPositions[d].position;
                    currDest = gM.walkingPositions[d].position;
                }

                if (chasing)
                {
                    agent.destination = chasingPlayer.position;
                }
            }

            if (player != null && hM.inside)
            {
                float playerDist = Vector3.Distance(transform.position, player.position);
                if (playerDist <= 1.2f && canSeePlayer)
                {
                    die();
                    started = false;
                }

                Vector3 dir = player.position - headObj.position;
                Ray r = new Ray(headObj.position, dir);
                if (Physics.Raycast(r, out rh, 7, lM))
                {
                    //Debug.Log(rh.collider.gameObject.name);
                    if (rh.collider.transform == player)
                    {
                        playerInSight = true;
                        if (!canSeePlayer)
                        {
                            heartBeat.Play();
                            phv.RPC("startChasing", RpcTarget.MasterClient, player.GetComponent<PhotonView>().OwnerActorNr);
                            canSeePlayer = true;
                        }
                    }
                    else
                    {
                        playerInSight = false;
                        if (canSeePlayer && !cancelingSight)
                        {
                            if (csp != null)
                                StopCoroutine(csp);

                            csp = StartCoroutine(cantSeePlayerRoutine());
                            cancelingSight = true;
                        }
                    }
                }
                else
                {
                    playerInSight = false;
                    if (canSeePlayer && !cancelingSight)
                    {
                        if (csp != null)
                            StopCoroutine(csp);

                        csp = StartCoroutine(cantSeePlayerRoutine());
                        cancelingSight = true;
                    }
                }
            }
        }
    }

    [PunRPC]
    public void startChasing(int aN)
    {
        chasing = true;
        if (player != null)
        {
            if (player.GetComponent<PhotonView>().OwnerActorNr == aN)
            {
                chasingPlayer = player;
                return;
            }
        }

        foreach (GameObject p in GameObject.FindGameObjectsWithTag("notPlayer"))
        {
            if (p.GetPhotonView().OwnerActorNr == aN)
            {
                chasingPlayer = p.transform;
            }
        }
    }

    [PunRPC]
    public void setDest(Vector3 dest)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            agent.destination = dest;
            currDest = dest;
        }
        canSeePlayer = false;
    }

    IEnumerator cantSeePlayerRoutine()
    {
        yield return new WaitForSeconds(1f);
        heartBeat.Stop();
        phv.RPC("setDest", RpcTarget.All, player.position);
        canSeePlayer = false;
        cancelingSight = false;
    }

    private void OnDestroy()
    {
        activityMetre.GetComponent<ActivityMetre>().currentLevel -= 10;
        heartBeat.Stop();
    }

    void die()
    {
        GameObject.Find("PauseMenu").SetActive(false);
        foreach (GameObject item in GameObject.FindGameObjectsWithTag("Item"))
        {
            if (item.GetComponent<Item>() != null)
            {
                if (item.GetComponent<Item>().inHand && item.GetComponent<Item>().isOwner)
                {
                    item.GetPhotonView().RPC("throwAway", RpcTarget.All);
                }
            }
        }
        player.GetComponent<useItems>().enabled = false;
        player.GetComponent<OnlinePlayerScript>().dieScreen.SetActive(true);
        manager.GetPhotonView().RPC("endHunt", RpcTarget.All);
    }
}
