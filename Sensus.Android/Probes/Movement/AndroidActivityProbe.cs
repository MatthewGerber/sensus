﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

using System;
using Sensus.Probes;
using Syncfusion.SfChart.XForms;
using Android.App;
using Android.Gms.Common.Apis;
using Android.Gms.Awareness;
using Android.Content;
using Android.Gms.Awareness.Fence;
using Sensus.Exceptions;
using Sensus.Probes.Movement;
using System.Collections.Generic;
using System.Threading;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidActivityProbe : ListeningProbe
    {
        public const string ACTIVITY_RECOGNITION_ACTION = "SENSUS_ACTIVITY_RECOGNITION";

        private GoogleApiClient _awarenessApiClient;
        private Dictionary<string, AndroidActivityProbeBroadcastReceiver> _activityReciever;

        public override Type DatumType
        {
            get
            {
                return typeof(ActivityDatum);
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Activity";
            }
        }

        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        protected override string DeviceAsleepWarning
        {
            get
            {
                return null;
            }
        }

        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting should not be enabled. It does not affect iOS and will unnecessarily reduce battery life on Android.";
            }
        }

        public AndroidActivityProbe()
        {
            _activityReciever = new Dictionary<string, AndroidActivityProbeBroadcastReceiver>();

            // create a separate broadcast receiver for each activity type
            CreateReceiver(nameof(DetectedActivityFence.InVehicle));
            CreateReceiver(nameof(DetectedActivityFence.OnBicycle));
            CreateReceiver(nameof(DetectedActivityFence.OnFoot));
            CreateReceiver(nameof(DetectedActivityFence.Running));
            CreateReceiver(nameof(DetectedActivityFence.Still));
            CreateReceiver(nameof(DetectedActivityFence.Tilting));
            CreateReceiver(nameof(DetectedActivityFence.Unknown));
            CreateReceiver(nameof(DetectedActivityFence.Walking));
        }

        private void CreateReceiver(string activityName)
        {
            AndroidActivityProbeBroadcastReceiver receiver = new AndroidActivityProbeBroadcastReceiver();

            receiver.ActivityChanged += async (sender, activityDatum) =>
            {
                await StoreDatumAsync(activityDatum);
            };

            _activityReciever.Add(activityName, receiver);
        }

        protected override void Initialize()
        {
            base.Initialize();

            // connected to the awareness client
            _awarenessApiClient = new GoogleApiClient.Builder(Application.Context).AddApi(Awareness.Api)

                .AddConnectionCallbacks(

                    bundle =>
                    {
                        SensusServiceHelper.Get().Logger.Log("Connected to Google Awareness API.", LoggingLevel.Normal, GetType());
                    },

                    status =>
                    {
                        SensusServiceHelper.Get().Logger.Log("Connection to Google Awareness API suspended. Status:  " + status, LoggingLevel.Normal, GetType());
                    })

                .Build();

            _awarenessApiClient.BlockingConnect();
        }

        protected override void StartListening()
        {
            AddFence(DetectedActivityFence.InVehicle, nameof(DetectedActivityFence.InVehicle));
            AddFence(DetectedActivityFence.OnBicycle, nameof(DetectedActivityFence.OnBicycle));
            AddFence(DetectedActivityFence.OnFoot, nameof(DetectedActivityFence.OnFoot));
            AddFence(DetectedActivityFence.Running, nameof(DetectedActivityFence.Running));
            AddFence(DetectedActivityFence.Still, nameof(DetectedActivityFence.Still));
            AddFence(DetectedActivityFence.Tilting, nameof(DetectedActivityFence.Tilting));
            AddFence(DetectedActivityFence.Unknown, nameof(DetectedActivityFence.Unknown));
            AddFence(DetectedActivityFence.Walking, nameof(DetectedActivityFence.Walking));
        }

        private void AddFence(int activityId, string activityName)
        {
            string id = ACTIVITY_RECOGNITION_ACTION + "." + activityName;

            // create fence
            AwarenessFence activityFence = DetectedActivityFence.During(activityId);
            Intent activityRecognitionCallbackIntent = new Intent(id);
            PendingIntent activityRecognitionCallbackPendingIntent = PendingIntent.GetBroadcast(Application.Context, 0, activityRecognitionCallbackIntent, 0);
            IFenceUpdateRequest addFenceRequest = new FenceUpdateRequestBuilder()
                .AddFence(id, activityFence, activityRecognitionCallbackPendingIntent)
                .Build();

            // add fence
            UpdateFences(addFenceRequest);

            // register receiver for fence
            Application.Context.RegisterReceiver(_activityReciever[activityName], new IntentFilter(id));
        }

        private void UpdateFences(IFenceUpdateRequest updateRequest)
        {
            ManualResetEvent updateWait = new ManualResetEvent(false);

            try
            {
                // update fences is asynchronous
                Awareness.FenceApi.UpdateFences(_awarenessApiClient, updateRequest).SetResultCallback<Statuses>(status =>
                {
                    try
                    {
                        if (status.IsSuccess)
                        {
                            SensusServiceHelper.Get().Logger.Log("Updated Google Awareness API fences.", LoggingLevel.Normal, GetType());
                        }
                        else if (status.IsCanceled)
                        {
                            SensusServiceHelper.Get().Logger.Log("Google Awareness API fence update canceled.", LoggingLevel.Normal, GetType());
                        }
                        else if (status.IsInterrupted)
                        {
                            SensusServiceHelper.Get().Logger.Log("Google Awareness API fence update interrupted", LoggingLevel.Normal, GetType());
                        }
                        else
                        {
                            string message = "Unrecognized fence update status:  " + status;
                            SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                            SensusException.Report(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while processing update status:  " + ex, LoggingLevel.Normal, GetType());
                    }
                    finally
                    {
                        // ensure that wait is always set
                        updateWait.Set();
                    }
                });
            }
            // catch any errors from calling UpdateFences
            catch (Exception ex)
            {
                // ensure that wait is always set
                SensusServiceHelper.Get().Logger.Log("Exception while updating fences:  " + ex, LoggingLevel.Normal, GetType());
                updateWait.Set();
            }

            updateWait.WaitOne();
        }

        protected override void StopListening()
        {
            // remove fences
            RemoveFence(nameof(DetectedActivityFence.InVehicle));
            RemoveFence(nameof(DetectedActivityFence.OnBicycle));
            RemoveFence(nameof(DetectedActivityFence.OnFoot));
            RemoveFence(nameof(DetectedActivityFence.Running));
            RemoveFence(nameof(DetectedActivityFence.Still));
            RemoveFence(nameof(DetectedActivityFence.Tilting));
            RemoveFence(nameof(DetectedActivityFence.Unknown));
            RemoveFence(nameof(DetectedActivityFence.Walking));

            // disconnect client
            _awarenessApiClient.Disconnect();
        }

        private void RemoveFence(string activityName)
        {
            try
            {
                IFenceUpdateRequest removeFenceRequest = new FenceUpdateRequestBuilder()
                    .RemoveFence(ACTIVITY_RECOGNITION_ACTION + "." + activityName)
                    .Build();

                UpdateFences(removeFenceRequest);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing fence:  " + ex, LoggingLevel.Normal, GetType());
            }

            try
            {
                Application.Context.UnregisterReceiver(_activityReciever[activityName]);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while unregistering receiver:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }
    }
}