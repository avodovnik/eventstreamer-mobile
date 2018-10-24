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

        public static string ConnectionString
        {
            get => Xamarin.Essentials.Preferences.Get("movesense_connectionstring", string.Empty);
            set => Xamarin.Essentials.Preferences.Set("movesense_connectionstring", value);
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
