﻿#region copyright
// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using SensusService;
using SensusUI.UiProperties;
using System;
using Xamarin.Forms;

namespace SensusUI
{
    /// <summary>
    /// Main Sensus page. First thing the user sees.
    /// </summary>
    public class MainPage : ContentPage
    {
        public static event EventHandler ViewProtocolsTapped;
        public static event EventHandler ViewLogTapped;

        public MainPage()
        {
            Title = "Sensus";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            Button viewProtocolsButton = new Button
            {
                Text = "View Protocols",
                Font = Font.SystemFontOfSize(20)
            };

            viewProtocolsButton.Clicked += (o, e) =>
                {
                    ViewProtocolsTapped(o, e);
                };

            contentLayout.Children.Add(viewProtocolsButton);

            Button viewLogButton = new Button
            {
                Text = "View Log",
                Font = Font.SystemFontOfSize(20)
            };

            viewLogButton.Clicked += (o, e) =>
                {
                    ViewLogTapped(o, e);
                };

            contentLayout.Children.Add(viewLogButton);

            Button stopSensusButton = new Button
            {
                Text = "Stop Sensus",
                Font = Font.SystemFontOfSize(20)
            };

            stopSensusButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Stop Sensus?", "Are you sure you want to stop Sensus?", "OK", "Cancel"))
                        await UiBoundSensusServiceHelper.Get().StopAsync();
                };

            contentLayout.Children.Add(stopSensusButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }

        public void DisplayServiceHelper(SensusServiceHelper serviceHelper)
        {
            // add service helper ui elements to main page
            foreach (StackLayout serviceStack in UiProperty.GetPropertyStacks(serviceHelper))
                ((Content as ScrollView).Content as StackLayout).Children.Add(serviceStack);
        }
    }
}
