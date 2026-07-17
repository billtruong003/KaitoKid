using System;
using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;
using UnityEngine.Events;

public class RepeatingEvent : MonoBehaviour
{
    [Header("Options")]
    public int hour;
    public bool offsetByDays;
    public UnityEvent onEvent;
    public UnityEvent onEnded;
    public bool activated;

    public DateTime clock => AuthenticationUtils.serverTime;

    public DateTime activationTime
    {
        get
        {
            int h = hour;
            if (offsetByDays)
                h += (int)clock.DayOfWeek;

            int dayAdd = 0;

            if (clock.Hour >= h && !activated)
            {
                h++;
                dayAdd++;
            }

            DateTime time = new DateTime(clock.Year, clock.Month, clock.Day + dayAdd, h, 0, 0);
            return time;
        }
    }
    public TimeSpan timeUntil
    {
        get
        {
            return activationTime - clock;
        }
    }

    private void Update()
    {
        int h = hour;
        if (offsetByDays)
            h += (int)clock.DayOfWeek;

        if (clock.Hour == h && clock.Minute < 1)
        {
            if (!activated)
            {
                onEvent.Invoke();
                activated = true;
            }
        }
        else
        {
            if (activated)
            {
                onEnded.Invoke();
                activated = false;
            }
        }
    }
}
