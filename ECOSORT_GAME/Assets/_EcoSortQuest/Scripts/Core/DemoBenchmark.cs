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
    /// <summary>
    /// Công cụ đo đạc hiệu năng FPS và frame-time chuẩn hóa cho bản Windows Standalone.
    /// Chỉ kích hoạt khi có cờ command-line -ecosort-benchmark.
    /// Tự động loại bỏ ảnh hưởng làm chậm khi mất focus và khởi động toàn màn hình.
    /// </summary>
    public sealed class DemoBenchmark : MonoBehaviour
    {
        [Serializable]
        public class Result
        {
            public string environment = "Windows standalone stationary benchmark";
            public string scene_tested = "kitchen";
            public string gpu, cpu, unity;
            public int width, height, frames;
            public bool is_fullscreen, was_focused;
            public int vsync_count, target_fps;
            public double seconds, average_fps, median_ms, p95_ms, min_fps, max_fps;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Application.isEditor || !Environment.GetCommandLineArgs().Contains("-ecosort-benchmark")) return;
            DontDestroyOnLoad(new GameObject("Opt-in Performance Benchmark", typeof(DemoBenchmark)));
        }

        IEnumerator Start()
        {
            // Thiết lập môi trường đo đạc không bị giới hạn (uncapped) và không throttle khi mất focus
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;

            var args = Environment.GetCommandLineArgs();
            int sceneIdx = Array.IndexOf(args, "-ecosort-benchmark-scene");
            string sceneChoice = (sceneIdx >= 0 && sceneIdx + 1 < args.Length) ? args[sceneIdx + 1].ToLowerInvariant() : "kitchen";

            // Yêu cầu toàn màn hình chuẩn để đo GPU rendering chính xác
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (sceneChoice == "campaign")
            {
                EcoSortApp.SelectedLearningMode = LearningMode.Campaign;
                EcoSortApp.SelectedLevel = 1;
            }
            else
            {
                EcoSortApp.SelectedAreaMission = "home-kitchen";
                EcoSortApp.SelectedLearningMode = LearningMode.Explore;
            }

            yield return null;
            yield return SceneManager.LoadSceneAsync("GameScene");
            yield return null;
            yield return null;

            if (sceneChoice != "campaign")
            {
                var app = FindAnyObjectByType<EcoSortApp>();
                if (app != null)
                {
                    var mi = typeof(EcoSortApp).GetMethod("AcceptRestorationMission", BindingFlags.Instance | BindingFlags.NonPublic);
                    mi?.Invoke(app, null);
                }
            }

            // Warmup 5 giây để nạp toàn bộ shader và geometry vào VRAM
            float warmupDeadline = Time.realtimeSinceStartup + 5.0f;
            while (Time.realtimeSinceStartup < warmupDeadline) yield return null;

            // Đo đạc trong 12 giây liên tục
            var samples = new List<double>(10000);
            double start = Time.realtimeSinceStartupAsDouble;
            double previous = start;
            bool maintainedFocus = true;

            while (Time.realtimeSinceStartupAsDouble - start < 12.0)
            {
                yield return null;
                if (!Application.isFocused) maintainedFocus = false;
                double now = Time.realtimeSinceStartupAsDouble;
                double deltaMs = (now - previous) * 1000.0;
                samples.Add(deltaMs);
                previous = now;
            }

            samples.Sort();
            double duration = previous - start;
            double avgFps = samples.Count > 0 && duration > 0 ? samples.Count / duration : 0;
            double medianMs = samples.Count > 0 ? samples[samples.Count / 2] : 0;
            double p95Ms = samples.Count > 0 ? samples[Math.Min(samples.Count - 1, (int)(samples.Count * 0.95))] : 0;

            var result = new Result
            {
                environment = "Windows Standalone " + (sceneChoice == "campaign" ? "Campaign Level 1" : "Kitchen Exploration"),
                scene_tested = sceneChoice,
                gpu = SystemInfo.graphicsDeviceName,
                cpu = SystemInfo.processorType,
                unity = Application.unityVersion,
                width = Screen.width,
                height = Screen.height,
                frames = samples.Count,
                is_fullscreen = Screen.fullScreen,
                was_focused = maintainedFocus,
                vsync_count = QualitySettings.vSyncCount,
                target_fps = Application.targetFrameRate,
                seconds = duration,
                average_fps = avgFps,
                median_ms = medianMs,
                p95_ms = p95Ms,
                min_fps = p95Ms > 0 ? 1000.0 / p95Ms : 0,
                max_fps = samples.Count > 0 && samples[0] > 0 ? 1000.0 / samples[0] : 0
            };

            int outIdx = Array.IndexOf(args, "-ecosort-benchmark-output");
            string output = (outIdx >= 0 && outIdx + 1 < args.Length)
                ? Path.GetFullPath(args[outIdx + 1])
                : Path.Combine(Application.persistentDataPath, "benchmark-result.json");

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
            Debug.Log($"[DemoBenchmark] Kết quả đo FPS đã ghi tại: {output}");

            Application.Quit(0);
        }
    }
}
