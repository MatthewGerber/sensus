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

using System.Collections.Generic;
using Android.Bluetooth.LE;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Threading;
using Sensus.Probes;
using Android.OS;
using Android.Bluetooth;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothClientScannerCallback : ScanCallback
    {
        private BluetoothGattService _service;
        private BluetoothGattCharacteristic _characteristic;
        private AndroidBluetoothDeviceProximityProbe _probe;
        private List<ScanResult> _scanResults;

        public AndroidBluetoothClientScannerCallback(BluetoothGattService service, BluetoothGattCharacteristic characteristic, AndroidBluetoothDeviceProximityProbe probe)
        {
            _service = service;
            _characteristic = characteristic;
            _probe = probe;
            _scanResults = new List<ScanResult>();
        }

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
        {
            lock (_scanResults)
            {
                SensusServiceHelper.Get().Logger.Log("Discovered peripheral:  " + result.Device.Address, LoggingLevel.Normal, GetType());
                _scanResults.Add(result);
            }
        }

        public override void OnBatchScanResults(IList<ScanResult> results)
        {
            lock (_scanResults)
            {
                foreach (ScanResult result in results)
                {
                    SensusServiceHelper.Get().Logger.Log("Discovered peripheral:  " + result.Device.Address, LoggingLevel.Normal, GetType());
                    _scanResults.Add(result);
                }
            }
        }

        public async Task<List<Tuple<string, DateTimeOffset>>> ReadPeripheralCharacteristicValuesAsync(CancellationToken cancellationToken)
        {
            List<Tuple<string, DateTimeOffset>> characteristicValueTimestamps = new List<Tuple<string, DateTimeOffset>>();

            // copy list of peripherals to read. note that the same device may be reported more than once. read each once.
            List<ScanResult> scanResults;
            lock (_scanResults)
            {
                scanResults = _scanResults.GroupBy(scanResult => scanResult.Device.Address).Select(group => group.First()).ToList();
            }

            _probe.ReadAttemptCount += scanResults.Count;

            // read characteristic from each peripheral
            foreach (ScanResult scanResult in scanResults)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                AndroidBluetoothClientGattCallback readCallback = null;

                try
                {
                    readCallback = new AndroidBluetoothClientGattCallback(_service, _characteristic);
                    scanResult.Device.ConnectGatt(global::Android.App.Application.Context, false, readCallback);
                    string characteristicValue = await readCallback.ReadCharacteristicValueAsync(cancellationToken);

                    if (characteristicValue != null)
                    {
                        long msSinceEpoch = Java.Lang.JavaSystem.CurrentTimeMillis() - SystemClock.ElapsedRealtime() + scanResult.TimestampNanos / 1000000;
                        DateTimeOffset encounterTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(msSinceEpoch);

                        characteristicValueTimestamps.Add(new Tuple<string, DateTimeOffset>(characteristicValue, encounterTimestamp));
                        _probe.ReadSuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while reading peripheral:  " + ex, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    readCallback?.DisconnectPeripheral();
                }
            }

            return characteristicValueTimestamps;
        }
    }
}