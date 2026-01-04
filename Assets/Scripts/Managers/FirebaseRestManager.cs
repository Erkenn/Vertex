using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class FirebaseRestManager : MonoBehaviour
{
    public static FirebaseRestManager Instance;

    // === НАСТРОЙКИ FIREBASE ===
    private string apiKey = "AIzaSyCXRM0CMGeCBpR4gR_g9VNm7kHAig3i5P8";
    private string databaseUrl = "https://vertex-c946c-default-rtdb.europe-west1.firebasedatabase.app";

    // === ДАННЫЕ ПОЛЬЗОВАТЕЛЯ ===
    private string currentUserId;
    private string idToken;
    private string refreshToken;
    private float tokenExpiryTime;

    public bool IsAuthenticated => !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(idToken);
    public string CurrentUserId => currentUserId;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadAuthTokens();
    }

    // === АВТОРИЗАЦИЯ ===
    public void SignIn(string email, string password, Action onSuccess, Action<string> onError)
    {
        StartCoroutine(SignInCoroutine(email, password, onSuccess, onError));
    }

    IEnumerator SignInCoroutine(string email, string password, Action onSuccess, Action<string> onError)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

        var authData = new AuthRequest { email = email, password = password, returnSecureToken = true };
        string json = JsonUtility.ToJson(authData);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(response.idToken))
                {
                    SaveAuthTokens(response);
                    onSuccess?.Invoke();
                    Debug.Log("✅ Успешный вход в Firebase");
                }
                else
                {
                    onError?.Invoke("Неизвестная ошибка входа");
                }
            }
            else
            {
                string error = ExtractErrorMessage(request.downloadHandler.text);
                onError?.Invoke(error);
                Debug.LogError($"❌ Ошибка входа: {error}");
            }
        }
    }

    public void SignUp(string email, string password, Action onSuccess, Action<string> onError)
    {
        StartCoroutine(SignUpCoroutine(email, password, onSuccess, onError));
    }

    IEnumerator SignUpCoroutine(string email, string password, Action onSuccess, Action<string> onError)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

        var authData = new AuthRequest { email = email, password = password };
        string json = JsonUtility.ToJson(authData);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(response.idToken))
                {
                    SaveAuthTokens(response);
                    onSuccess?.Invoke();
                    Debug.Log("✅ Успешная регистрация в Firebase");
                }
                else
                {
                    onError?.Invoke("Неизвестная ошибка регистрации");
                }
            }
            else
            {
                string error = ExtractErrorMessage(request.downloadHandler.text);
                onError?.Invoke(error);
                Debug.LogError($"❌ Ошибка регистрации: {error}");
            }
        }
    }

    public void SignOut()
    {
        currentUserId = null;
        idToken = null;
        refreshToken = null;
        tokenExpiryTime = 0;

        PlayerPrefs.DeleteKey("Firebase_UserId");
        PlayerPrefs.DeleteKey("Firebase_IdToken");
        PlayerPrefs.DeleteKey("Firebase_RefreshToken");
        PlayerPrefs.DeleteKey("Firebase_TokenExpiry");
        PlayerPrefs.Save();

        Debug.Log("🚪 Выход из аккаунта выполнен");
    }

    // === РАБОТА С ДАННЫМИ ===
    public void SaveLevelProgress(int levelIndex, int coinsCollected, float levelTime)
    {
        if (!IsAuthenticated) return;

        var data = new Dictionary<string, object>
        {
            { "coins", coinsCollected },
            { "time", levelTime },
            { "timestamp", DateTime.UtcNow.ToUnixTimeSeconds() }
        };

        string json = Json.Serialize(data);
        string path = $"users/{currentUserId}/levels/{levelIndex}";
        StartCoroutine(PutDataCoroutine(path, json));
    }

    public void SavePlayerName(string name)
    {
        if (!IsAuthenticated) return;

        var data = new Dictionary<string, object> { { "displayName", name } };
        string json = Json.Serialize(data);
        string path = $"users/{currentUserId}/profile";
        StartCoroutine(PutDataCoroutine(path, json));

        PlayerPrefs.SetString("PlayerName", name);
    }

    public void SaveLeaderboardEntry(float totalTime)
    {
        if (!IsAuthenticated) return;

        string displayName = PlayerPrefs.GetString("PlayerName",
            PlayerPrefs.GetString("PlayerEmail", "Игрок").Split('@')[0]);

        var data = new Dictionary<string, object>
        {
            { "displayName", displayName },
            { "totalTime", totalTime },
            { "timestamp", DateTime.UtcNow.ToUnixTimeSeconds() }
        };

        string json = Json.Serialize(data);
        string path = $"leaderboard/{currentUserId}";
        StartCoroutine(PutDataCoroutine(path, json));
    }

    public void LoadLevelProgress(int levelIndex, Action<int, float> onLoaded)
    {
        if (!IsAuthenticated)
        {
            onLoaded?.Invoke(0, 0f);
            return;
        }

        string path = $"users/{currentUserId}/levels/{levelIndex}";
        StartCoroutine(GetDataCoroutine(path, (json) => {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                onLoaded?.Invoke(0, 0f);
                return;
            }

            try
            {
                var data = Json.Deserialize(json) as Dictionary<string, object>;
                int coins = data != null && data.ContainsKey("coins") ? Convert.ToInt32(data["coins"]) : 0;
                float time = data != null && data.ContainsKey("time") ? Convert.ToSingle(data["time"]) : 0f;
                onLoaded?.Invoke(coins, time);
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка парсинга данных уровня: {e}");
                onLoaded?.Invoke(0, 0f);
            }
        }));
    }

    public void LoadLeaderboard(Action<List<LeaderboardEntry>> callback)
    {
        string path = "leaderboard";
        StartCoroutine(GetDataCoroutine(path, (json) => {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                callback?.Invoke(new List<LeaderboardEntry>());
                return;
            }

            try
            {
                var root = Json.Deserialize(json) as Dictionary<string, object>;
                var entries = new List<LeaderboardEntry>();

                if (root != null)
                {
                    foreach (var kvp in root)
                    {
                        var userData = kvp.Value as Dictionary<string, object>;
                        if (userData != null && userData.ContainsKey("totalTime"))
                        {
                            float totalTime = Convert.ToSingle(userData["totalTime"]);
                            string displayName = userData.ContainsKey("displayName") ?
                                userData["displayName"].ToString() : "Аноним";

                            entries.Add(new LeaderboardEntry
                            {
                                UserId = kvp.Key,
                                DisplayName = displayName,
                                TotalTime = totalTime
                            });
                        }
                    }
                }

                entries.Sort((a, b) => a.TotalTime.CompareTo(b.TotalTime));
                callback?.Invoke(entries);
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка загрузки лидерборда: {e}");
                callback?.Invoke(new List<LeaderboardEntry>());
            }
        }));
    }

    // === ВНУТРЕННИЕ МЕТОДЫ ===
    IEnumerator PutDataCoroutine(string path, string jsonData)
    {
        string url = $"{databaseUrl}/{path}.json?auth={idToken}";
        byte[] body = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Ошибка сохранения данных ({url}): {request.error}");
            }
            else
            {
                Debug.Log($"✅ Успешно сохранено: {path}");
            }
        }
    }

    IEnumerator GetDataCoroutine(string path, Action<string> callback)
    {
        string url = $"{databaseUrl}/{path}.json?auth={idToken}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Ошибка загрузки данных ({url}): {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    void SaveAuthTokens(AuthResponse response)
    {
        currentUserId = response.localId;
        idToken = response.idToken;
        refreshToken = response.refreshToken;
        tokenExpiryTime = Time.time + (response.expiresIn - 60);

        PlayerPrefs.SetString("Firebase_UserId", currentUserId);
        PlayerPrefs.SetString("Firebase_IdToken", idToken);
        PlayerPrefs.SetString("Firebase_RefreshToken", refreshToken);
        PlayerPrefs.SetFloat("Firebase_TokenExpiry", tokenExpiryTime);
        PlayerPrefs.Save();
    }

    void LoadAuthTokens()
    {
        if (PlayerPrefs.HasKey("Firebase_IdToken"))
        {
            currentUserId = PlayerPrefs.GetString("Firebase_UserId");
            idToken = PlayerPrefs.GetString("Firebase_IdToken");
            refreshToken = PlayerPrefs.GetString("Firebase_RefreshToken");
            tokenExpiryTime = PlayerPrefs.GetFloat("Firebase_TokenExpiry");

            if (Time.time >= tokenExpiryTime)
            {
                RefreshToken();
            }
        }
    }

    void RefreshToken()
    {
        SignOut();
    }

    string ExtractErrorMessage(string errorJson)
    {
        try
        {
            var errorObj = Json.Deserialize(errorJson) as Dictionary<string, object>;
            if (errorObj != null && errorObj.ContainsKey("error"))
            {
                var errorDetails = errorObj["error"] as Dictionary<string, object>;
                if (errorDetails != null && errorDetails.ContainsKey("message"))
                {
                    return errorDetails["message"].ToString();
                }
            }
        }
        catch { }
        return "Ошибка сети или сервера";
    }

    // Вспомогательные классы
    [Serializable]
    private class AuthRequest
    {
        public string email;
        public string password;
        public bool returnSecureToken = true;
    }

    [Serializable]
    private class AuthResponse
    {
        public string idToken;
        public string refreshToken;
        public string localId;
        public int expiresIn;
    }
}

