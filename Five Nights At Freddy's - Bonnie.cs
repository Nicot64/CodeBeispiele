using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bonnie : MonoBehaviour
{
    public int AI_Level;
    public PowerManager pM;
    public TimeManager tM;
    public CameraManager cM;
    public List<Room> availableRooms;
    public Room currentRoom;
    public Office office;
    public DoorControl door;
    public List<AudioSource> steps;


    IEnumerator Start()
    {
        while (pM.hasPower && tM.isGoing)
        {
            //Alle ca. fünf Sekunden während das Level läuft hat die KI eine bestimmte Chance, sich abhängig von ihrem Level (welches mit zunehmender Zeit gesteigert wird) zum Spieler zu bewegen.
            yield return new WaitForSeconds(4.97f);
            int n = Random.Range(1, 21);
            if (AI_Level >= n)
            {
                ChangeRoom();
            }
        }
    }

    void ChangeRoom()
    {
        if (currentRoom == null)
        {
            //IN FRONT OF DOOR          
            if (door.closed.activeSelf)
            {
                currentRoom = availableRooms[0];
                office.bonnie = false;
                office.Change();
                door.canPlayStinger = false;
                steps[Random.Range(0, steps.Count)].Play();
                ChangeRoom();
            }
            else
            {
                //Hier hat der Spieler nicht schnell genug reagiert und verliert das Spiel beim nächsten Verlassen der Kamera.
                if (office.gotKilledBy == "")
                {
                    office.gotKilledBy = "Bonnie";
                    cM.SnuckIntoOffice();
                    steps[Random.Range(0, steps.Count)].Play();
                    door.ForceOff();
                }
            }
        }
        else
        {
            //NORMAL BEHAVIOUR
            //Hier bewegt sich die KI zufällig
            if (currentRoom.gameObject.name == "-WestHallB" || currentRoom.gameObject.name == "-SupplyCloset")
            {
                int i = Random.Range(0, 10);
                if (i < 7)
                {
                    currentRoom.whoIsHere = currentRoom.whoIsHere.Replace("Bonnie", "");
                    currentRoom.SetImage();
                    office.bonnie = true;
                    office.Change();
                    door.canPlayStinger = true;
                    cM.Moving();
                    steps[Random.Range(0, steps.Count)].Play();
                    currentRoom = null;
                    return;
                }
            }

            Room r = availableRooms[Random.Range(0, availableRooms.Count)];
            if (r.whoIsHere != "")
            {
                ChangeRoom();
            }
            else
            {
                currentRoom.whoIsHere = currentRoom.whoIsHere.Replace("Bonnie", "");
                currentRoom.SetImage();
                r.whoIsHere = "Bonnie";
                r.SetImage();
                cM.Moving();
                steps[Random.Range(0, steps.Count)].Play();
                currentRoom = r;
            }
        }
    }
}
