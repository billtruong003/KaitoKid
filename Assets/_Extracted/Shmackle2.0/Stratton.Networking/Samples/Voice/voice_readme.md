Simple voice setup using Photon Voice 2 (+ Fusion)

Steps to setup (This assumes that you have the base Fusion setup):
- On your `NetworkRunner` game object, add `FusionVoiceClient` component. This is the suggested setup from Photon and ensures the proper networking flow with consideration to Fusion. [https://doc.photonengine.com/voice/current/getting-started/voice-for-fusion]
- Add a child game object with a new `Recorder` component on the `NetworkRunner` game object. Assign the new `Recorder` as the `Primary Recorder` of the `NetworkRunner` `FusionVoiceClient` component. Use `SampleNetworkRunner` prefab as reference.
- On your player game object, make sure it has `VoiceNetworkObject`.
- Add a child game object on the player, then add a `Speaker` component.
- Optionally, add a `SpeakerStatusListener` to listen to the speaking status changes of the player. Use `SamplePlayer` prefab as reference. You might need to play around some configs on the network runner's `Recorder` component like `VoiceDetection` and its `Threshold` for more accurate status indicator.
- Place the `VoiceManager` prefab on the scene. `VoiceManager` script exposes more utility function for the Photon voice including mute, channels, device selection, etc. Making this class a singleton could be considered.

This should let you have the voice running in-game. Some UI's are added just to test the features.