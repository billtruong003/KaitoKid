using Teabag.Authentication;
using Oculus.Platform;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Teabag.UI;
using Teabag.Core;

public class CalendarPresent : MonoBehaviour
{
    public static PresentYear year;
    PresentDay dayInfo
    {
        get
        {
            if (year == null)
                return null;


            if (year.Presents.Length < day)
            {
                Debug.LogError("Present day does not exist");
                return null;
            }

            // We subtract 1 since arrays start at 1
            return year.Presents[day - 1];
        }
        set
        {
            if (year == null)
                return;


            if (year.Presents.Length < day)
            {
                Debug.LogError("Present day does not exist");
                return;
            }

            year.Presents[day - 1] = value;
        }
    }

    public int day;
    public State state = State.None;
    public TMP_Text rewardText;

    [Header("Button")]
    public GorillaButton button;
    public TMP_Text text;

    [Header("Effects")]
    public ParticleSystem particles;
    public ParticleSystem moneyParticles;
    public AdvancedAudioClip openClip;

    [Header("Open")]
    public TransformLerp lerp;
    public TransformLerp rewardLerp;

    private void Awake()
    {
        if (year != null)
            Load();
        else
            text.text = "ERR";
    }

    public static async UniTask<PresentYear> GetChristmasCalendarAsync()
    {
        var response = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(
            new ExecuteCloudScriptRequest()
            {
                FunctionName = "GetChristmasCalendar"
            });
        if (response.IsError)
        {
            Debug.Log("Failed to get Christmas calendar");
            return null;
        }

        Debug.Log("Got Christmas calendar: " + response.Result.FunctionResult);
        year = JsonUtility.FromJson<PresentYear>(response.Result.FunctionResult.ToString());
        return year;
    }

    public void Load()
    {
        DateTime date = SyncedTime.now;
        bool isToday = date.Day == day;
        if (isToday)
            state = State.Openable;

        if (dayInfo.Opened)
            state = State.Opened;

        text.text = day.ToString();
        rewardText.text = "";

        lastDay = date.Day;
    }

    int lastDay = 0;

    private void Update()
    {
        if (SyncedTime.now.Day != lastDay && year != null)
            Load();

        button.interactable = state == State.Openable;

        if (state == State.Opened)
            lerp.t += Time.deltaTime;

        if (rewardText.text != "")
            rewardLerp.t += Time.deltaTime * 0.75f;
    }

    public async UniTaskVoid OnPress()
    {
        if (state != State.Openable)
            return;

        state = State.Opening;

        Debug.Log("Opening present for day " + day);

        var response = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(new ExecuteCloudScriptRequest()
        {
            FunctionName = "OpenChristmasPresent",
            FunctionParameter = new
            {
                Day = day
            }
        });

        if (response.IsError)
        {
            Debug.LogError("Error while opening Christmas present: " + day);
            text.text = "ERR";
            state = State.None;
            return;
        }

        Debug.Log("Open Christmas present response: " + response.Result.FunctionResult.ToString());

        bool result = false;
        bool.TryParse(response.Result.FunctionResult.ToString(), out result);
        if (!result)
        {
            Debug.LogError("Functionally failed to open Christmas present: " + day);
            text.text = "ERR";
            state = State.None;
            return;
        }

        dayInfo.Opened = true;

        Debug.Log("Reward: " + dayInfo.Reward);
        AuthenticationUtils.currency += dayInfo.Reward;

        Debug.Log("Opened");
        state = State.Opened;

        rewardText.text = dayInfo.Reward.ToString();

        particles.Play();
        moneyParticles.Play();
        //AudioService.Play(openClip, transform.position);

        await UniTask.Delay(5000);

        rewardText.text = "";
    }

    public enum State
    {
        None,
        Openable,
        Opening,
        Opened
    }

    [Serializable]
    public class PresentYear
    {
        public int Year;
        public PresentDay[] Presents = new PresentDay[0];
    }

    [Serializable]
    public class PresentDay
    {
        public int Day;
        public int Reward;
        public bool Opened;
    }
}
