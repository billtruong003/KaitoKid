using System;
using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;

public class Train : MonoBehaviour
{
    public GameObject rocks;
    public TrainTrack trainTrack;

    [Header("Options")]
    public float offset = 0;
    public float speed = 1;
    public float rotationSpeed = 100;

    [Header("Info")]
    public float time = 0;
    public float lerp = 0;
    public float point = 0;


    void Update()
    {
        DateTime now = AuthenticationUtils.serverTime;

        time = now.Minute * 60 + now.Second + (float)now.Millisecond / 1000;
        time *= speed;
        time += offset;
        time /= 60;

        lerp = SanitiseFloat(time, 1);
        point = lerp * trainTrack.points.Count;

        Vector3 previousPos = transform.position;
        Quaternion previousRot = transform.rotation;
        SetTrainPosition(point);
        transform.position = Vector3.Lerp(previousPos, transform.position, Time.deltaTime * 10);
        transform.rotation = Quaternion.Lerp(previousRot, transform.rotation, Time.deltaTime * rotationSpeed);
    }

    void SetTrainPosition(float t)
    {
        /*
        if (t < 1 && rocks != null)
            rocks.SetActive(DateTime.UtcNow.Minute % 2 == 0);
        */

        for (int i = 0; i < trainTrack.points.Count; i++)
        {
            int rounded = Mathf.FloorToInt(t);
            int index = SanitiseInt(i, trainTrack.points.Count);
            int last = index - 1;
            if (last < 0)
            {
                last = trainTrack.points.Count - 1;
            }

            if (i == rounded)
            {
                Vector3 pos = Vector3.Lerp(trainTrack.points[last].position, trainTrack.points[index].position, t - rounded);
                Quaternion rot = Quaternion.LookRotation((trainTrack.points[index].position - trainTrack.points[last].position).normalized);
                transform.SetPositionAndRotation(pos, rot);
            }
        }
    }

    int SanitiseInt(int i, int l)
    {
        while (i >= l)
            i -= l;

        if (i < 0)
            i = 0;

        return i;
    }

    float SanitiseFloat(float i, float l)
    {
        while (i >= l)
            i -= l;

        if (i < 0)
            i = l + i;

        return i;
    }
}
