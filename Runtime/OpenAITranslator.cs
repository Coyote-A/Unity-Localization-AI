using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Localization.AI
{
    public static class OpenAITranslator
    {
        public const string API_KEY_PLAYERPREFS_KEY = "OPENAI_API_KEY";
        public const string MODEL_PLAYERPREFS_KEY = "OPENAI_MODEL";
        public const string SYSTEM_PROMPT_PLAYERPREFS_KEY = "OPENAI_SYSTEM_PROMPT";
        public const string OVERRIDE_PROMPT_PLAYERPREFS_KEY = "OPENAI_OVERRIDE_PROMPT";
        public const string BASE_URL_PLAYERPREFS_KEY = "OPENAI_BASE_URL";

        public const string DEFAULT_MODEL = "gpt-4o";
        public const string DEFAULT_BASE_URL = "https://api.openai.com/v1/chat/completions";
        public const string DEFAULT_SYSTEM_PROMPT = "You are a professional translator. Maintain the tone and nuances of the original text. Only provide the translated text without any explanations.";

        [Serializable]
        public class ChatRequest
        {
            public string model = "gpt-4o";
            public List<ChatMessage> messages;
        }

        [Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        public class ChatResponse
        {
            public List<Choice> choices;
        }

        [Serializable]
        public class Choice
        {
            public ChatMessage message;
        }

        public static async Task<string> TranslateAsync(string text, string targetLanguage = "English", string systemPrompt = "")
        {
            string apiKey = PlayerPrefs.GetString(API_KEY_PLAYERPREFS_KEY, "");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("API Key is not set. Please set it in Unity Preferences > Localization AI.");
            }

            string baseUrl = PlayerPrefs.GetString(BASE_URL_PLAYERPREFS_KEY, DEFAULT_BASE_URL);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = DEFAULT_BASE_URL;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                if (string.IsNullOrEmpty(systemPrompt))
                {
                    bool isOverride = PlayerPrefs.GetInt(OVERRIDE_PROMPT_PLAYERPREFS_KEY, 0) == 1;
                    if (isOverride)
                    {
                        systemPrompt = PlayerPrefs.GetString(SYSTEM_PROMPT_PLAYERPREFS_KEY, DEFAULT_SYSTEM_PROMPT);
                    }
                    else
                    {
                        systemPrompt = DEFAULT_SYSTEM_PROMPT;
                    }

                    // Append target language to system prompt
                    systemPrompt += $" Translate to {targetLanguage}.";
                }

                var requestBody = new ChatRequest
                {
                    model = PlayerPrefs.GetString(MODEL_PLAYERPREFS_KEY, DEFAULT_MODEL),
                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { role = "system", content = systemPrompt },
                        new ChatMessage { role = "user", content = text }
                    }
                };

                string json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(baseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    var chatResponse = JsonConvert.DeserializeObject<ChatResponse>(responseString);
                    return chatResponse.choices[0].message.content.Trim();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API Error ({baseUrl}): {response.StatusCode} - {error}");
                }
            }
        }
    }
}
