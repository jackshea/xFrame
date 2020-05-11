using System.IO;
using xFrame.Infrastructure;
using xFrame.Logger;
using NLog;
using NLog.Targets;
using UnityEngine;

public class LoggerLoader : SystemLoader
{
    public override void Load()
    {
        base.Load();
        Target.Register<UnityConsoleTarget>("UnityConsole");
        string configFile = GetConfigFile();
        LogManager.LoadConfiguration(configFile);
        if (string.IsNullOrEmpty(LogManager.Configuration.Variables["LogRoot"].Text))
        {
            LogManager.Configuration.Variables["LogRoot"] = Application.persistentDataPath;
        }

        Container.Register<ILog,LogImpl>();
    }

    public string GetConfigFile()
    {
        string configFile = Path.Combine(Application.streamingAssetsPath, "NLog", "nlog.config");
#if UNITY_ANDROID
        var www = new WWW(configFile);
        string dir = Path.Combine(Application.temporaryCachePath, "NLog");
        Directory.CreateDirectory(dir);
        configFile = Path.Combine(dir, "nlog.config");
        File.WriteAllText(configFile, www.text);
#endif
        //Debug.Log($"ConfigName = {configFile}");
        //Debug.Log($"isFileExist = {File.Exists(configFile)}");
        return configFile;
    }
}
