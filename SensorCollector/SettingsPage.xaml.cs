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
            this.entConnectionString.Text = UserPreferences.ConnectionString;
            this.entStream.On = UserPreferences.StreamImmediatelly;
        }

        void Handle_Clicked(object sender, System.EventArgs e)
        {
            //throw new NotImplementedException();

            UserPreferences.Name = this.entName.Text;
            UserPreferences.Age = this.entAge.Text;
            UserPreferences.ConnectionString = this.entConnectionString.Text;
            UserPreferences.StreamImmediatelly = this.entStream.On;

            Navigation.PopModalAsync();
        }
    }
}
