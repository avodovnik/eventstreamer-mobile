using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace SensorCollector
{
    public partial class SettingsPage : ContentPage
    {
        
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // load the data from the preferences
            this.entName.Text = UserPreferences.Name;
            this.entAge.Text = UserPreferences.Age;
            //this.entConnectionString.Text = UserPreferences.ConnectionString;
            this.entNamespace.Text = UserPreferences.Namespace;
            this.entKeyName.Text = UserPreferences.KeyName;
            this.entKeyValue.Text = UserPreferences.KeyValue;
            this.entStream.On = UserPreferences.StreamImmediatelly;
            this.entEventHubName.Text = UserPreferences.EventHubName;
        }

        void Handle_Clicked(object sender, System.EventArgs e)
        {
            //throw new NotImplementedException();

            UserPreferences.Name = this.entName.Text;
            UserPreferences.Age = this.entAge.Text;
            //UserPreferences.ConnectionString = this.entConnectionString.Text;
            UserPreferences.KeyName = this.entKeyName.Text;
            UserPreferences.KeyValue = this.entKeyValue.Text;
            UserPreferences.Namespace = this.entNamespace.Text;
            UserPreferences.StreamImmediatelly = this.entStream.On;
            UserPreferences.EventHubName = this.entEventHubName.Text;

            Navigation.PopModalAsync();
        }
    }
}
