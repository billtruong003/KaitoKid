using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;
using Teabag.Core;
using TMPro;

public class WeekDayCosmetics : MonoBehaviour
{
    [Header("Cosmetics")]
    public List<PurchaseStand> stands = new List<PurchaseStand>();
    public DailyCosmetics cosmetics = new DailyCosmetics();

    [Header("Visuals")]
    public MeshRenderer display;
    public TextMeshPro countdown;
    public DataViewer dataViewer;
    public List<Alarm> alarms;
    public List<Texture> textures = new List<Texture>();
    public List<Door> doors = new List<Door>();
    public AudioSource audioSource;


    //[Header("Debug")]
    //public int dayOfWeek;
    int lastDay = -1;

    private void Awake()
    {
#if !SKIP_PLATFORM_AUTH
        cosmetics = JsonUtility.FromJson<DailyCosmetics>(AuthenticationUtils.titleData["DailyCosmetics"]);
#endif
    }

    private void Update()
    {
        DateTime time = AuthenticationUtils.serverTime;
        DateTime nextDay = time.AddDays(1);
        nextDay = nextDay.AddHours(-nextDay.Hour);
        nextDay = nextDay.AddMinutes(-nextDay.Minute);
        nextDay = nextDay.AddSeconds(-nextDay.Second);
        nextDay = nextDay.AddMilliseconds(-nextDay.Millisecond);

        TimeSpan span = nextDay - time;
        string c = Date.DisplayCountdown(span);
        dataViewer.Show(new Dictionary<string, string>()
        {
            {
                "TIME",
                c
            }
        });

        countdown.text = c;
        int m = (int)span.TotalMinutes;
        if ((int)span.Seconds == 0)
            m -= 1;
        if (m < 5)
        {
            int s = (int)span.TotalSeconds;
            countdown.color = s % 2 == 0 ? Color.red : Color.white;
            foreach (Alarm alarm in alarms)
                alarm.Enable();
        }
        else
        {
            countdown.color = Color.white;
            foreach (Alarm alarm in alarms)
                alarm.Disable();
        }

        int dayOfWeek = (int)time.DayOfWeek;
        if (lastDay != dayOfWeek)
            LoadDay(dayOfWeek);
    }

    public async UniTaskVoid LoadDay(int day)
    {
        lastDay = day;

        foreach (Door door in doors)
            door.open = false;

        audioSource.Play();

        await UniTask.Delay(1000);

        if (textures[day] != null)
            display.materials[1].mainTexture = textures[day];

        for (int i = 0; i < stands.Count; i++)
        {
            if (i >= cosmetics.Cosmetics.Count)
            {
                continue;
            }

            List<string> c = cosmetics.Cosmetics[day].Cosmetics;
            if (i < c.Count)
            {
                stands[i].gameObject.SetActive(true);
                stands[i].LoadCosmetic(c[i]);
            }
            else
                stands[i].gameObject.SetActive(false);
        }

        await UniTask.Delay(1000);

        foreach (Door door in doors)
            door.open = true;
    }

    [Serializable]
    public class DailyCosmetics
    {
        public List<DailyCosmetic> Cosmetics = new List<DailyCosmetic>();
    }

    [Serializable]
    public class DailyCosmetic
    {
        public string Day = "";
        public List<string> Cosmetics = new List<string>();

        public DailyCosmetic(string dayName, List<string> cosmeticNames)
        {
            Day = dayName;
            Cosmetics = cosmeticNames;
        }
    }
}
