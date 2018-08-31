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
using System.Linq;
using Xunit;
using Sensus.Extensions;
using Sensus.Probes.User.Scripts;

namespace Sensus.Tests.Probes.User.Scripts
{
    
    public class ScheduleTriggerTests
    {
        [Fact]
        public void Deserialize1PointTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("10:00", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00-10:30" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("10:00-10:30", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1PointTrailingCommaTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00," };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("10:00", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1Point1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1Point1WindowSpacesTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,                10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1PointTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "Su-10:00" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("Su-10:00", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "Mo-10:00-10:30" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("Mo-10:00-10:30", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1PointTrailingCommaTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "Tu-10:00," };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("Tu-10:00", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1Point1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "We-10:00,10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("We-10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1Point1WindowSpacesTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,                Th-10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("10:00, Th-10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void SchedulesAllFutureTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00, 10:10-10:20" };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.Equal(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.Equal(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.Equal(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));
        }

        [Fact]
        public void SchedulesPullsOnlyTenDays()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00" };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 19, 0, 0, 0);

            var triggerTimeCount = schedule.GetTriggerTimes(referenceDate, afterDate).Count();

            Assert.Equal(10, triggerTimeCount);
        }

        [Fact]
        public void SchedulesAllFutureNoExpirationsTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00, 10:10-10:20" };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.Equal(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.Equal(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.Equal(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.Equal(null, triggerTimes[0].Expiration);
            Assert.Equal(null, triggerTimes[1].Expiration);
            Assert.Equal(null, triggerTimes[2].Expiration);
            Assert.Equal(null, triggerTimes[3].Expiration);
            Assert.Equal(null, triggerTimes[4].Expiration);
            Assert.Equal(null, triggerTimes[5].Expiration);
        }

        [Fact]
        public void SchedulesAllFutureExpirationAgeTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "10:00, 10:10-10:20"
            };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate, TimeSpan.FromMinutes(10)).Take(6).ToArray();

            Assert.Equal(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.Equal(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.Equal(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.Equal(referenceDate + triggerTimes[0].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[0].Expiration);
            Assert.Equal(referenceDate + triggerTimes[1].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[1].Expiration);
            Assert.Equal(referenceDate + triggerTimes[2].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[2].Expiration);
            Assert.Equal(referenceDate + triggerTimes[3].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[3].Expiration);
            Assert.Equal(referenceDate + triggerTimes[4].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[4].Expiration);
            Assert.Equal(referenceDate + triggerTimes[5].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[5].Expiration);
        }

        [Fact]
        public void SchedulesAllFutureExpirationWindowTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowExpiration = true,
                WindowsString = "10:00, 10:10-10:20"
            };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.Equal(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.Equal(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.Equal(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.Equal(null, triggerTimes[0].Expiration);
            Assert.Equal(new DateTime(1986, 4, 18, 10, 20, 00), triggerTimes[1].Expiration);
            Assert.Equal(null, triggerTimes[2].Expiration);
            Assert.Equal(new DateTime(1986, 4, 19, 10, 20, 00), triggerTimes[3].Expiration);
            Assert.Equal(null, triggerTimes[4].Expiration);
            Assert.Equal(new DateTime(1986, 4, 20, 10, 20, 00), triggerTimes[5].Expiration);
        }

        [Fact]
        public void SchedulesAllFutureExpirationWindowAndAgeTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowExpiration = true,
                WindowsString = "10:00, 10:10-10:20"
            };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate, TimeSpan.FromMinutes(5)).Take(6).ToArray();

            Assert.Equal(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.Equal(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.Equal(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.Equal(referenceDate + triggerTimes[0].ReferenceTillTrigger + TimeSpan.FromMinutes(5), triggerTimes[0].Expiration);
            Assert.Equal(new DateTime(1986, 4, 18, 10, 20, 00).Min(referenceDate + triggerTimes[1].ReferenceTillTrigger + TimeSpan.FromMinutes(5)), triggerTimes[1].Expiration);
            Assert.Equal(referenceDate + triggerTimes[2].ReferenceTillTrigger + TimeSpan.FromMinutes(5), triggerTimes[2].Expiration);
            Assert.Equal(new DateTime(1986, 4, 19, 10, 20, 00).Min(referenceDate + triggerTimes[3].ReferenceTillTrigger + TimeSpan.FromMinutes(5)), triggerTimes[3].Expiration);
            Assert.Equal(referenceDate + triggerTimes[4].ReferenceTillTrigger + TimeSpan.FromMinutes(5), triggerTimes[4].Expiration);
            Assert.Equal(new DateTime(1986, 4, 20, 10, 20, 00).Min(referenceDate + triggerTimes[5].ReferenceTillTrigger + TimeSpan.FromMinutes(5)), triggerTimes[5].Expiration);
        }

        [Fact]
        public void DowSameDayTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "Mo-12:34"
            };

            var reference = new DateTime(2017, 7, 10, 10, 0, 0);
            var after = new DateTime(2017, 7, 10, 10, 0, 0);  // 2017-7-10 was a Monday

            var triggerTimes = schedule.GetTriggerTimes(reference, after).Take(2).ToArray();

            Assert.Equal(triggerTimes[0].Trigger, reference + new TimeSpan(0, 2, 34, 0));
            Assert.Equal(triggerTimes[1].Trigger, reference + new TimeSpan(7, 2, 34, 0));
        }

        [Fact]
        public void DowSameDayPriorToAfterTimeTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "Mo-8:34"  // this time is prior to the time of day specified in the after datetime. we allow this to be scheduled. in practice this will cause surveys (and other scheduled events) to trigger immediately.
            };

