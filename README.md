# Production path

Application.persistentDataPath: https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html

%userprofile%/AppData/Local/DefaultCompany/vr_osu

To add beatmaps:  
%userprofile%/AppData/Local/DefaultCompany/vr_osu/Assets/Beatmaps/YOUR_OSZ_FILE.osz

# Notes for myself

## Adding certain classes/dependencies into project
https://docs.unity3d.com/Manual/dotnetProfileAssemblies.html

* .NET v4
* Create `csc.rsp` under /Assets
* Add dependency e.g. `-r:System.IO.Compression.dll`
* voila!