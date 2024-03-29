﻿using Store.Helpers;
using System;
using System.Linq;
using Uwp.Helpers;
using Uwp.Models;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Store
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Edit : Page
    {
        public Edit()
        {
            this.InitializeComponent();

            //ads
            string str = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
            if (str == "Windows.Mobile")
            {
                minorAdd.Width = 320;
                minorAdd.Height = 50;
            }
        }


        private ConfessLoader confess;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ConfessLoader && (ConfessLoader)e.Parameter != null)
            {
                confess = (ConfessLoader)e.Parameter;
                title_text.Text = confess.Title;
                body_text.Text = confess.Body;
                categories.ItemsSource = Uwp.Helpers.Logic.Categories.ToList();
                categories.SelectedValue = confess.Category;
            }
        }
        private async void Update_Tapped(object sender, RoutedEventArgs e)
        {
            if (!Logic.IsInternet())
            {
                await new MessageDialog("No INTERNET connection has been found.").ShowAsync();
                return;
            }
            ChangeLoading(true);
            if (string.IsNullOrEmpty(title_text.Text))
            {
                await new MessageDialog("Please type a Title").ShowAsync();
                ChangeLoading(false);
                return;
            }
            if (categories.SelectedItem == null)
            {
                await new MessageDialog("Please choose a Category").ShowAsync();
                ChangeLoading(false);
                return;
            }
            if (string.IsNullOrEmpty(body_text.Text))
            {
                await new MessageDialog("Please type a Body").ShowAsync();
                ChangeLoading(false);
                return;
            }
            if (body_text.Text.Length < 100)
            {
                await new MessageDialog("Please type more texts for the Body").ShowAsync();
                return;
            }
            try
            {
                Confess fetch = new Confess
                {
                    Title = title_text.Text,
                    Body = body_text.Text,
                    Category = categories.SelectedItem.ToString(),
                    Guid = confess.Guid
                };


                //Save
                await Online.ConfessClass.UpdateConfess(fetch);
                await new MessageDialog("Updated").ShowAsync();
                ChangeLoading(false);

                //close this page
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                else
                {
                    Frame.Navigate(typeof(Homer));
                }

            }
            catch (Exception)
            {

            }
        }

        private async void Delete_Tapped(object sender, RoutedEventArgs e)
        {

            if (!Logic.IsInternet())
            {
                await new MessageDialog("No INTERNET connection has been found.").ShowAsync();
                return;
            }
            ChangeLoading(true);
            MessageDialog answer = new MessageDialog("Do you want to delete this Comment?", "Confirmation");
            answer.Commands.Add(new UICommand("Yes", null));
            answer.Commands.Add(new UICommand("No", null));
            answer.DefaultCommandIndex = 0;
            answer.CancelCommandIndex = 1;
            IUICommand cmd = await answer.ShowAsync();

            if (cmd.Label == "Yes")
            {
                try
                {
                    await Online.ConfessClass.DeleteConfess(confess.Guid);

                }
                catch (Exception)
                {

                }
                await new MessageDialog("Deleted").ShowAsync();
                Frame.Navigate(typeof(Homer));
            }

            ChangeLoading(false);
        }
        private void ChangeLoading(bool value)
        {
            loadingBox.IsEnabled = value;
            if (value)
            {
                loadingBox.Visibility = Visibility.Visible;
            }
            else
            {
                loadingBox.Visibility = Visibility.Collapsed;
            }
        }
        private void title_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            title_text.Text = Logic.ToTitle(title_text.Text);
        }

        private void body_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            int val = body_text.Text.Length;
            int min = 100;
            if (val > min)
            {
                body_header.Text = "Body";
            }
            else
            {
                body_header.Text = $"Body( type {min - val} characters or more)";
            }
        }
    }
}
