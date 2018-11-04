using System;
namespace SensorCollector
{
    public static class UserPreferences
    {
        public static string Name
        {
            get => Xamarin.Essentials.Preferences.Get("movesense_name", string.Empty);
            set => Xamarin.Essentials.Preferences.Set("movesense_name", value);
        }

        public static string Namespace {
            get => Xamarin.Essentials.Preferences.Get("movesense_namespace", string.Empty);
            set => Xamarin.Essentials.Preferences.Set("movesense_namespace", value);
        }

        public static string KeyName
        {
            get => Xamarin.Essentials.Preferences.Get("movesense_keyname", string.Empty);
            set => Xamarin.Essentials.Preferences.Set("movesense_keyname", value);
        }

        public static string KeyValue
        {
            get => Xamarin.Essentials.Preferences.Get("movesense_key", string.Empty);
            set => Xamarin.Essentials.Preferences.Set("movesense_key", value);
        }

        public static string EventHubName
        {
            get => Xamarin.Essentials.Preferences.Get("movesense_topic", string.Empty);
            set => Xamarin.Essentials.Preferences.Set("movesense_topic", value);
        }

        public static string Age
        {
            get => Xamarin.Essentials.Preferences.Get("movesense_age", String.Empty);
            set => Xamarin.Essentials.Preferences.Set("movesense_age", value);
        }

        public static bool StreamImmediatelly {
            get => Xamarin.Essentials.Preferences.Get("movesense_stream", true);
            set => Xamarin.Essentials.Preferences.Set("movesense_stream", value);
        }
    }
}