            var reference = new DateTime(2017, 7, 10, 10, 0, 0);
            var after = new DateTime(2017, 7, 10, 10, 0, 0);  // 2017-7-10 was a Monday

            var triggerTimes = schedule.GetTriggerTimes(reference, after).Take(2).ToArray();

            Assert.Equal(triggerTimes[0].Trigger, reference + new TimeSpan(0, -2, 34, 0));
            Assert.Equal(triggerTimes[1].Trigger, reference + new TimeSpan(7, -2, 34, 0));
        }

        [Fact]
        public void DowWithinWeekTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "Fr-12:34"
            };

            var reference = new DateTime(2017, 7, 10, 10, 0, 0);
            var after = new DateTime(2017, 7, 10, 10, 0, 0);  // 2017-7-10 was a Monday

            var triggerTimes = schedule.GetTriggerTimes(reference, after).Take(1).ToArray();

            Assert.Equal(triggerTimes[0].Trigger, reference + new TimeSpan(4, 2, 34, 0));
        }

        [Fact]
        public void DowNextWeekTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "Su-12:34"
            };

            var reference = new DateTime(2017, 7, 10, 10, 0, 0);
            var after = new DateTime(2017, 7, 10, 10, 0, 0);  // 2017-7-10 was a Monday

            var triggerTimes = schedule.GetTriggerTimes(reference, after).Take(1).ToArray();

            Assert.Equal(triggerTimes[0].Trigger, reference + new TimeSpan(6, 2, 34, 0));
        }

        [Fact]
        public void DowNextWeekPriorToAfterTimeOfDayTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "Su-8:34"  // this time of day is prior to the after time of day below. with day-interval-based scheduling, we would skip ahead to the next day (monday) at this time. with dow-based scheduling we will schedule at this exact time.
            };

            var reference = new DateTime(2017, 7, 10, 10, 0, 0);
            var after = new DateTime(2017, 7, 10, 10, 0, 0);  // 2017-7-10 was a Monday

            var triggerTimes = schedule.GetTriggerTimes(reference, after).Take(1).ToArray();

            Assert.Equal(triggerTimes[0].Trigger, reference + new TimeSpan(6, -2, 34, 0));
        }

        [Fact]
        public void DowNextWeekPriorToAfterTimeOfDayPlusIntervalBasedWindowTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "Su-8:34,10:00"  // this time of day is prior to the after time of day below. with day-interval-based scheduling, we would skip ahead to the next day (monday) at this time. with dow-based scheduling we will schedule at this exact time.
            };

            var reference = new DateTime(2017, 7, 10, 10, 0, 0);
            var after = new DateTime(2017, 7, 10, 10, 0, 0);  // 2017-7-10 was a Monday

            var triggerTimes = schedule.GetTriggerTimes(reference, after).Take(11).ToArray();

            Assert.Equal(11, triggerTimes.Length);

            // the 10am interval-based trigger should be scheduled for 10am each day, starting with tomorrow.
            Assert.Equal(triggerTimes[0].Trigger, reference + TimeSpan.FromDays(0));  // Monday
            Assert.Equal(triggerTimes[1].Trigger, reference + TimeSpan.FromDays(1));  // Tuesday
            Assert.Equal(triggerTimes[2].Trigger, reference + TimeSpan.FromDays(2));  // Wednesday
            Assert.Equal(triggerTimes[3].Trigger, reference + TimeSpan.FromDays(3));  // Thursday
            Assert.Equal(triggerTimes[4].Trigger, reference + TimeSpan.FromDays(4));  // Friday
            Assert.Equal(triggerTimes[5].Trigger, reference + TimeSpan.FromDays(5));  // Saturday
            Assert.Equal(triggerTimes[6].Trigger, reference + new TimeSpan(6, -2, 34, 0));  // On Sunday, the DOW-based window will be scheduled at 8:34am...                                             
            Assert.Equal(triggerTimes[7].Trigger, reference + TimeSpan.FromDays(6));        // ...preceding our 10am interval-based trigger.
            Assert.Equal(triggerTimes[8].Trigger, reference + TimeSpan.FromDays(7));        
        }

        [Fact]
        public void DowNextWeekWindowExpirationTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowExpiration = true,
                WindowsString = "Su-12:00-14:00"
            };

            var reference = new DateTime(2017, 7, 10, 10, 0, 0);
            var after = new DateTime(2017, 7, 10, 10, 0, 0);  // 2017-7-10 was a Monday

            var triggerTimes = schedule.GetTriggerTimes(reference, after).Take(1).ToArray();

            Assert.True(triggerTimes[0].Trigger >= reference + new TimeSpan(6, 2, 0, 0));
            Assert.True(triggerTimes[0].Trigger <= reference + new TimeSpan(6, 4, 0, 0));
            Assert.Equal(triggerTimes[0].Expiration.Value, reference + new TimeSpan(6, 4, 0, 0));
        }
    }
}