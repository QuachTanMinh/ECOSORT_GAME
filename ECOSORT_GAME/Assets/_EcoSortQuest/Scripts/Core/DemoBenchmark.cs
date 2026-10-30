using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSortQuest
{
    // Opt-in standalone measurement, never enabled during ordinary play.
    public sealed class DemoBenchmark:MonoBehaviour
    {
        [Serializable]public class Result
        {
            public string environment="Windows standalone stationary home-kitchen",gpu,cpu,unity;
            public int width,height,frames;public double seconds,average_fps,median_ms,p95_ms;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if(Application.isEditor || !Environment.GetCommandLineArgs().Contains("-ecosort-benchmark"))return;
            DontDestroyOnLoad(new GameObject("Opt in performance measurement",typeof(DemoBenchmark)));
        }
        IEnumerator Start()
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Application.runInBackground=true;
            EcoSortApp.SelectedAreaMission="home-kitchen";EcoSortApp.SelectedLearningMode=LearningMode.Explore;
            yield return null;yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return null;
            var app=FindFirstObjectByType<EcoSortApp>();
            typeof(EcoSortApp).GetMethod("AcceptRestorationMission",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);
            float warmup=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<warmup)yield return null;
            var samples=new List<double>(10000);double start=Time.realtimeSinceStartupAsDouble,previous=start;
            while(Time.realtimeSinceStartupAsDouble-start<12)
            {
                yield return null;double now=Time.realtimeSinceStartupAsDouble;samples.Add((now-previous)*1000);previous=now;
            }
            samples.Sort();var r=new Result{gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,unity=Application.unityVersion,
                width=Screen.width,height=Screen.height,frames=samples.Count,seconds=previous-start,average_fps=samples.Count/(previous-start),
                median_ms=samples[samples.Count/2],p95_ms=samples[Math.Min(samples.Count-1,(int)(samples.Count*.95))]};
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-ecosort-benchmark-output");
            string output=index>=0 && index+1<args.Length?Path.GetFullPath(args[index+1]):Path.Combine(Application.persistentDataPath,"benchmark.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(r,true));Application.Quit(0);
        }
    }
}
