# Homework 2 - Import and opening instructions

## Complete project ZIP

Extract `homework2-dgd0005-siracsimsek.zip`, add its `Pong/` directory in Unity Hub, and open it with **Unity 6000.3.2f1**. Unity reads the supplied `Packages/manifest.json` and restores the required packages. Open `Assets/Scenes/Pong.unity`. This is the complete project, including the written answers and verification evidence.

## Unity asset package

`homework2-dgd0005-siracsimsek.unitypackage` contains the project's Assets, including FishNet, both scenes, prefabs, materials, gameplay scripts and Editor tools. A Unity asset package does **not** carry ProjectSettings, the Packages manifest or the external written answers. Import it into a Unity **6000.3.2f1 Universal 3D** project with the dependencies below installed; FishNet is already included, so do not import another copy of it.

Use Window > Package Management > Package Manager to install the registry packages at the pinned versions. Add the `com.coplaydev.unity-mcp` dependency by its Git URL; it is used by the included `PongEditorTools` Editor helper. A running MCP server is optional for playing Pong. The manifest below records the source project's full dependency set; merge entries into an existing project's dependencies rather than removing that project's other packages.

```json
{
  "dependencies": {
    "com.unity.inputsystem": "1.20.1",
    "com.unity.render-pipelines.universal": "17.3.0",
    "com.unity.test-framework": "1.4.6",
    "com.unity.ugui": "2.0.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.multiplayer.playmode": "2.0.2",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0"
  }
}
```

After packages/scripts finish importing, open `Assets/Scenes/Pong.unity`. The complete project ZIP also preserves Player Settings, the 30 Hz scene, build-scene registration, the Server tag definition and the package lock file. The standalone asset package is for importing assets into an already configured project.

The isolated CLI export's temporary audit scripts are excluded from the package. The vendored FishNet Mirror-upgrade menu has a local Unity 6 compatibility patch: it uses `PlayerSettings.GetScriptingDefineSymbols` and `SetScriptingDefineSymbols` with `NamedBuildTarget` instead of the obsolete group methods.

## Written answers

`Week04/ANSWERS.md` is the separate answer sheet. `Week04/README.md` retains the required answers plus screenshots and measured checks. The Console image is `Week04/Captures/server-console-c.png`; the standalone answer copy is distributed alongside that image in the delivery folder.
