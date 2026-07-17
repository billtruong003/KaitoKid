using System;
using System.Collections.Generic;
using Teabag.Core;
using UnityEngine;

public class GameLoopService : IGameLoopService
{
    public event Action OnManagerChanged;
    public GameLoopManager GameLoopManager { get; private set; }
    public SpaceStationManager SpaceStationManager { get; private set; }
    public WaitingZoneManager WaitingZoneManager { get; private set; }
    public TrainController TrainController { get; private set; }
    public SubwayDropVehicle SubwayDropVehicle { get; private set; }

    private readonly List<TrainWagon> _wagons = new();
    public IReadOnlyList<TrainWagon> Wagons => _wagons;

    public int CurrentPhase => GameLoopManager != null ? (int)GameLoopManager.phase : 0;
    public bool HasManager => GameLoopManager != null;
    public bool HasWaitingZoneManager => WaitingZoneManager != null;
    public bool SuppressSceneCleanup => GameLoopManager != null;
    public bool SuppressRespawnOnSceneLoad => GameLoopManager != null || WaitingZoneManager != null;

    public void NotifyMatchComplete() => GameLoopManager?.HandleMatchComplete();
    public void ReturnToStation() => GameLoopManager?.HandleReturnToStationAsync();

    public void Register(GameLoopManager manager) => GameLoopManager = manager;
    public void Register(SpaceStationManager manager) => SpaceStationManager = manager;
    public void Register(WaitingZoneManager manager) => WaitingZoneManager = manager;
    public void Register(TrainController manager) => TrainController = manager;
    public void Register(SubwayDropVehicle manager) => SubwayDropVehicle = manager;

    public void Unregister(GameLoopManager manager) { if (GameLoopManager == manager) GameLoopManager = null; }
    public void Unregister(SpaceStationManager manager) { if (SpaceStationManager == manager) SpaceStationManager = null; }
    public void Unregister(WaitingZoneManager manager) { if (WaitingZoneManager == manager) WaitingZoneManager = null; }
    public void Unregister(TrainController manager) { if (TrainController == manager) TrainController = null; }
    public void Unregister(SubwayDropVehicle manager) { if (SubwayDropVehicle == manager) SubwayDropVehicle = null; }

    public void RegisterWagon(TrainWagon wagon)
    {
        if (wagon != null && !_wagons.Contains(wagon))
            _wagons.Add(wagon);
    }

    public void UnregisterWagon(TrainWagon wagon) => _wagons.Remove(wagon);

    public TrainWagon GetWagonByIndex(int wagonIndex)
    {
        for (var i = 0; i < _wagons.Count; i++)
        {
            if (_wagons[i].WagonIndex == wagonIndex)
                return _wagons[i];
        }
        return null;
    }

    public void Initialize() { }

    public void Dispose()
    {
        GameLoopManager = null;
        SpaceStationManager = null;
        WaitingZoneManager = null;
        TrainController = null;
        SubwayDropVehicle = null;
        _wagons.Clear();
    }

    public void InvokeManagerChanged()
    {
        OnManagerChanged?.Invoke();
    }
}
