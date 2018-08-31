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
using Xunit;
using Sensus.Probes.User.Scripts;

namespace Sensus.Tests.Probes.User.Scripts
{
    
    public class TriggerWindowTests
    {
        [Fact]
        public void NextScheduleWindowNoExpiration()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, null);

                Assert.True(nextTriggerTime.ReferenceTillTrigger >= TimeSpan.FromDays(8));
                Assert.True(nextTriggerTime.ReferenceTillTrigger <= TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.Equal(null, nextTriggerTime.Expiration);
            }
        }

        [Fact]
        public void NextSchedulePointNoExpiration()
        {
            var t = new TriggerWindow("10:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, null);

                Assert.Equal(TimeSpan.FromDays(8), nextTriggerTime.ReferenceTillTrigger);
                Assert.Equal(null, nextTriggerTime.Expiration);
            }
        }

        [Fact]
        public void NextSchedulePointExpirationNotTooBig()
        {
            var t = new TriggerWindow("10:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = reference.AddDays(30);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, null);

                Assert.Equal(TimeSpan.FromDays(31), nextTriggerTime.ReferenceTillTrigger);
                Assert.Equal(null, nextTriggerTime.Expiration);
            }
        }

        [Fact]
        public void NextSchedulePointExpirationExpireWindow()
        {
            var t = new TriggerWindow("10:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = reference.AddDays(30);

            var nextTriggerTime = t.GetNextTriggerTime(reference, after, true, null);

            Assert.Equal(TimeSpan.FromDays(31), nextTriggerTime.ReferenceTillTrigger);
            Assert.Equal(null, nextTriggerTime.Expiration);
        }

        [Fact]
        public void NextScheduleWindowExpireAge()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expire = TimeSpan.FromMinutes(10);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, expire);

                Assert.True(nextTriggerTime.ReferenceTillTrigger >= TimeSpan.FromDays(8));
                Assert.True(nextTriggerTime.ReferenceTillTrigger <= TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.Equal(reference + nextTriggerTime.ReferenceTillTrigger + expire, nextTriggerTime.Expiration);
            }
        }

        [Fact]
        public void NextScheduleWindowWithExpireWindow()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, true, null);

                Assert.True(nextTriggerTime.ReferenceTillTrigger >= TimeSpan.FromDays(8));
                Assert.True(nextTriggerTime.ReferenceTillTrigger <= TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.Equal(reference.AddDays(8).AddHours(2), nextTriggerTime.Expiration);
            }
        }

        [Fact]
        public void NextScheduleWindowWithExpirationTime()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expire = TimeSpan.FromMinutes(1);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, expire);

                Assert.True(nextTriggerTime.ReferenceTillTrigger >= TimeSpan.FromDays(8));
                Assert.True(nextTriggerTime.ReferenceTillTrigger <= TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.That(nextTriggerTime.Expiration, Is.EqualTo(reference + nextTriggerTime.ReferenceTillTrigger + expire).Within(TimeSpan.FromSeconds(1)));
            }
        }
    }
}
