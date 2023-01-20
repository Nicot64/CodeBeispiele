using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using PlayFab; //Für die Spieler-Accounts und die Leaderboards habe ich den Account-Dienst "PlayFab" benutzt.
using TMPro;
using System;

public class Leaderboard : MonoBehaviour
{
    public string LevelName;
    public GameObject loadImg, contentScore, contentTime;
    public ScrollRect scoreRect, timeRect;
    public bool loaded;
    int count = 0;

    //Wenn der Spieler ein Leaderboard für ein bestimmtes Level aufruft, werden alle aktuellen Daten geladen.
    private void OnEnable()
    {
        if (!loaded)
        {
            Load();
            loaded = true;
        }
    }

    public void Load()
    {
        loadImg.SetActive(true);

        //Das zuletzt geladene Leaderboard wird zurückgesetzt.
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("EntryText"))
        {
            Destroy(g);
        }

        scoreRect.content.sizeDelta = new Vector2(0, 80);
        timeRect.content.sizeDelta = new Vector2(0, 80);

        var scoreRequest = new GetLeaderboardRequest { StatisticName = LevelName + "-Score" };
        PlayFabClientAPI.GetLeaderboard(scoreRequest, GotScore, Failed);

        var timeRequest = new GetLeaderboardRequest { StatisticName = LevelName + "-Time" };
        PlayFabClientAPI.GetLeaderboard(timeRequest, GotTime, Failed);
    }

    private void GotTime(GetLeaderboardResult obj)
    {
        int offset = 0;
        for (int i = obj.Leaderboard.Count - 1; i > -1; i--)
        {
            scoreRect.content.sizeDelta += new Vector2(0, 60);
            GameObject c = Instantiate(contentTime, timeRect.content);
            c.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 60 * offset);
            float t = obj.Leaderboard[i].StatValue / 1000000f;
            if (!string.IsNullOrWhiteSpace(obj.Leaderboard[i].DisplayName))
            {
                c.GetComponent<TextMeshProUGUI>().text = "Platz " + Mathf.Abs(i - obj.Leaderboard.Count).ToString() + " - " + obj.Leaderboard[i].DisplayName + " - " + t.ToString() + "s";
            }
            else
            {
                c.GetComponent<TextMeshProUGUI>().text = "Platz " + Mathf.Abs(i - obj.Leaderboard.Count).ToString() + " - Kein Name - " + t.ToString() + "s";
            }
            c.SetActive(true);
            offset++;
        }
        count++;
        CheckCount();
    }

    private void Failed(PlayFabError obj)
    {
        Debug.Log("Failed to get leaderboard. " + obj.ErrorMessage);
        count++;
        CheckCount();
    }

    private void GotScore(GetLeaderboardResult obj)
    {
        for (int i = 0; i < obj.Leaderboard.Count; i++)
        {
            scoreRect.content.sizeDelta += new Vector2(0, 60);
            GameObject c = Instantiate(contentScore, scoreRect.content);
            c.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 60 * i);
            if (!string.IsNullOrWhiteSpace(obj.Leaderboard[i].DisplayName))
            {
                c.GetComponent<TextMeshProUGUI>().text = "Platz " + (i + 1).ToString() + " - " + obj.Leaderboard[i].DisplayName + " - " + obj.Leaderboard[i].StatValue;
            }
            else
            {
                c.GetComponent<TextMeshProUGUI>().text = "Platz " + (i + 1).ToString() + " - Kein Name - " + obj.Leaderboard[i].StatValue;
            }
            c.SetActive(true);
        }
        count++;
        CheckCount();
    }

    void CheckCount()
    {
        //Wenn beide Punktelisten geladen wurden, wird das Leaderboard angezeigt.
        if (count == 2)
        {
            loadImg.SetActive(false);
            count = 0;
        }
    }
}
