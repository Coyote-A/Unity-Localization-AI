using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Unity.Localization.AI.Editor
{
    public static class LocalizationAISettings
    {
        private struct Preset
        {
            public string Label;
            public string BaseUrl;
            public string Model;

            public Preset(string label, string baseUrl, string model)
            {
                Label = label;
                BaseUrl = baseUrl;
                Model = model;
            }
        }

        private static readonly Preset[] Presets = new[]
        {
            new Preset("OpenAI: gpt-4o",              "https://api.openai.com/v1/chat/completions",                              "gpt-4o"),
            new Preset("OpenAI: gpt-4o-mini",         "https://api.openai.com/v1/chat/completions",                              "gpt-4o-mini"),
            new Preset("OpenRouter: Qwen 3.6 Plus",   "https://openrouter.ai/api/v1/chat/completions",                           "qwen/qwen3.6-plus"),
            new Preset("OpenRouter: DeepSeek V3",     "https://openrouter.ai/api/v1/chat/completions",                           "deepseek/deepseek-chat-v3"),
            new Preset("OpenRouter: Claude 3.7",      "https://openrouter.ai/api/v1/chat/completions",                           "anthropic/claude-3.7-sonnet"),
            new Preset("DeepSeek direct",             "https://api.deepseek.com/v1/chat/completions",                            "deepseek-chat"),
            new Preset("DashScope: Qwen Plus",        "https://dashscope-intl.aliyuncs.com/compatible-mode/v1/chat/completions", "qwen-plus"),
            new Preset("Custom",                      "",                                                                         ""),
        };

        private static int FindPresetIndex(string baseUrl, string model)
        {
            for (int i = 0; i < Presets.Length; i++)
            {
                if (Presets[i].Label == "Custom") continue;
                if (Presets[i].BaseUrl == baseUrl && Presets[i].Model == model) return i;
            }
            return Presets.Length - 1; // Custom
        }

        [SettingsProvider]
        public static SettingsProvider CreateLocalizationAISettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Localization AI", SettingsScope.User)
            {
                label = "Localization AI",
                guiHandler = (searchContext) =>
                {
                    var style = new GUIStyle { padding = new RectOffset(10, 10, 10, 10) };
                    EditorGUILayout.BeginVertical(style, GUILayout.ExpandWidth(true));

                    // API Key
                    var apiKey = PlayerPrefs.GetString(OpenAITranslator.API_KEY_PLAYERPREFS_KEY, "");
                    EditorGUI.BeginChangeCheck();
                    apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetString(OpenAITranslator.API_KEY_PLAYERPREFS_KEY, apiKey);
                        PlayerPrefs.Save();
                    }

                    EditorGUILayout.Space();

                    // Current values
                    var currentBaseUrl = PlayerPrefs.GetString(OpenAITranslator.BASE_URL_PLAYERPREFS_KEY, OpenAITranslator.DEFAULT_BASE_URL);
                    var currentModel   = PlayerPrefs.GetString(OpenAITranslator.MODEL_PLAYERPREFS_KEY,   OpenAITranslator.DEFAULT_MODEL);

                    // Preset dropdown
                    int presetIndex = FindPresetIndex(currentBaseUrl, currentModel);
                    EditorGUI.BeginChangeCheck();
                    presetIndex = EditorGUILayout.Popup("Preset", presetIndex, Presets.Select(p => p.Label).ToArray());
                    if (EditorGUI.EndChangeCheck() && Presets[presetIndex].Label != "Custom")
                    {
                        currentBaseUrl = Presets[presetIndex].BaseUrl;
                        currentModel   = Presets[presetIndex].Model;
                        PlayerPrefs.SetString(OpenAITranslator.BASE_URL_PLAYERPREFS_KEY, currentBaseUrl);
                        PlayerPrefs.SetString(OpenAITranslator.MODEL_PLAYERPREFS_KEY,   currentModel);
                        PlayerPrefs.Save();
                    }

                    // Base URL (free text)
                    EditorGUI.BeginChangeCheck();
                    currentBaseUrl = EditorGUILayout.TextField(new GUIContent("Base URL", "OpenAI-compatible chat completions endpoint."), currentBaseUrl);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetString(OpenAITranslator.BASE_URL_PLAYERPREFS_KEY, currentBaseUrl);
                        PlayerPrefs.Save();
                    }

                    // Model (free text)
                    EditorGUI.BeginChangeCheck();
                    currentModel = EditorGUILayout.TextField(new GUIContent("Model",
                        "Any OpenAI-compatible model id. Examples:\n" +
                        "- gpt-4o, gpt-4o-mini\n" +
                        "- qwen/qwen3.6-plus (OpenRouter)\n" +
                        "- deepseek-chat (DeepSeek direct)\n" +
                        "- anthropic/claude-3.7-sonnet (OpenRouter)"), currentModel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetString(OpenAITranslator.MODEL_PLAYERPREFS_KEY, currentModel);
                        PlayerPrefs.Save();
                    }

                    EditorGUILayout.Space();

                    // Override Prompt
                    bool isOverride = PlayerPrefs.GetInt(OpenAITranslator.OVERRIDE_PROMPT_PLAYERPREFS_KEY, 0) == 1;
                    EditorGUI.BeginChangeCheck();
                    isOverride = EditorGUILayout.Toggle("Override System Prompt", isOverride);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetInt(OpenAITranslator.OVERRIDE_PROMPT_PLAYERPREFS_KEY, isOverride ? 1 : 0);
                        PlayerPrefs.Save();
                    }

                    // System Prompt
                    string systemPrompt = PlayerPrefs.GetString(OpenAITranslator.SYSTEM_PROMPT_PLAYERPREFS_KEY, OpenAITranslator.DEFAULT_SYSTEM_PROMPT);

                    EditorGUI.BeginDisabledGroup(!isOverride);
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.LabelField("System Prompt");

                    var textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                    systemPrompt = EditorGUILayout.TextArea(systemPrompt, textAreaStyle, GUILayout.Height(100), GUILayout.ExpandWidth(true));

                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetString(OpenAITranslator.SYSTEM_PROMPT_PLAYERPREFS_KEY, systemPrompt);
                        PlayerPrefs.Save();
                    }
                    EditorGUI.EndDisabledGroup();

                    if (!isOverride)
                    {
                        EditorGUILayout.HelpBox("Default Prompt: " + OpenAITranslator.DEFAULT_SYSTEM_PROMPT, MessageType.None);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "Works with any OpenAI-compatible Chat Completions endpoint: OpenAI, OpenRouter, DeepSeek, DashScope (Qwen), etc.\n" +
                        "Pick a preset or enter Base URL and Model manually.", MessageType.Info);

                    EditorGUILayout.EndVertical();
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Localization", "AI", "OpenAI", "OpenRouter", "Qwen", "DeepSeek", "API Key" })
            };

            return provider;
        }
    }
}