// === ПРОСТОЙ JSON СЕРИАЛИЗАТОР ===
public static class Json
{
    public static string Serialize(Dictionary<string, object> dict)
    {
        if (dict == null || dict.Count == 0)
            return "{}";

        var entries = new List<string>();
        foreach (var kvp in dict)
        {
            string valueStr = FormatValue(kvp.Value);
            string key = EscapeString(kvp.Key);
            entries.Add($"\"{key}\":{valueStr}");
        }
        return "{" + string.Join(",", entries) + "}";
    }

    public static object Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "null") return null;
        if (json.StartsWith("{") && json.EndsWith("}"))
        {
            // Простой парсер для плоских объектов
            var dict = new Dictionary<string, object>();
            string content = json.Substring(1, json.Length - 2).Trim();
            if (string.IsNullOrEmpty(content)) return dict;

            var pairs = SplitJsonPairs(content);
            foreach (var pair in pairs)
            {
                int colonIndex = pair.IndexOf(':');
                if (colonIndex <= 0) continue;

                string key = UnescapeString(pair.Substring(0, colonIndex).Trim().Trim('"'));
                string valueStr = pair.Substring(colonIndex + 1).Trim();

                object value = ParseJsonValue(valueStr);
                dict[key] = value;
            }
            return dict;
        }
        return json;
    }

    static List<string> SplitJsonPairs(string content)
    {
        var pairs = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (c == '{' || c == '[') depth++;
            else if (c == '}' || c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                pairs.Add(content.Substring(start, i - start));
                start = i + 1;
            }
        }
        if (start < content.Length)
            pairs.Add(content.Substring(start));
        return pairs;
    }

    static object ParseJsonValue(string valueStr)
    {
        valueStr = valueStr.Trim();
        if (valueStr == "null") return null;
        if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            return UnescapeString(valueStr.Substring(1, valueStr.Length - 2));
        if (valueStr == "true") return true;
        if (valueStr == "false") return false;
        if (int.TryParse(valueStr, out int i)) return i;
        if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float f)) return f;
        return valueStr;
    }

    static string FormatValue(object value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{EscapeString(str)}\"";
        if (value is bool b) return b.ToString().ToLower();
        if (value is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (value is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (value is int i) return i.ToString();
        if (value is long l) return l.ToString();
        return value.ToString();
    }

    static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    static string UnescapeString(string s)
    {
        return s.Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
    }
}

public static class DateTimeExtensions
{
    public static long ToUnixTimeSeconds(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }
}

[System.Serializable]
public class LeaderboardEntry
{
    public string UserId;
    public string DisplayName;
    public float TotalTime;
}