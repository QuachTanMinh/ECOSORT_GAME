using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EcoSortQuest
{
    public sealed class RestEcoSortService : IEcoSortService
    {
        readonly ApiSettings settings;
        public bool IsMock => false;
        public RestEcoSortService(ApiSettings settings) { this.settings = settings; }
        bool ValidUrl(out string error)
        {
            error = "Hãy cấu hình baseUrl HTTP(S) trong API Settings.";
            return Uri.TryCreate(settings.baseUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https");
        }
        IEnumerator Send(string method, string path, string json, Action<string> success, Action<string> failure)
        {
            if (!ValidUrl(out string error)) { failure(error); yield break; }
            using (var request = new UnityWebRequest(settings.baseUrl.TrimEnd('/') + path, method))
            {
                request.timeout = settings.timeoutSeconds;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    failure("API " + method + " " + path + " (" + request.responseCode + "): " + request.error);
                else success(request.downloadHandler.text);
            }
        }
        public IEnumerator Health(Action<string> success, Action<string> failure) => Send("GET", "/health", null, success, failure);
        public IEnumerator Predict(byte[] image, WasteType mockClass, Action<Prediction> success, Action<string> failure)
        {
            if (!ValidUrl(out string error)) { failure(error); yield break; }
            if (image == null || image.Length == 0 || image.Length > settings.maxUploadBytes)
            { failure("Ảnh trống hoặc vượt giới hạn upload."); yield break; }
            var sections = new List<IMultipartFormSection> {
                new MultipartFormFileSection("file", image, "capture.jpg", "image/jpeg") };
            using (var request = UnityWebRequest.Post(settings.baseUrl.TrimEnd('/') + "/predict", sections))
            {
                request.timeout = settings.timeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                { failure("Predict (" + request.responseCode + "): " + request.error); yield break; }
                Prediction prediction;
                try { prediction = JsonUtility.FromJson<Prediction>(request.downloadHandler.text); }
                catch (Exception) { failure("Phản hồi AI không phải JSON hợp lệ."); yield break; }
                if (prediction == null || string.IsNullOrWhiteSpace(prediction.class_name))
                { failure("Phản hồi AI thiếu class_name."); yield break; }
                success(prediction);
            }
        }
        public IEnumerator GetProgress(string id, Action<PlayerProgress> success, Action<string> failure)
        {
            yield return Send("GET", "/progress/" + Uri.EscapeDataString(id), null, json =>
            {
                PlayerProgress progress;
                try { progress = JsonUtility.FromJson<PlayerProgress>(json); }
                catch (Exception) { failure("Progress JSON không hợp lệ."); return; }
                if (progress == null || progress.player_id != id || progress.unlocked_level < 1 || progress.unlocked_level > 3)
                { failure("Progress trả về thiếu dữ liệu hoặc sai player_id."); return; }
                success(progress);
            }, failure);
        }
        public IEnumerator PutProgress(PlayerProgress progress, Action success, Action<string> failure) =>
            Send("PUT", "/progress/" + Uri.EscapeDataString(progress.player_id), JsonUtility.ToJson(progress), _ => success(), failure);
        public IEnumerator PostScore(ScoreRequest score, Action success, Action<string> failure) =>
            Send("POST", "/scores", JsonUtility.ToJson(score), _ => success(), failure);
        public IEnumerator PostArResult(ArResultRequest result, Action success, Action<string> failure) =>
            Send("POST", "/ar-results", JsonUtility.ToJson(result), _ => success(), failure);
    }
}
