# Open Media Transport for Unity

UPM package: [`Packages/org.openmediatransport.unity`](Packages/org.openmediatransport.unity)

## Sample project

This repository is a Unity **2022.3** project. Open the cloned folder in Unity Hub, then play a scene under `Assets/Scenes`:

| Scene | What it does |
| --- | --- |
| `Loopback` | Sends the spinning cube and receives it in the same Player. Press Play. |
| `Sender` | Publishes `Unity Sender` for another OMT receiver. |
| `Receiver` | Lists discovered sources. Click a name to show it on the Quad. |

Windows may prompt for firewall access the first time. Allow local network traffic.

If Loopback stays on "waiting", DaVinci Resolve or another OMT app may be holding port 6400. On the Receiver, type `omt://127.0.0.1:6400` or pick the source from **Select**.

## Install into another project

In Unity Package Manager, add this Git URL (use `#feat/unity-omt-package` until the first PR is merged):

```
https://github.com/MikanseiLaboratory/unity-omt.git?path=/Packages/org.openmediatransport.unity
```

See the [package README](Packages/org.openmediatransport.unity/README.md) for sender/receiver usage.
