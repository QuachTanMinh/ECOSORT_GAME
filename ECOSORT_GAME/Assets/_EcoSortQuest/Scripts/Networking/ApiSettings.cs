using UnityEngine;

namespace EcoSortQuest
{
    public enum ApiMode { Mock, Rest }
    [CreateAssetMenu(menuName = "EcoSort/API Settings")]
    public sealed class ApiSettings : ScriptableObject
    {
        public ApiMode mode = ApiMode.Mock;
        [Tooltip("FastAPI base URL only. Never put an Adafruit key in the client.")]
        public string baseUrl = "";
        [Range(2, 60)] public int timeoutSeconds = 15;
        [Range(0, 1)] public float minimumConfidence = .75f;
        [Min(1)] public int maxUploadBytes = 4 * 1024 * 1024;
        [Range(0, 3)] public float mockLatency = .35f;
    }
}
